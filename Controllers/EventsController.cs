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
}