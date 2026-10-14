using Microsoft.Extensions.Logging;
using RaceDay.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Organiser
    {
        public int OrganiserId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Result> CapturedResults { get; set; } = new List<Result>();
    }

}

