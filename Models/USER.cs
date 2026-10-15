using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}