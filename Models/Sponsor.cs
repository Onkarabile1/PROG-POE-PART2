using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Sponsor
{
    public int SponsorId { get; set; }

    [Required, MaxLength(200)]
    public string SponsorName { get; set; } = string.Empty;

    [MaxLength(255)] public string? ContactEmail { get; set; }
    [MaxLength(20)] public string? ContactPhone { get; set; }
    [MaxLength(500)] public string? LogoUrl { get; set; }

    public ICollection<EventSponsor> EventSponsors { get; set; } = new List<EventSponsor>();
}