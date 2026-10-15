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
[Route("api/events/{eventId:int}/weather")]
public class WeatherController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IWeatherService _weather;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(RaceDayDbContext db, IWeatherService weather, ILogger<WeatherController> logger)
    {
        _db = db;
        _weather = weather;
        _logger = logger;
    }

    // Public: the most recent snapshot.
    [HttpGet]
    public async Task<IActionResult> GetLatestWeather(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var latest = await _db.WeatherLogs
            .AsNoTracking()
            .Where(w => w.EventId == eventId)
            .OrderByDescending(w => w.LoggedAt)
            .ThenByDescending(w => w.WeatherLogId)
            .FirstOrDefaultAsync();

        if (latest is null)
            return NotFound(new { message = "No weather has been logged for this event yet." });

        return Ok(WeatherLogResponse.From(latest));
    }

    // Public: every snapshot, newest first.
    [HttpGet("history")]
    public async Task<IActionResult> GetWeatherHistory(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var logs = await _db.WeatherLogs
            .AsNoTracking()
            .Where(w => w.EventId == eventId)
            .OrderByDescending(w => w.LoggedAt)
            .ThenByDescending(w => w.WeatherLogId)
            .ToListAsync();

        return Ok(logs.Select(WeatherLogResponse.From));
    }

    // Fetches current conditions for the event's city from Open-Meteo and stores them as a new snapshot.
    [HttpPost("refresh")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> RefreshWeather(int eventId, CancellationToken cancellationToken)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetProfileId())
            return Forbid();

        if (string.IsNullOrWhiteSpace(ev.City))
            return BadRequest(new { message = "Set a city on the event before refreshing its weather." });

        WeatherReading reading;
        try
        {
            reading = await _weather.GetCurrentWeatherAsync(ev.City, ev.Province, cancellationToken);
        }
        catch (WeatherLocationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (WeatherUnavailableException ex)
        {
            _logger.LogWarning(ex, "Weather lookup failed for event {EventId}", eventId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }

        var log = new WeatherLog
        {
            EventId = eventId,
            TemperatureC = reading.TemperatureC,
            Condition = reading.Condition,
            WindSpeedKmh = reading.WindSpeedKmh,
            LoggedAt = DateTime.UtcNow
        };

        _db.WeatherLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, WeatherLogResponse.From(log));
    }
}