using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.API.DTOs;

public class EventRequest
{
    [Required, MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Run, Walk or Cycle.</summary>
    [Required, MaxLength(50)]
    [AllowedValues(EventTypes.Run, EventTypes.Walk, EventTypes.Cycle,
        ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? Province { get; set; }
    [MaxLength(200)] public string? Venue { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Venue { get; set; }
    public string? Description { get; set; }

    // Only filled in by GET /api/events/{id}.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CategoryResponse>? Categories { get; set; }

    public static EventResponse From(Event e, bool includeCategories = false) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.Date,
        City = e.City,
        Province = e.Province,
        Venue = e.Venue,
        Description = e.Description,
        Categories = includeCategories
            ? e.Categories.OrderBy(c => c.DistanceKm).Select(CategoryResponse.From).ToList()
            : null
    };
}