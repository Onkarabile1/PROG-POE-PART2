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
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }
    // >>> GetEvents
    /// <summary>Lists upcoming events.</summary>
    /// <remarks>
    /// Public, no token needed. Without a date filter, only events from today onwards are returned, soonest first.
    /// </remarks>
    /// <param name="province">Optional. Only events in this province.</param>
    /// <param name="eventType">Optional. Run, Walk or Cycle.</param>
    /// <param name="date">Optional. Only events on this exact date (yyyy-MM-dd). Replaces the upcoming-only rule.</param>
    /// <response code="200">The matching events.</response>
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]

    // >>> GetEvent
    /// <summary>Returns one event, including its categories.</summary>
    /// <remarks>Public, no token needed. The categories carry the distances and route details.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The event with its categories.</response>
    /// <response code="404">Event not found.</response>
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]

    // >>> CreateEvent
    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// Access: Organiser.
    /// Body: eventName, eventType (Run, Walk or Cycle), eventDate (yyyy-MM-dd), city, province, venue and description.
    /// </remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed, for example an event type that is not Run, Walk or Cycle.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]

    // >>> UpdateEvent
    /// <summary>Updates an event.</summary>
    /// <remarks>
    /// Access: the Organiser who owns the event.
    /// Body: eventName, eventType (Run, Walk or Cycle), eventDate, city, province, venue and description.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]

    // >>> DeleteEvent
    /// <summary>Deletes an event and its categories, sponsor links and weather logs.</summary>
    /// <remarks>Access: the Organiser who owns the event. No body. Not allowed once anyone has enrolled.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments, so it cannot be deleted.</response>
    // Public. Upcoming events by default; pass ?date=yyyy-MM-dd to see events on one specific day.
    [HttpGet]

    public async Task<IActionResult> GetEvents(
        [FromQuery] string? province,
        [FromQuery] string? eventType,
        [FromQuery] DateOnly? date)
    {
        var query = _db.Events.AsNoTracking();

        if (date is not null)
        {
            var day = date.Value;
            query = query.Where(e => e.Date == day);
        }
        else
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.Date >= today);
        }

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(e => e.Province == province);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        var events = await query.OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(e => EventResponse.From(e)));
    }
    /// <summary>Lists the logged-in Organiser's own events, past and upcoming, with their categories and enrolment counts.</summary>
    /// <remarks>Used by the Organiser dashboard in the web application. Sorted by date, soonest first. Cancelled enrolments are not counted.</remarks>
    /// <response code="200">The Organiser's events.</response>
    /// <response code="401">No valid login token was sent.</response>
    /// <response code="403">The logged-in user is not an Organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEvents()
    {
        var organiserId = User.GetProfileId();

        var events = await _db.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.Date)
            .ToListAsync();

        var counts = await _db.Enrolments
            .Where(en => en.Category.Event.OrganiserId == organiserId && en.Status == EnrolmentStatuses.Active)
            .GroupBy(en => en.Category.EventId)
            .Select(g => new { EventId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Total);

        var response = events.Select(e =>
        {
            var item = EventResponse.From(e, includeCategories: true);
            item.EnrolmentCount = counts.TryGetValue(e.EventId, out var total) ? total : 0;
            return item;
        });

        return Ok(response);
    }
    // Public.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null)
            return NotFound(new { message = "Event not found." });

        return Ok(EventResponse.From(ev, includeCategories: true));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var ev = new Event
        {
            OrganiserId = User.GetProfileId(),
            EventName = request.EventName,
            EventType = request.EventType,
            Date = request.EventDate!.Value,
            City = request.City,
            Province = request.Province,
            Venue = request.Venue,
            Description = request.Description
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, EventResponse.From(ev));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        ev.EventName = request.EventName;
        ev.EventType = request.EventType;
        ev.Date = request.EventDate!.Value;
        ev.City = request.City;
        ev.Province = request.Province;
        ev.Venue = request.Venue;
        ev.Description = request.Description;

        await _db.SaveChangesAsync();
        return Ok(EventResponse.From(ev));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        // Enrolments are protected by a Restrict rule, so an event that has any cannot be removed.
        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category.EventId == id);
        if (hasEnrolments)
            return Conflict(new { message = "Cannot delete an event that has enrolments." });

        // Categories, sponsor links and weather logs are deleted with the event (cascade).
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }
    /// <summary>Uploads or replaces the banner image of an event (stored in Azure Blob Storage).</summary>
    /// <remarks>Organiser who owns the event only. JPG, PNG or WEBP, max 5 MB.</remarks>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, [FromServices] IBlobStorageService blobs)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        try { ev.BannerUrl = await blobs.UploadImageAsync(file, "banners"); }
        catch (ArgumentException ex) { return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest); }

        await _db.SaveChangesAsync();
        return Ok(EventResponse.From(ev, includeCategories: true));
    }
}