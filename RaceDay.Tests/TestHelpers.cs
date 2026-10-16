using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.Tests;

/// <summary>A registered, logged-in user with an HttpClient that already carries their token.</summary>
public record TestUser(HttpClient Client, string Email, int ProfileId);

public static class TestHelpers
{
    public const string Password = "Password123";

    // An https base address stops the app's HTTPS redirect from interfering with the tests.
    public static HttpClient NewClient(RaceDayApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";

    public static object RegisterBody(string email, string role, string? dateOfBirth = "1995-05-15", string password = Password) => new
    {
        email,
        password,
        role,
        firstName = "Test",
        lastName = role,
        phone = "0821234567",
        dateOfBirth
    };

    public static async Task<TestUser> RegisterAndLoginAsync(RaceDayApiFactory factory, string role)
    {
        var client = NewClient(factory);
        var email = UniqueEmail(role.ToLowerInvariant());

        var register = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, role));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        return new TestUser(client, email, auth.User.ProfileId);
    }

    public static object EventBody(string name = "Spring Fun Run", string eventType = "Run",
        string province = "Gauteng", int daysAhead = 30) => new
        {
            eventName = name,
            eventType,
            eventDate = DateTime.Today.AddDays(daysAhead).ToString("yyyy-MM-dd"),
            city = "Johannesburg",
            province,
            venue = "Zoo Lake",
            description = "Test event"
        };

    public static object CategoryBody(string name = "5km", int maxParticipants = 100,
        decimal entryFee = 120m, decimal distanceKm = 5m) => new
        {
            categoryName = name,
            distanceKm,
            entryFee,
            maxParticipants,
            startPoint = "Main gate",
            endPoint = "Main gate",
            elevationGainM = 30,
            mapUrl = "https://example.com/map"
        };

    public static async Task<EventResponse> CreateEventAsync(HttpClient organiser,
        string name = "Spring Fun Run", string province = "Gauteng")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(name: name, province: province));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId,
        int maxParticipants = 100, decimal entryFee = 120m)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            CategoryBody(maxParticipants: maxParticipants, entryFee: entryFee));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<(EventResponse Event, CategoryResponse Category)> CreateEventWithCategoryAsync(
        HttpClient organiser, int maxParticipants = 100)
    {
        var ev = await CreateEventAsync(organiser);
        var category = await CreateCategoryAsync(organiser, ev.EventId, maxParticipants);
        return (ev, category);
    }

    public static async Task<EnrolmentResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }

    // Sends a request with an empty JSON body for POST and PUT. Used for the access-rule tests.
    public static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        return client.SendAsync(request);
    }
}