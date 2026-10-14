using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [Required, MaxLength(20)]
    public string BibNumber { get; set; } = string.Empty;

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Cancelled

    public Payment? Payment { get; set; }
    public Result? Result { get; set; }
}