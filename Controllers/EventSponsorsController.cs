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
[Route("api/events/{eventId:int}/sponsors")]
public class EventSponsorsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventSponsorsController(RaceDayDbContext db)
    {
        _db = db;
    }
    // >>> GetEventSponsors
    /// <summary>Lists the sponsors linked to an event, with their tiers.</summary>
    /// <remarks>Public, no token needed.</remarks>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">The event's sponsors.</response>
    /// <response code="404">Event not found.</response>
    [ProducesResponseType(typeof(IEnumerable<EventSponsorResponse>), StatusCodes.Status200OK)]

    // >>> LinkSponsor
    /// <summary>Links an existing sponsor to an event with a sponsorship tier.</summary>
    /// <remarks>Access: the Organiser who owns the event. Body: sponsorId and sponsorshipTier (for example Gold).</remarks>
    /// <param name="eventId">The event id.</param>
    /// <response code="201">Sponsor linked to the event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event or sponsor not found.</response>
    /// <response code="409">The sponsor is already linked to this event.</response>
    [ProducesResponseType(typeof(EventSponsorResponse), StatusCodes.Status201Created)]

    // >>> UnlinkSponsor
    /// <summary>Removes a sponsor from an event.</summary>
    /// <remarks>Access: the Organiser who owns the event. No body. The sponsor record itself is kept.</remarks>
    /// <param name="eventId">The event id.</param>
    /// <param name="sponsorId">The sponsor id.</param>
    /// <response code="200">Sponsor removed from the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found, or the sponsor is not linked to it.</response>
    // Public.
    [HttpGet]
    public async Task<IActionResult> GetEventSponsors(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var links = await _db.EventSponsors
            .AsNoTracking()
            .Include(es => es.Sponsor)
            .Where(es => es.EventId == eventId)
            .OrderBy(es => es.Sponsor.SponsorName)
            .ToListAsync();

        return Ok(links.Select(EventSponsorResponse.From));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> LinkSponsor(int eventId, EventSponsorRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        var sponsor = await _db.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == request.SponsorId);
        if (sponsor is null)
            return NotFound(new { message = "Sponsor not found." });

        var alreadyLinked = await _db.EventSponsors
            .AnyAsync(es => es.EventId == eventId && es.SponsorId == request.SponsorId);

        if (alreadyLinked)
            return Conflict(new { message = "This sponsor is already linked to the event." });

        var link = new EventSponsor
        {
            EventId = eventId,
            Sponsor = sponsor,
            SponsorshipTier = request.SponsorshipTier
        };

        _db.EventSponsors.Add(link);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, EventSponsorResponse.From(link));
    }

    [HttpDelete("{sponsorId:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UnlinkSponsor(int eventId, int sponsorId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        var link = await _db.EventSponsors
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.SponsorId == sponsorId);

        if (link is null)
            return NotFound(new { message = "This sponsor is not linked to the event." });

        _db.EventSponsors.Remove(link);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Sponsor removed from the event." });
    }
}