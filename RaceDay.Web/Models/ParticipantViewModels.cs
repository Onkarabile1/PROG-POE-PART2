using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RaceDay.Web.Models;

public class BrowseViewModel
{
    public List<EventResponse> Events { get; set; } = new();
    public string? Province { get; set; }
    public string? EventType { get; set; }
    public DateOnly? Date { get; set; }
}

public class EventDetailsViewModel
{
    public EventResponse Event { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

public class EnrolmentRow
{
    public EnrolmentResponse Enrolment { get; set; } = new();
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Fee { get; set; }
}

public class ResultRow
{
    public ResultResponse Result { get; set; } = new();
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Finishers { get; set; }

    public string PositionText =>
        Result.Position is { } p
            ? Finishers > 0 ? $"{Ordinal(p)} of {Finishers}" : Ordinal(p)
            : "—";

    private static string Ordinal(int n)
    {
        var suffix = (n % 100 is >= 11 and <= 13) ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n.ToString(CultureInfo.InvariantCulture) + suffix;
    }
}

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int EnrolmentId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
}

// The profile form is built from whatever text fields GET api/users/me returns,
// so it keeps working whatever your Part 2 user fields are called.
public class ProfileViewModel
{
    private static readonly string[] Hidden = { "role", "token", "createdat", "createddate" };

    public Dictionary<string, string?> Fields { get; set; } = new();
    public string? PictureUrl { get; set; }

    public static string Label(string key) =>
        char.ToUpper(key[0]) + Regex.Replace(key[1..], "([a-z])([A-Z])", "$1 $2");

    public static ProfileViewModel From(Dictionary<string, JsonElement> data)
    {
        var vm = new ProfileViewModel();
        foreach (var (key, value) in data)
        {
            var lower = key.ToLowerInvariant();
            var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            if (lower == "profilepictureurl") { vm.PictureUrl = text; continue; }
            if (lower.EndsWith("id") || lower.Contains("password") || lower.Contains("url") || Hidden.Contains(lower)) continue;
            if (value.ValueKind is JsonValueKind.String or JsonValueKind.Null) vm.Fields[key] = text;
        }
        return vm;
    }
}