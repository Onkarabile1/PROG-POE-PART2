using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; }

    public int MaxParticipants { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal DistanceKm { get; set; }

    [MaxLength(200)] public string? StartPoint { get; set; }
    [MaxLength(200)] public string? EndPoint { get; set; }
    public int? ElevationGainM { get; set; }
    [MaxLength(500)] public string? MapUrl { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}