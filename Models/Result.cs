using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public int CapturedByOrganiserId { get; set; }
    public Organiser CapturedByOrganiser { get; set; } = null!;

    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished"; // Finished, DNF, DNS
}