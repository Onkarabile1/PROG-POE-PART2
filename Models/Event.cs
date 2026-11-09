using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public Organiser Organiser { get; set; } = null!;

    [Required, MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? Province { get; set; }
    [MaxLength(200)] public string? Venue { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(500)] public string? BannerUrl { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<EventSponsor> EventSponsors { get; set; } = new List<EventSponsor>();
    public ICollection<WeatherLog> WeatherLogs { get; set; } = new List<WeatherLog>();
}