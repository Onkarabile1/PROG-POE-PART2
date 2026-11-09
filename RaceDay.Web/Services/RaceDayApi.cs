using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public interface IRaceDayApi
{
    Task<ApiResult<T>> GetAsync<T>(string url);
    Task<ApiResult<T>> PostAsync<T>(string url, object? body = null);
    Task<ApiResult<T>> PutAsync<T>(string url, object? body = null);
    Task<ApiResult> DeleteAsync(string url);
}

/// <summary>The only class in the MVC app that talks to the Part 2 API. It sends the session's token with every call.</summary>
public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ICurrentUser _currentUser;

    public RaceDayApi(HttpClient http, ICurrentUser currentUser)
    {
        _http = http;
        _currentUser = currentUser;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, body);

    public async Task<ApiResult> DeleteAsync(string url)
    {
        var result = await SendAsync<object>(HttpMethod.Delete, url, null);
        return new ApiResult { Success = result.Success, StatusCode = result.StatusCode, Error = result.Error };
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url.TrimStart('/'));

            if (_currentUser.Token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUser.Token);

            if (body is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            else if (method == HttpMethod.Post || method == HttpMethod.Put)
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    StatusCode = response.StatusCode,
                    Error = await ApiErrors.ReadAsync(response)
                };
            }

            var text = await response.Content.ReadAsStringAsync();
            var data = string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOptions);

            return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ApiResult<T>
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Error = "The RaceDay service is not reachable right now. Please try again shortly."
            };
        }
    }
}