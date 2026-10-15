using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
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

    [HttpGet]
    public async Task<IActionResult> GetMe()
    {
        var user = await LoadCurrentUser();
        if (user is null) return Unauthorized();

        return Ok(UserResponse.From(user));
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