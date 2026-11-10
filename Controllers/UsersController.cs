using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;
using System.Security.Claims;
using AppUser = RaceDay.API.Models.User;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public UsersController(RaceDayDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<AppUser?> LoadCurrentUser() =>
        _db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
    // >>> GetMe
    /// <summary>Returns the logged-in user's account and profile.</summary>
    /// <remarks>Access: any logged-in user. No body.</remarks>
    /// <response code="200">The user and their Organiser or Participant profile.</response>
    /// <response code="401">Not logged in.</response>
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]

    // >>> UpdateMe
    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <remarks>
    /// Access: any logged-in user.
    /// Body: firstName, lastName, phone (optional) and dateOfBirth (optional, Participants only).
    /// </remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]

    // >>> ChangePassword
    /// <summary>Changes the logged-in user's password.</summary>
    /// <remarks>Access: any logged-in user. Body: currentPassword and newPassword (at least 8 characters).</remarks>
    /// <response code="200">Password updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in, or the current password is incorrect.</response>

    [HttpGet]

    public async Task<IActionResult> GetMe()
    {
        var user = await LoadCurrentUser();
        if (user is null) return Unauthorized();

        return Ok(UserResponse.From(user));
    }
    /// <summary>Uploads or replaces the signed-in user's profile picture (Azure Blob Storage).</summary>
    [HttpPost("me/picture")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPicture(IFormFile file, [FromServices] IBlobStorageService blobs)
    {
        // >>> load the current user exactly the way GetMe does, into a variable called `user`
        // (return Unauthorized() / NotFound() the same way GetMe does if it is missing)
        var user = await LoadCurrentUser();

        if (user is null)
            return Unauthorized();
        try { user.ProfilePictureUrl = await blobs.UploadImageAsync(file, "profiles"); }
        catch (ArgumentException ex) { return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest); }

        await _db.SaveChangesAsync();
        return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
    }
    [HttpPut]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await LoadCurrentUser();
        if (user is null) return Unauthorized();

        if (user.Organiser is not null)
        {
            user.Organiser.FirstName = request.FirstName;
            user.Organiser.LastName = request.LastName;
            user.Organiser.PhoneNumber = request.Phone;
        }
        else if (user.Participant is not null)
        {
            user.Participant.FirstName = request.FirstName;
            user.Participant.LastName = request.LastName;
            user.Participant.Phone = request.Phone;
            if (request.DateOfBirth is not null)
                user.Participant.DateOfBirth = request.DateOfBirth.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(UserResponse.From(user));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return Unauthorized();

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Current password is incorrect." });

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password updated." });
    }
}