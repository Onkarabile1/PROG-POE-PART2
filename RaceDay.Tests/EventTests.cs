using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private Task<TestUser> NewUserAsync(string role) => TestHelpers.RegisterAndLoginAsync(_factory, role);

    // ---------- Viewing ----------

    [Fact]
    public async Task GetEvents_WithoutToken_ReturnsOk()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ReturnsTheEventWithItsCategories()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (ev, _) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);

        var client = TestHelpers.NewClient(_factory);
        var response = await client.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loaded = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(ev.EventName, loaded!.EventName);
        var category = Assert.Single(loaded.Categories!);
        Assert.Equal("5km", category.CategoryName);
    }

    [Fact]
    public async Task GetEvent_Unknown_ReturnsNotFound()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_FilteredByProvince_ReturnsOnlyEventsInThatProvince()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var province = $"Province-{Guid.NewGuid():N}";
        var inProvince = await TestHelpers.CreateEventAsync(organiser.Client, province: province);
        await TestHelpers.CreateEventAsync(organiser.Client, province: "Gauteng");

        var client = TestHelpers.NewClient(_factory);
        var events = await client.GetFromJsonAsync<List<EventResponse>>($"/api/events?province={province}");

        var only = Assert.Single(events!);
        Assert.Equal(inProvince.EventId, only.EventId);
    }

    // ---------- Creating ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated_AndIsOwnedByThatOrganiser()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.True(created!.EventId > 0);
        Assert.Equal(organiser.ProfileId, created.OrganiserId);
        Assert.Equal("Run", created.EventType);
    }

    [Theory]
    [InlineData("Run")]
    [InlineData("Walk")]
    [InlineData("Cycle")]
    public async Task CreateEvent_WithEachAllowedType_ReturnsCreated(string eventType)
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events",
            TestHelpers.EventBody(eventType: eventType));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Marathon")]
    [InlineData("run")]
    [InlineData("")]
    public async Task CreateEvent_WithInvalidType_ReturnsBadRequest(string eventType)
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events",
            TestHelpers.EventBody(eventType: eventType));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutAName_ReturnsBadRequest()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.EventBody(name: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutADate_ReturnsBadRequest()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events",
            new { eventName = "No date", eventType = "Run" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Updating ----------

    [Fact]
    public async Task UpdateEvent_ByTheOwner_ReturnsOkWithTheNewValues()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}",
            TestHelpers.EventBody(name: "Renamed Walk", eventType: "Walk"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Walk", updated!.EventName);
        Assert.Equal("Walk", updated.EventType);
    }

    [Fact]
    public async Task UpdateEvent_ByAnotherOrganiser_ReturnsForbidden_AndLeavesTheEventUnchanged()
    {
        var owner = await NewUserAsync(Roles.Organiser);
        var other = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner.Client, name: "Original Name");

        var response = await other.Client.PutAsJsonAsync($"/api/events/{ev.EventId}",
            TestHelpers.EventBody(name: "Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var reloaded = await TestHelpers.NewClient(_factory).GetFromJsonAsync<EventResponse>($"/api/events/{ev.EventId}");
        Assert.Equal("Original Name", reloaded!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_Unknown_ReturnsNotFound()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PutAsJsonAsync("/api/events/999999", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}",
            TestHelpers.EventBody(eventType: "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Deleting ----------

    [Fact]
    public async Task DeleteEvent_ByTheOwner_ReturnsOk_AndTheEventIsGone()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser.Client);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await TestHelpers.NewClient(_factory).GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByAnotherOrganiser_ReturnsForbidden_AndTheEventRemains()
    {
        var owner = await NewUserAsync(Roles.Organiser);
        var other = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner.Client);

        var delete = await other.Client.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await TestHelpers.NewClient(_factory).GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterwards.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ThatHasEnrolments_ReturnsConflict()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var participant = await NewUserAsync(Roles.Participant);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);
        await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var response = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_Unknown_ReturnsNotFound()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.DeleteAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}