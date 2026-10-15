using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class SponsorRequest
{
    [Required, MaxLength(200)]
    public string SponsorName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(255)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [Url, MaxLength(500)]
    public string? LogoUrl { get; set; }
}

public class SponsorResponse
{
    public int SponsorId { get; set; }
    public string SponsorName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoUrl { get; set; }

    public static SponsorResponse From(Sponsor s) => new()
    {
        SponsorId = s.SponsorId,
        SponsorName = s.SponsorName,
        ContactEmail = s.ContactEmail,
        ContactPhone = s.ContactPhone,
        LogoUrl = s.LogoUrl
    };
}

public class EventSponsorRequest
{
    [Range(1, int.MaxValue)]
    public int SponsorId { get; set; }

    /// <summary>For example Gold, Silver or Bronze.</summary>
    [Required, MaxLength(50)]
    public string SponsorshipTier { get; set; } = string.Empty;
}

public class EventSponsorResponse
{
    public int EventSponsorId { get; set; }
    public int EventId { get; set; }
    public int SponsorId { get; set; }
    public string SponsorName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoUrl { get; set; }
    public string? SponsorshipTier { get; set; }

    // Needs Sponsor to be loaded.
    public static EventSponsorResponse From(EventSponsor es) => new()
    {
        EventSponsorId = es.EventSponsorId,
        EventId = es.EventId,
        SponsorId = es.SponsorId,
        SponsorName = es.Sponsor.SponsorName,
        ContactEmail = es.Sponsor.ContactEmail,
        ContactPhone = es.Sponsor.ContactPhone,
        LogoUrl = es.Sponsor.LogoUrl,
        SponsorshipTier = es.SponsorshipTier
    };
}