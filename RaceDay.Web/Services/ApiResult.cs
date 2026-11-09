using System.Net;

namespace RaceDay.Web.Services;

public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>A friendly message that is safe to show to the user.</summary>
    public string Error { get; init; } = string.Empty;

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}