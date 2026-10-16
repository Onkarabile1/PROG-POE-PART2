using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private Task<TestUser> NewUserAsync(string role) => TestHelpers.RegisterAndLoginAsync(_factory, role);

    private async Task<(TestUser Organiser, EventResponse Event, CategoryResponse Category)> NewEventWithCategoryAsync(
        int maxParticipants = 100)
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client, maxParticipants);
        return (organiser, ev, category);
    }

    // ---------- Enrolling ----------

    [Fact]
    public async Task Enrol_AsParticipant_RecordsTheParticipantTheEventAndTheCategory()
    {
        var (_, ev, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);

        var response = await participant.Client.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participant.ProfileId, enrolment!.ParticipantId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal(EnrolmentStatuses.Active, enrolment.Status);
        Assert.StartsWith($"E{ev.EventId}-", enrolment.BibNumber);
        Assert.Equal(PaymentStatuses.Pending, enrolment.PaymentStatus);
    }

    [Fact]
    public async Task Enrolment_AppearsInTheParticipantsOwnList()
    {
        var (_, ev, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var mine = await participant.Client.GetFromJsonAsync<List<EnrolmentResponse>>("/api/users/me/enrolments");

        var found = Assert.Single(mine!);
        Assert.Equal(enrolment.EnrolmentId, found.EnrolmentId);
        Assert.Equal(ev.EventName, found.EventName);
        Assert.Equal(category.CategoryName, found.CategoryName);
    }

    [Fact]
    public async Task Enrolment_IsVisibleToTheOrganiserWhoOwnsTheEvent()
    {
        var (organiser, _, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var list = await organiser.Client.GetFromJsonAsync<List<EnrolmentResponse>>(
            $"/api/categories/{category.CategoryId}/enrolments");

        Assert.Contains(list!, e => e.EnrolmentId == enrolment.EnrolmentId && e.ParticipantId == participant.ProfileId);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameCategory_ReturnsConflict()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var second = await participant.Client.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenTheCategoryIsFull_ReturnsConflict()
    {
        var (_, _, category) = await NewEventWithCategoryAsync(maxParticipants: 1);
        var first = await NewUserAsync(Roles.Participant);
        var second = await NewUserAsync(Roles.Participant);
        await TestHelpers.EnrolAsync(first.Client, category.CategoryId);

        var response = await second.Client.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnUnknownCategory_ReturnsNotFound()
    {
        var participant = await NewUserAsync(Roles.Participant);

        var response = await participant.Client.PostAsync("/api/categories/999999/enrolments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_TwoParticipants_GetDifferentBibNumbers()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var first = await NewUserAsync(Roles.Participant);
        var second = await NewUserAsync(Roles.Participant);

        var firstEnrolment = await TestHelpers.EnrolAsync(first.Client, category.CategoryId);
        var secondEnrolment = await TestHelpers.EnrolAsync(second.Client, category.CategoryId);

        Assert.NotEqual(firstEnrolment.BibNumber, secondEnrolment.BibNumber);
    }

    [Fact]
    public async Task ListEnrolments_ByAnOrganiserWhoDoesNotOwnTheEvent_ReturnsForbidden()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var otherOrganiser = await NewUserAsync(Roles.Organiser);

        var response = await otherOrganiser.Client.GetAsync($"/api/categories/{category.CategoryId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Cancelling ----------

    [Fact]
    public async Task Cancel_OwnEnrolment_ReturnsOk_AndTheStatusBecomesCancelled()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var cancel = await participant.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var mine = await participant.Client.GetFromJsonAsync<List<EnrolmentResponse>>("/api/users/me/enrolments");
        Assert.Equal(EnrolmentStatuses.Cancelled, Assert.Single(mine!).Status);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_ReturnsForbidden()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var owner = await NewUserAsync(Roles.Participant);
        var stranger = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(owner.Client, category.CategoryId);

        var response = await stranger.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByTheOrganiserWhoOwnsTheEvent_ReturnsOk()
    {
        var (organiser, _, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var response = await organiser.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByAnOrganiserWhoDoesNotOwnTheEvent_ReturnsForbidden()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var otherOrganiser = await NewUserAsync(Roles.Organiser);
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        var response = await otherOrganiser.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Unknown_ReturnsNotFound()
    {
        var participant = await NewUserAsync(Roles.Participant);

        var response = await participant.Client.DeleteAsync("/api/enrolments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancelling_FreesASpotInAFullCategory()
    {
        var (_, _, category) = await NewEventWithCategoryAsync(maxParticipants: 1);
        var first = await NewUserAsync(Roles.Participant);
        var second = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(first.Client, category.CategoryId);
        await first.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        var response = await second.Client.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task EnrolAgainAfterCancelling_ReactivatesTheSameEnrolment()
    {
        var (_, _, category) = await NewEventWithCategoryAsync();
        var participant = await NewUserAsync(Roles.Participant);
        var original = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);
        await participant.Client.DeleteAsync($"/api/enrolments/{original.EnrolmentId}");

        var again = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);

        Assert.Equal(original.EnrolmentId, again.EnrolmentId);
        Assert.Equal(original.BibNumber, again.BibNumber);
        Assert.Equal(EnrolmentStatuses.Active, again.Status);
    }
}