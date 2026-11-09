using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>What the MVC app remembers about the logged-in user. It lives in the server-side session.</summary>
public class SessionUser
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int ProfileId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public interface ICurrentUser
{
    SessionUser? User { get; }
    bool IsAuthenticated { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    string? Token { get; }
    void SignIn(AuthResponse auth);
    void SignOut();
}

public class CurrentUser : ICurrentUser
{
    private const string SessionKey = "RaceDay.User";

    private readonly IHttpContextAccessor _accessor;
    private SessionUser? _cached;
    private bool _loaded;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    public SessionUser? User
    {
        get
        {
            if (!_loaded)
            {
                var json = Session?.GetString(SessionKey);
                _cached = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<SessionUser>(json);
                _loaded = true;
            }

            return _cached;
        }
    }

    public bool IsAuthenticated => User is not null && !string.IsNullOrEmpty(User.Token);
    public bool IsOrganiser => IsAuthenticated && User!.Role == Roles.Organiser;
    public bool IsParticipant => IsAuthenticated && User!.Role == Roles.Participant;
    public string? Token => User?.Token;

    public void SignIn(AuthResponse auth)
    {
        var user = new SessionUser
        {
            Token = auth.Token,
            UserId = auth.User.UserId,
            ProfileId = auth.User.ProfileId,
            Email = auth.User.Email,
            Role = auth.User.Role,
            FirstName = auth.User.FirstName,
            LastName = auth.User.LastName
        };

        Session!.SetString(SessionKey, JsonSerializer.Serialize(user));
        _cached = user;
        _loaded = true;
    }

    public void SignOut()
    {
        Session?.Clear();
        _cached = null;
        _loaded = true;
    }
}