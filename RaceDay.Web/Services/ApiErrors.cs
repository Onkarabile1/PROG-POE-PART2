using System.Net;
using System.Text.Json;

namespace RaceDay.Web.Services;

/// <summary>Turns an error response from the API into a short, friendly message.</summary>
public static class ApiErrors
{
    public static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        return Clean(message.GetString());

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                                ? p.Value.EnumerateArray().Select(e => e.GetString())
                                : Enumerable.Empty<string?>())
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .Select(Clean)
                            .Distinct()
                            .ToList();

                        if (messages.Count > 0)
                            return string.Join(" ", messages);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: fall through to the generic message below.
        }

        return ForStatus(response.StatusCode);
    }

    // Never show technical text such as JSON paths or line numbers.
    private static string Clean(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Contains("JSON") || message.Contains("Path:"))
            return "Some of the details entered are not valid. Please check and try again.";

        return message;
    }

    private static string ForStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "Some of the details entered are not valid. Please check and try again.",
        HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "We could not find what you were looking for.",
        HttpStatusCode.Conflict => "That action conflicts with existing information.",
        HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout =>
            "The RaceDay service is having trouble right now. Please try again shortly.",
        _ => "Something went wrong. Please try again."
    };
}