using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class RoleAccessTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public RoleAccessTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // Every endpoint that needs a login must answer 401 when no token is sent.
    [Theory]
    [InlineData("GET", "/api/users/me")]
    [InlineData("PUT", "/api/users/me")]
    [InlineData("PUT", "/api/users/me/password")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("POST", "/api/categories/1/enrolments")]
    [InlineData("GET", "/api/users/me/enrolments")]
    [InlineData("GET", "/api/categories/1/enrolments")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("GET", "/api/enrolments/1/payment")]
    [InlineData("POST", "/api/enrolments/1/payment")]
    [InlineData("PUT", "/api/payments/1")]
    [InlineData("POST", "/api/enrolments/1/result")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("GET", "/api/users/me/results")]
    [InlineData("POST", "/api/sponsors")]
    [InlineData("PUT", "/api/sponsors/1")]
    [InlineData("POST", "/api/events/1/sponsors")]
    [InlineData("DELETE", "/api/events/1/sponsors/1")]
    [InlineData("POST", "/api/events/1/weather/refresh")]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string method, string url)
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await TestHelpers.SendAsync(client, method, url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Organiser-only endpoints must refuse a logged-in Participant.
    [Theory]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("GET", "/api/categories/1/enrolments")]
    [InlineData("POST", "/api/enrolments/1/result")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("PUT", "/api/payments/1")]
    [InlineData("POST", "/api/sponsors")]
    [InlineData("PUT", "/api/sponsors/1")]
    [InlineData("POST", "/api/events/1/sponsors")]
    [InlineData("DELETE", "/api/events/1/sponsors/1")]
    [InlineData("POST", "/api/events/1/weather/refresh")]
    public async Task OrganiserOnlyEndpoint_AsParticipant_ReturnsForbidden(string method, string url)
    {
        var participant = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Participant);

        var response = await TestHelpers.SendAsync(participant.Client, method, url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Participant-only endpoints must refuse a logged-in Organiser.
    [Theory]
    [InlineData("POST", "/api/categories/1/enrolments")]
    [InlineData("GET", "/api/users/me/enrolments")]
    [InlineData("GET", "/api/users/me/results")]
    [InlineData("POST", "/api/enrolments/1/payment")]
    public async Task ParticipantOnlyEndpoint_AsOrganiser_ReturnsForbidden(string method, string url)
    {
        var organiser = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Organiser);

        var response = await TestHelpers.SendAsync(organiser.Client, method, url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Public endpoints must work without logging in.
    [Theory]
    [InlineData("/api/events", HttpStatusCode.OK)]
    [InlineData("/api/sponsors", HttpStatusCode.OK)]
    [InlineData("/api/events/999999", HttpStatusCode.NotFound)]
    [InlineData("/api/events/999999/categories", HttpStatusCode.NotFound)]
    [InlineData("/api/categories/999999", HttpStatusCode.NotFound)]
    [InlineData("/api/categories/999999/results", HttpStatusCode.NotFound)]
    [InlineData("/api/events/999999/sponsors", HttpStatusCode.NotFound)]
    [InlineData("/api/events/999999/weather", HttpStatusCode.NotFound)]
    [InlineData("/api/events/999999/weather/history", HttpStatusCode.NotFound)]
    public async Task PublicEndpoint_WithoutToken_IsNotBlocked(string url, HttpStatusCode expected)
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync(url);

        Assert.Equal(expected, response.StatusCode);
    }

    // A token signed with a different key must be rejected, even if it claims to be an Organiser.
    [Fact]
    public async Task ForgedToken_ClaimingToBeAnOrganiser_ReturnsUnauthorized()
    {
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-signing-key-1234567890"));
        var forged = new JwtSecurityToken(
            issuer: "RaceDay.API",
            audience: "RaceDay.Client",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, Roles.Organiser),
                new Claim("profileId", "1")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256));

        var client = TestHelpers.NewClient(_factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(forged));

        var response = await TestHelpers.SendAsync(client, "POST", "/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}