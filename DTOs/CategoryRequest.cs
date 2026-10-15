using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    [MaxLength(200)] public string? StartPoint { get; set; }
    [MaxLength(200)] public string? EndPoint { get; set; }

    [Range(0, 10000)]
    public int? ElevationGainM { get; set; }

    [MaxLength(500)] public string? MapUrl { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants,
        StartPoint = c.StartPoint,
        EndPoint = c.EndPoint,
        ElevationGainM = c.ElevationGainM,
        MapUrl = c.MapUrl
    };
}