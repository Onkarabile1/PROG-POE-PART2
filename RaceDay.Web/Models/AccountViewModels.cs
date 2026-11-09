using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = Roles.Participant;

    [Required(ErrorMessage = "Please enter your first name."), StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name."), StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}