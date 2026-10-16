using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class CategoryTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public CategoryTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private Task<TestUser> NewUserAsync(string role) => TestHelpers.RegisterAndLoginAsync(_factory, role);

    [Fact]
    public async Task CreateCategory_ByTheEventOwner_ReturnsCreated_WithRouteDetails()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            TestHelpers.CategoryBody(name: "10km", distanceKm: 10m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal(ev.EventId, category!.EventId);
        Assert.Equal("10km", category.CategoryName);
        Assert.Equal(10m, category.DistanceKm);
        Assert.Equal("Main gate", category.StartPoint);
        Assert.Equal("https://example.com/map", category.MapUrl);
    }

    [Fact]
    public async Task CreateCategory_ByAnotherOrganiser_ReturnsForbidden()
    {
        var owner = await NewUserAsync(Roles.Organiser);
        var other = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner.Client);

        var response = await other.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            TestHelpers.CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ForAnUnknownEvent_ReturnsNotFound()
    {
        var organiser = await NewUserAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events/999999/categories",
            TestHelpers.CategoryBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithZeroDistance_ReturnsBadRequest()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            TestHelpers.CategoryBody(distanceKm: 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoriesForEvent_WithoutToken_ReturnsThem()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);

        var client = TestHelpers.NewClient(_factory);
        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/events/{ev.EventId}/categories");

        var only = Assert.Single(categories!);
        Assert.Equal(category.CategoryId, only.CategoryId);
    }

    [Fact]
    public async Task UpdateCategory_LoweringTheLimitBelowTheNumberEnrolled_ReturnsConflict()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client, maxParticipants: 2);

        var first = await NewUserAsync(Roles.Participant);
        var second = await NewUserAsync(Roles.Participant);
        await TestHelpers.EnrolAsync(first.Client, category.CategoryId);
        await TestHelpers.EnrolAsync(second.Client, category.CategoryId);

        var response = await organiser.Client.PutAsJsonAsync($"/api/categories/{category.CategoryId}",
            TestHelpers.CategoryBody(maxParticipants: 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithAnActiveEnrolment_ReturnsConflict()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var participant = await NewUserAsync(Roles.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);
        await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var response = await organiser.Client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithNoEnrolments_ReturnsOk_AndTheCategoryIsGone()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);

        var delete = await organiser.Client.DeleteAsync($"/api/categories/{category.CategoryId}");
        var afterwards = await TestHelpers.NewClient(_factory).GetAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }
}