using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/sponsors")]
public class SponsorsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public SponsorsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Public.
    [HttpGet]
    public async Task<IActionResult> GetSponsors()
    {
        var sponsors = await _db.Sponsors
            .AsNoTracking()
            .OrderBy(s => s.SponsorName)
            .ToListAsync();

        return Ok(sponsors.Select(SponsorResponse.From));
    }

    // Sponsors are shared across events, so any organiser can create or edit them.
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> CreateSponsor(SponsorRequest request)
    {
        var sponsor = new Sponsor
        {
            SponsorName = request.SponsorName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            LogoUrl = request.LogoUrl
        };

        _db.Sponsors.Add(sponsor);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, SponsorResponse.From(sponsor));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UpdateSponsor(int id, SponsorRequest request)
    {
        var sponsor = await _db.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == id);
        if (sponsor is null)
            return NotFound(new { message = "Sponsor not found." });

        sponsor.SponsorName = request.SponsorName;
        sponsor.ContactEmail = request.ContactEmail;
        sponsor.ContactPhone = request.ContactPhone;
        sponsor.LogoUrl = request.LogoUrl;

        await _db.SaveChangesAsync();
        return Ok(SponsorResponse.From(sponsor));
    }
}