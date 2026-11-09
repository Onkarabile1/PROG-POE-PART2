using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDay.Web.Services;

public record UploadResult(bool Success, string? Error);

public interface IRaceDayUploads
{
    Task<UploadResult> UploadAsync(string path, IFormFile file);
}

public class RaceDayUploads : IRaceDayUploads
{
    private readonly HttpClient _http;
    private readonly ICurrentUser _current;

    public RaceDayUploads(HttpClient http, ICurrentUser current) { _http = http; _current = current; }

    public async Task<UploadResult> UploadAsync(string path, IFormFile file)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(content, "file", file.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/')) { Content = form };
            var token = _current.Token;
            if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode) return new UploadResult(true, null);
            return new UploadResult(false, ExtractMessage(await res.Content.ReadAsStringAsync(), res.StatusCode));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new UploadResult(false, "The RaceDay service could not be reached. Please try again.");
        }
    }

    private static string ExtractMessage(string body, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "detail", "message", "title" })
                if (doc.RootElement.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String)
                    return p.GetString()!;
        }
        catch { /* fall through */ }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You are not allowed to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            _ => "The upload failed. Please try again."
        };
    }
}