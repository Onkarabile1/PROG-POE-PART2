using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class WeatherLog
{
    public int WeatherLogId { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TemperatureC { get; set; }

    [MaxLength(100)] public string? Condition { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal WindSpeedKmh { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}