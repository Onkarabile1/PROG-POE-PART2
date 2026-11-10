using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class DashboardViewModel
{
    public List<EventResponse> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount ?? 0);
    public List<EventResponse> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ToList();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(200)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please describe the event."), StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please enter the city."), StringLength(100)]
    public string? City { get; set; }

    [Required(ErrorMessage = "Please choose the province.")]
    public string? Province { get; set; }

    [StringLength(200)]
    [Display(Name = "Venue (optional)")]
    public string? Venue { get; set; }

    public bool IsEdit => EventId.HasValue;
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please name the category (for example 10 km or Under 18)."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee.")]
    [Range(0, 100000, ErrorMessage = "The entry fee must be 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }

    [Required(ErrorMessage = "Please enter the participant limit.")]
    [Range(1, 100000, ErrorMessage = "The limit must be at least 1.")]
    [Display(Name = "Participant limit")]
    public int? MaxParticipants { get; set; }

    [StringLength(200)]
    [Display(Name = "Start point")]
    public string? StartPoint { get; set; }

    [StringLength(200)]
    [Display(Name = "End point")]
    public string? EndPoint { get; set; }

    [Range(0, 10000, ErrorMessage = "Elevation gain must be between 0 and 10000 m.")]
    [Display(Name = "Elevation gain (m)")]
    public int? ElevationGainM { get; set; }

    [Url(ErrorMessage = "Please enter a valid web address."), StringLength(500)]
    [Display(Name = "Map link")]
    public string? MapUrl { get; set; }
    public IFormFile? Banner { get; set; }
    public string? ExistingBannerUrl { get; set; }
}

public class CategoriesPageViewModel
{
    public EventResponse Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class CategoryEnrolments
{
    public CategoryResponse Category { get; set; } = new();
    public List<EnrolmentResponse> Enrolments { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventResponse Event { get; set; } = new();
    public List<CategoryEnrolments> Groups { get; set; } = new();

    public IEnumerable<EnrolmentResponse> All => Groups.SelectMany(g => g.Enrolments);
    public int ConfirmedCount => All.Count(e => e.DisplayStatus == "Confirmed");
    public int PendingCount => All.Count(e => e.DisplayStatus == "Pending");
    public int CancelledCount => All.Count(e => e.DisplayStatus == "Cancelled");
}

public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string Status { get; set; } = "Finished";
    public string FinishTimeText { get; set; } = string.Empty;
    public int? Position { get; set; }
}

public class ResultsPageViewModel
{
    public EventResponse Event { get; set; } = new();
    public List<CategoryResponse> Categories { get; set; } = new();
    public CategoryResponse? Selected { get; set; }
    public List<ResultRowViewModel> Rows { get; set; } = new();
    public bool CanEnterResults { get; set; }
}

public class SaveResultViewModel
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
}
