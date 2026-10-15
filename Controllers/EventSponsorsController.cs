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