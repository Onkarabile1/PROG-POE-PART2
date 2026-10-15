using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;
using AppUser = RaceDay.API.Models.User;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AuthController(RaceDayDbContext db, ITokenService tokens, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request.Role != Roles.Organiser && request.Role != Roles.Participant)
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        if (request.Role == Roles.Participant && request.DateOfBirth is null)
            return BadRequest(new { message = "Date of birth is required for participants." });

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email already registered." });

        var user = new AppUser { Email = email, Role = request.Role };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        if (request.Role == Roles.Organiser)
        {
            user.Organiser = new Organiser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                PhoneNumber = request.Phone
            };
        }
        else
        {
            user.Participant = new Participant
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth!.Value
            };
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserResponse.From(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        var profile = UserResponse.From(user);

        return Ok(new AuthResponse
        {
            Token = _tokens.CreateToken(user, profile.ProfileId),
            User = profile
        });
    }
}