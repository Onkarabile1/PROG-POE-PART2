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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private IQueryable<Enrolment> EnrolmentQuery() =>
        _db.Enrolments
            .AsNoTracking()
            .Include(en => en.Participant)
            .Include(en => en.Payment)
            .Include(en => en.Category)
            .ThenInclude(c => c.Event);

    private static Payment NewPendingPayment(Category category) => new()
    {
        Amount = category.EntryFee,
        PaymentStatus = PaymentStatuses.Pending
    };

    // Bib numbers look like E1-0001: the event id, then a running number for that event.
    private async Task<string> NextBibNumber(int eventId)
    {
        var prefix = $"E{eventId}-";

        var existing = await _db.Enrolments
            .Where(en => en.BibNumber.StartsWith(prefix))
            .Select(en => en.BibNumber)
            .ToListAsync();

        var highest = existing
            .Select(b => int.TryParse(b.Substring(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{highest + 1:D4}";
    }

    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var participantId = User.GetProfileId();

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
            return NotFound(new { message = "Category not found." });

        var existing = await _db.Enrolments
            .Include(en => en.Payment)
            .FirstOrDefaultAsync(en => en.CategoryId == categoryId && en.ParticipantId == participantId);

        if (existing is not null && existing.Status == EnrolmentStatuses.Active)
            return Conflict(new { message = "You are already enrolled in this category." });

        var activeCount = await _db.Enrolments
            .CountAsync(en => en.CategoryId == categoryId && en.Status == EnrolmentStatuses.Active);

        if (activeCount >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        int enrolmentId;

        if (existing is not null)
        {
            // Re-enrolling after a cancellation reuses the cancelled record
            // (a participant can only have one enrolment per category).
            existing.Status = EnrolmentStatuses.Active;
            existing.EnrolmentDate = DateTime.UtcNow;

            if (existing.Payment is null)
            {
                existing.Payment = NewPendingPayment(category);
            }
            else if (existing.Payment.PaymentStatus != PaymentStatuses.Paid)
            {
                existing.Payment.Amount = category.EntryFee;
                existing.Payment.PaymentStatus = PaymentStatuses.Pending;
                existing.Payment.PaymentMethod = null;
                existing.Payment.TransactionRef = null;
                existing.Payment.PaymentDate = null;
            }

            await _db.SaveChangesAsync();
            enrolmentId = existing.EnrolmentId;
        }
        else
        {
            // Two people enrolling at the same moment could be given the same bib number.
            // The unique index rejects the second one, so we work out the next number and try again.
            Enrolment? created = null;

            for (var attempt = 1; attempt <= 3 && created is null; attempt++)
            {
                var candidate = new Enrolment
                {
                    ParticipantId = participantId,
                    CategoryId = categoryId,
                    BibNumber = await NextBibNumber(category.EventId),
                    EnrolmentDate = DateTime.UtcNow,
                    Status = EnrolmentStatuses.Active,
                    Payment = NewPendingPayment(category)
                };

                _db.Enrolments.Add(candidate);

                try
                {
                    await _db.SaveChangesAsync();
                    created = candidate;
                }
                catch (DbUpdateException)
                {
                    _db.ChangeTracker.Clear();
                }
            }

            if (created is null)
                return Conflict(new { message = "Could not complete the enrolment. Please try again." });

            enrolmentId = created.EnrolmentId;
        }

        var enrolment = await EnrolmentQuery().FirstAsync(en => en.EnrolmentId == enrolmentId);
        return StatusCode(StatusCodes.Status201Created, EnrolmentResponse.From(enrolment));
    }

    [HttpGet("api/users/me/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = User.GetProfileId();

        var enrolments = await EnrolmentQuery()
            .Where(en => en.ParticipantId == participantId)
            .OrderByDescending(en => en.Category.Event.Date)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.From));
    }

    [HttpGet("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> GetEnrolmentsForCategory(int categoryId)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetProfileId())
            return Forbid();

        var enrolments = await EnrolmentQuery()
            .Where(en => en.CategoryId == categoryId)
            .OrderBy(en => en.BibNumber)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.From));
    }

    // Participant (own) or Organiser (owner).
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == id);

        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (!User.CanAccess(enrolment))
            return Forbid();

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return Ok(new { message = "Enrolment was already cancelled." });

        // The payment is left as it is. An organiser issues refunds with PUT /api/payments/{id}.
        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Enrolment cancelled." });
    }
}