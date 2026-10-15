using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, for example 00:27:45. Required when the status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position in the category. Required when the status is Finished.</summary>
    public int? Position { get; set; }

    /// <summary>Finished, DNF or DNS.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
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

    // Needs Enrolment, Enrolment.Participant and Enrolment.Category.Event to be loaded.
    public static ResultResponse From(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
        BibNumber = r.Enrolment.BibNumber,
        CategoryId = r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.CategoryName,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.Date,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status,
        CapturedByOrganiserId = r.CapturedByOrganiserId
    };
}