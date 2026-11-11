namespace RaceDay.Web.Models;

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
    public List<CategoryResponse>? Categories { get; set; }
    public int? EnrolmentCount { get; set; }

    public bool IsUpcoming => EventDate >= DateOnly.FromDateTime(DateTime.Today);

    public string Location =>
        string.Join(", ", new[] { Venue, City, Province }.Where(s => !string.IsNullOrWhiteSpace(s)));

    // The API keeps distance on categories, so an event's distances are the distances of its categories.
    public string Distances =>
        Categories is { Count: > 0 }
            ? string.Join(" / ", Categories.Select(c => c.DistanceKm).Distinct().OrderBy(d => d).Select(d => $"{d:0.##} km"))
            : "No categories yet";

    public string BannerUrl { get; internal set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }

    // What the user sees: Confirmed = enrolled and paid, Pending = enrolled but not paid yet.
    public string DisplayStatus =>
        Status == "Cancelled" ? "Cancelled" :
        PaymentStatus == "Paid" ? "Confirmed" :
        PaymentStatus == "Refunded" ? "Refunded" :
        "Pending";

    public string StatusCss => DisplayStatus switch
    {
        "Confirmed" => "rd-status-confirmed",
        "Cancelled" => "rd-status-cancelled",
        "Refunded" => "rd-status-refunded",
        _ => "rd-status-pending"
    
    
    };
    public bool CanPay => DisplayStatus == "Pending";
    public bool CanCancel => !Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CapturedByOrganiserId { get; set; }

    public string FinishTimeText => FinishTime?.ToString(@"hh\:mm\:ss") ?? "-";
}