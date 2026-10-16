using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ResultTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private Task<TestUser> NewUserAsync(string role) => TestHelpers.RegisterAndLoginAsync(_factory, role);

    private async Task<(TestUser Organiser, CategoryResponse Category, TestUser Participant, EnrolmentResponse Enrolment)>
        SetUpAsync()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);
        return (organiser, category, participant, enrolment);
    }

    private static object Finished(int position, string time = "00:27:45") =>
        new { finishTime = time, position, status = "Finished" };

    [Fact]
    public async Task CaptureResult_ByTheOwningOrganiser_ReturnsCreated()
    {
        var (organiser, _, _, enrolment) = await SetUpAsync();

        var response = await organiser.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(1, result!.Position);
        Assert.Equal(ResultStatuses.Finished, result.Status);
        Assert.Equal(new TimeSpan(0, 27, 45), result.FinishTime);
        Assert.Equal(organiser.ProfileId, result.CapturedByOrganiserId);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiser, _, _, enrolment) = await SetUpAsync();
        await organiser.Client.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        var second = await organiser.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(2));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ByAnotherOrganiser_ReturnsForbidden()
    {
        var (_, _, _, enrolment) = await SetUpAsync();
        var otherOrganiser = await NewUserAsync(Roles.Organiser);

        var response = await otherOrganiser.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutAFinishTime_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpAsync();

        var response = await organiser.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", new { position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithAPositionAlreadyTaken_ReturnsConflict()
    {
        var (organiser, category, _, firstEnrolment) = await SetUpAsync();
        var secondParticipant = await NewUserAsync(Roles.Participant);
        var secondEnrolment = await TestHelpers.EnrolAsync(secondParticipant.Client, category.CategoryId);
        await organiser.Client.PostAsJsonAsync($"/api/enrolments/{firstEnrolment.EnrolmentId}/result", Finished(1));

        var response = await organiser.Client.PostAsJsonAsync(
            $"/api/enrolments/{secondEnrolment.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_ShowsTheParticipantsOwnResult()
    {
        var (organiser, _, participant, enrolment) = await SetUpAsync();
        await organiser.Client.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        var mine = await participant.Client.GetFromJsonAsync<List<ResultResponse>>("/api/users/me/results");

        var result = Assert.Single(mine!);
        Assert.Equal(enrolment.EnrolmentId, result.EnrolmentId);
    }

    [Fact]
    public async Task Leaderboard_IsPublic_AndOrderedByPosition()
    {
        var (organiser, category, _, firstEnrolment) = await SetUpAsync();
        var secondParticipant = await NewUserAsync(Roles.Participant);
        var secondEnrolment = await TestHelpers.EnrolAsync(secondParticipant.Client, category.CategoryId);

        // Capture the runner-up first, so ordering has to come from the position, not the capture order.
        await organiser.Client.PostAsJsonAsync($"/api/enrolments/{firstEnrolment.EnrolmentId}/result", Finished(2, "00:30:10"));
        await organiser.Client.PostAsJsonAsync($"/api/enrolments/{secondEnrolment.EnrolmentId}/result", Finished(1, "00:26:59"));

        var client = TestHelpers.NewClient(_factory);
        var board = await client.GetFromJsonAsync<List<ResultResponse>>($"/api/categories/{category.CategoryId}/results");

        Assert.Equal(new int?[] { 1, 2 }, board!.Select(r => r.Position).ToArray());
    }
}