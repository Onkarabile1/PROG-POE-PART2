using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class EventSponsor
{
    public int EventSponsorId { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int SponsorId { get; set; }
    public Sponsor Sponsor { get; set; } = null!;

    [MaxLength(50)] public string? SponsorshipTier { get; set; } // Gold, Silver, Bronze
}