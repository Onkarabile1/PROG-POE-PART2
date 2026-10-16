using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Organiser_ReturnsCreated_AndDoesNotExposeThePassword()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("organiser");

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(email, Roles.Organiser));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, user!.Email);
        Assert.Equal(Roles.Organiser, user.Role);
    }

    [Fact]
    public async Task Register_Participant_ReturnsCreated_WithDateOfBirth()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("participant");

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(email, Roles.Participant, dateOfBirth: "1990-03-21"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(Roles.Participant, user!.Role);
        Assert.Equal(new DateOnly(1990, 3, 21), user.DateOfBirth);
    }

    [Fact]
    public async Task Register_StoresAHashedPassword_NotThePlainText()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("hashed");

        await client.PostAsJsonAsync("/api/auth/register", TestHelpers.RegisterBody(email, Roles.Organiser));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Users.Single(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(stored.PasswordHash));
        Assert.NotEqual(TestHelpers.Password, stored.PasswordHash);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(TestHelpers.UniqueEmail("nodob"), Roles.Participant, dateOfBirth: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(TestHelpers.UniqueEmail("admin"), "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordShorterThanEightCharacters_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(TestHelpers.UniqueEmail("short"), Roles.Organiser, password: "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody("not-an-email", Roles.Organiser));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailTwice_ReturnsConflict_EvenWithDifferentCapitals()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("duplicate");

        var first = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.RegisterBody(email, Roles.Organiser));
        var second = await client.PostAsJsonAsync("/api/auth/register",
            TestHelpers.RegisterBody(email.ToUpperInvariant(), Roles.Participant));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ---------- Login ----------

    [Theory]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser(string role)
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("login");
        await client.PostAsJsonAsync("/api/auth/register", TestHelpers.RegisterBody(email, role));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(email, auth.User.Email);
        Assert.Equal(role, auth.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("wrongpass");
        await client.PostAsJsonAsync("/api/auth/register", TestHelpers.RegisterBody(email, Roles.Organiser));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail("nobody"), password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_GivesTheSameErrorForWrongPasswordAndUnknownEmail()
    {
        var client = TestHelpers.NewClient(_factory);
        var email = TestHelpers.UniqueEmail("samemessage");
        await client.PostAsJsonAsync("/api/auth/register", TestHelpers.RegisterBody(email, Roles.Organiser));

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail("ghost"), password = "WrongPassword1" });

        Assert.Equal(await unknownEmail.Content.ReadAsStringAsync(),
                     await wrongPassword.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithoutPassword_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "someone@test.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Using the token ----------

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithGarbageToken_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsTheLoggedInUsersProfile()
    {
        var participant = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Participant);

        var response = await participant.Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(participant.Email, me!.Email);
        Assert.Equal(Roles.Participant, me.Role);
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheLoggedInUsersName()
    {
        var organiser = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Organiser);

        var response = await organiser.Client.PutAsJsonAsync("/api/users/me",
            new { firstName = "Updated", lastName = "Name", phone = "0831112222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Updated", updated!.FirstName);
        Assert.Equal("Name", updated.LastName);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsUnauthorized()
    {
        var user = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Participant);

        var response = await user.Client.PutAsJsonAsync("/api/users/me/password",
            new { currentPassword = "NotMyPassword1", newPassword = "BrandNewPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_OnlyTheNewPasswordWorksAfterwards()
    {
        var user = await TestHelpers.RegisterAndLoginAsync(_factory, Roles.Participant);

        var change = await user.Client.PutAsJsonAsync("/api/users/me/password",
            new { currentPassword = TestHelpers.Password, newPassword = "BrandNewPassword1" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var client = TestHelpers.NewClient(_factory);
        var withNew = await client.PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = "BrandNewPassword1" });
        var withOld = await client.PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }
}