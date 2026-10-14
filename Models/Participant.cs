using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Participant
    {
        public int ParticipantId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateOnly DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}

