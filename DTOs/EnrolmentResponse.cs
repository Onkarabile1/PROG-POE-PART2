using RaceDay.Api.Models;
using RaceDay.API.Models;

namespace RaceDay.API.DTOs;

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

    // Needs Participant, Payment and Category.Event to be loaded.
    public static EnrolmentResponse From(Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = $"{en.Participant.FirstName} {en.Participant.LastName}",
        CategoryId = en.CategoryId,
        CategoryName = en.Category.CategoryName,
        EventId = en.Category.EventId,
        EventName = en.Category.Event.EventName,
        EventDate = en.Category.Event.Date,
        BibNumber = en.BibNumber,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        PaymentStatus = en.Payment?.PaymentStatus

    };
}