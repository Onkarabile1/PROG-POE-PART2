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
    // >>> Register
    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Public, no token needed. Creates the login account and the linked profile.
    /// Body: email, password (at least 8 characters), role (Organiser or Participant), firstName, lastName,
    /// phone (optional) and dateOfBirth (required for Participants only).
    /// </remarks>
    /// <response code="201">Account created. Returns the new user without the password.</response>
    /// <response code="400">Validation failed, for example an invalid role or a Participant without a date of birth.</response>
    /// <response code="409">The email is already registered.</response>
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]

    // >>> Login
    /// <summary>Logs in and returns a JWT token.</summary>
    /// <remarks>
    /// Public, no token needed. Body: email and password.
    /// Send the returned token as "Authorization: Bearer your-token" on protected requests,
    /// or paste it into the Authorize button in Swagger.
    /// </remarks>
    /// <response code="200">Login succeeded. Returns the token and the user profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Invalid email or password.</response>
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
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