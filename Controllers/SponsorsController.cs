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
    // >>> GetSponsors
    /// <summary>Lists all sponsors on the platform.</summary>
    /// <remarks>Public, no token needed. Ordered by name.</remarks>
    /// <response code="200">All sponsors.</response>
    [ProducesResponseType(typeof(IEnumerable<SponsorResponse>), StatusCodes.Status200OK)]

    // >>> CreateSponsor
    /// <summary>Creates a sponsor record.</summary>
    /// <remarks>
    /// Access: Organiser.
    /// Body: sponsorName, contactEmail (optional, valid email), contactPhone (optional) and logoUrl (optional, valid URL).
    /// </remarks>
    /// <response code="201">Sponsor created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create sponsors.</response>
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status201Created)]

    // >>> UpdateSponsor
    /// <summary>Updates a sponsor's details.</summary>
    /// <remarks>Access: Organiser. Same body as creating a sponsor.</remarks>
    /// <param name="id">The sponsor id.</param>
    /// <response code="200">The updated sponsor.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can update sponsors.</response>
    /// <response code="404">Sponsor not found.</response>
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status200OK)]
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