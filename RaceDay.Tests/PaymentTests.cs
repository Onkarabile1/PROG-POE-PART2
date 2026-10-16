using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class PaymentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public PaymentTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private Task<TestUser> NewUserAsync(string role) => TestHelpers.RegisterAndLoginAsync(_factory, role);

    // An organiser, an event with a 120.00 entry fee, and a participant who has enrolled.
    private async Task<(TestUser Organiser, TestUser Participant, EnrolmentResponse Enrolment)> SetUpAsync()
    {
        var organiser = await NewUserAsync(Roles.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser.Client);
        var participant = await NewUserAsync(Roles.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, category.CategoryId);
        return (organiser, participant, enrolment);
    }

    private static object Payment(decimal amount) =>
        new { amount, paymentMethod = "Card", transactionRef = "TXN-001" };

    [Fact]
    public async Task SubmitPayment_WithTheExactFee_ReturnsCreated_AsPaid()
    {
        var (_, participant, enrolment) = await SetUpAsync();

        var response = await participant.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(120m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(PaymentStatuses.Paid, payment!.PaymentStatus);
        Assert.Equal(120m, payment.Amount);
        Assert.NotNull(payment.PaymentDate);
    }

    [Fact]
    public async Task SubmitPayment_WithTheWrongAmount_ReturnsBadRequest()
    {
        var (_, participant, enrolment) = await SetUpAsync();

        var response = await participant.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(50m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitPayment_Twice_ReturnsConflict()
    {
        var (_, participant, enrolment) = await SetUpAsync();
        await participant.Client.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(120m));

        var second = await participant.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(120m));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task SubmitPayment_ForSomeoneElsesEnrolment_ReturnsForbidden()
    {
        var (_, _, enrolment) = await SetUpAsync();
        var stranger = await NewUserAsync(Roles.Participant);

        var response = await stranger.Client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(120m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_IsVisibleToTheParticipantAndTheOwningOrganiser_ButNotToAnotherOrganiser()
    {
        var (organiser, participant, enrolment) = await SetUpAsync();
        var otherOrganiser = await NewUserAsync(Roles.Organiser);
        var url = $"/api/enrolments/{enrolment.EnrolmentId}/payment";

        Assert.Equal(HttpStatusCode.OK, (await participant.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organiser.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task UpdatePaymentStatus_RefundByTheOwningOrganiser_ReturnsOk()
    {
        var (organiser, participant, enrolment) = await SetUpAsync();
        await participant.Client.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/payment", Payment(120m));
        var payment = await participant.Client.GetFromJsonAsync<PaymentResponse>(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment");

        var response = await organiser.Client.PutAsJsonAsync($"/api/payments/{payment!.PaymentId}",
            new { paymentStatus = "Refunded" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(PaymentStatuses.Refunded, updated!.PaymentStatus);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ByAnotherOrganiser_ReturnsForbidden_AndAnUnknownStatusIsRejected()
    {
        var (organiser, participant, enrolment) = await SetUpAsync();
        var otherOrganiser = await NewUserAsync(Roles.Organiser);
        var payment = await participant.Client.GetFromJsonAsync<PaymentResponse>(
            $"/api/enrolments/{enrolment.EnrolmentId}/payment");
        var url = $"/api/payments/{payment!.PaymentId}";

        var byOther = await otherOrganiser.Client.PutAsJsonAsync(url, new { paymentStatus = "Refunded" });
        var badStatus = await organiser.Client.PutAsJsonAsync(url, new { paymentStatus = "Nonsense" });

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
    }
}