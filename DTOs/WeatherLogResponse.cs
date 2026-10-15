using RaceDay.Api.Models;
using RaceDay.API.Models;

namespace RaceDay.API.DTOs;

public class WeatherLogResponse
{
    public int WeatherLogId { get; set; }
    public int EventId { get; set; }
    public decimal TemperatureC { get; set; }
    public string? Condition { get; set; }
    public decimal WindSpeedKmh { get; set; }
    public DateTime LoggedAt { get; set; }

    public static WeatherLogResponse From(WeatherLog w) => new()
    {
        WeatherLogId = w.WeatherLogId,
        EventId = w.EventId,
        TemperatureC = w.TemperatureC,
        Condition = w.Condition,
        WindSpeedKmh = w.WindSpeedKmh,
        LoggedAt = w.LoggedAt
    };
}