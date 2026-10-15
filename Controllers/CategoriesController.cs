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
public class CategoriesController : ControllerBase
{
    private const string ActiveStatus = "Active";

    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }
    // >>> GetCategoriesForEvent
    /// <summary>Lists the categories of an event, shortest distance first.</summary>
    /// <remarks>Public, no token needed.</remarks>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">Event not found.</response>
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]

    // >>> GetCategory
    /// <summary>Returns one category, including its route details.</summary>
    /// <remarks>Public, no token needed. Route details are the start and end points, elevation gain and map link.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="200">The category.</response>
    /// <response code="404">Category not found.</response>
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]

    // >>> CreateCategory
    /// <summary>Adds a category (for example 5km or Under 20) to an event.</summary>
    /// <remarks>
    /// Access: the Organiser who owns the event.
    /// Body: categoryName, distanceKm, entryFee, maxParticipants, startPoint, endPoint, elevationGainM and mapUrl.
    /// </remarks>
    /// <param name="eventId">The event id.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]

    // >>> UpdateCategory
    /// <summary>Updates a category, including its route details.</summary>
    /// <remarks>
    /// Access: the Organiser who owns the event. Same body as creating a category.
    /// The participant limit cannot be lower than the number already enrolled.
    /// </remarks>
    /// <param name="id">The category id.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The new participant limit is lower than the number already enrolled.</response>
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]

    // >>> DeleteCategory
    /// <summary>Deletes a category that has no active enrolments.</summary>
    /// <remarks>Access: the Organiser who owns the event. No body.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="200">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has active enrolments.</response>
    // Public.
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.From));
    }

    // Public.
    [HttpGet("api/categories/{id:int}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
            return NotFound(new { message = "Category not found." });

        return Ok(CategoryResponse.From(category));
    }

    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants,
            StartPoint = request.StartPoint,
            EndPoint = request.EndPoint,
            ElevationGainM = request.ElevationGainM,
            MapUrl = request.MapUrl
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, CategoryResponse.From(category));
    }

    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetProfileId())
            return Forbid();

        // The new limit cannot be lower than the number of people already enrolled.
        var activeCount = await _db.Enrolments.CountAsync(en => en.CategoryId == id && en.Status == ActiveStatus);
        if (request.MaxParticipants < activeCount)
            return Conflict(new { message = $"{activeCount} participants are already enrolled, so the limit cannot be lower than that." });

        category.CategoryName = request.CategoryName;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        category.StartPoint = request.StartPoint;
        category.EndPoint = request.EndPoint;
        category.ElevationGainM = request.ElevationGainM;
        category.MapUrl = request.MapUrl;

        await _db.SaveChangesAsync();
        return Ok(CategoryResponse.From(category));
    }

    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetProfileId())
            return Forbid();

        var enrolments = await _db.Enrolments.Where(en => en.CategoryId == id).ToListAsync();

        if (enrolments.Any(en => en.Status == ActiveStatus))
            return Conflict(new { message = "Cannot delete a category that has active enrolments." });

        // Only cancelled enrolments are left; remove them (their payments and results go with them).
        _db.Enrolments.RemoveRange(enrolments);
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Category deleted." });
    }
}