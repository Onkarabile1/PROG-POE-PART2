using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private IQueryable<Result> ResultQuery() =>
        _db.Results
            .AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event);

    // Returns an error message, or null when the request is valid. "status" is the cleaned-up status value.
    private static string? Validate(ResultRequest request, out string status)
    {
        status = ResultStatuses.All.FirstOrDefault(
            s => string.Equals(s, request.Status, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        if (status.Length == 0)
            return $"Status must be one of: {string.Join(", ", ResultStatuses.All)}.";

        if (status == ResultStatuses.Finished)
        {
            if (request.FinishTime is null || request.FinishTime <= TimeSpan.Zero)
                return "A finished result needs a finish time greater than zero.";

            if (request.Position is null || request.Position < 1)
                return "A finished result needs a position of 1 or more.";
        }
        else if (request.FinishTime is not null || request.Position is not null)
        {
            return "A DNF or DNS result cannot have a finish time or a position.";
        }

        return null;
    }

    // Two people cannot hold the same position in one category.
    private Task<bool> PositionTaken(int categoryId, int position, int ignoreResultId = 0) =>
        _db.Results.AnyAsync(r =>
            r.Enrolment.CategoryId == categoryId &&
            r.Position == position &&
            r.ResultId != ignoreResultId);

    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Result)
            .Include(en => en.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (!User.OrganisesEnrolment(enrolment))
            return Forbid();

        var error = Validate(request, out var status);
        if (error is not null)
            return BadRequest(new { message = error });

        if (enrolment.Status != EnrolmentStatuses.Active)
            return Conflict(new { message = "A result cannot be captured for a cancelled enrolment." });

        if (enrolment.Result is not null)
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        if (request.Position is not null && await PositionTaken(enrolment.CategoryId, request.Position.Value))
            return Conflict(new { message = $"Position {request.Position} is already taken in this category." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            CapturedByOrganiserId = User.GetProfileId(),
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var created = await ResultQuery().FirstAsync(r => r.ResultId == result.ResultId);
        return StatusCode(StatusCodes.Status201Created, ResultResponse.From(created));
    }

    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> CorrectResult(int id, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment)
            .ThenInclude(en => en.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null)
            return NotFound(new { message = "Result not found." });

        if (!User.OrganisesEnrolment(result.Enrolment))
            return Forbid();

        var error = Validate(request, out var status);
        if (error is not null)
            return BadRequest(new { message = error });

        if (request.Position is not null &&
            await PositionTaken(result.Enrolment.CategoryId, request.Position.Value, result.ResultId))
            return Conflict(new { message = $"Position {request.Position} is already taken in this category." });

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;

        await _db.SaveChangesAsync();

        var updated = await ResultQuery().FirstAsync(r => r.ResultId == id);
        return Ok(ResultResponse.From(updated));
    }

    [HttpGet("api/users/me/results")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = User.GetProfileId();

        var results = await ResultQuery()
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.Date)
            .ToListAsync();

        return Ok(results.Select(ResultResponse.From));
    }

    // Public leaderboard: finishers by position first, then DNF and DNS.
    [HttpGet("api/categories/{categoryId:int}/results")]
    public async Task<IActionResult> GetCategoryResults(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId))
            return NotFound(new { message = "Category not found." });

        var results = await ResultQuery()
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .ToListAsync();

        return Ok(results
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ThenBy(r => r.Status)
            .Select(ResultResponse.From));
    }
}