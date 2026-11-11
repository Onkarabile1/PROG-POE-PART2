using System.ComponentModel.DataAnnotations;
using RaceDay.API.Models;

namespace RaceDay.API.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
    public int UserId { get; set; }
    public int ProfileId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Expects the Organiser/Participant navigation property to be loaded.
    public static UserResponse From(RaceDay.API.Models.User user)
    {
        var response = new UserResponse
        {

            UserId = user.UserId,
            Email = user.Email,
            Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl,
        };

        if (user.Organiser is not null)
        {
            response.ProfileId = user.Organiser.OrganiserId;
            response.FirstName = user.Organiser.FirstName;
            response.LastName = user.Organiser.LastName;
            response.Phone = user.Organiser.PhoneNumber;
        }
        else if (user.Participant is not null)
        {
            response.ProfileId = user.Participant.ParticipantId;
            response.FirstName = user.Participant.FirstName;
            response.LastName = user.Participant.LastName;
            response.Phone = user.Participant.Phone;
            response.DateOfBirth = user.Participant.DateOfBirth;
        }

        return response;
    }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
    public string? ProfilePictureUrl { get; set; }

}