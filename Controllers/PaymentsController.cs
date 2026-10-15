using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public PaymentsController(RaceDayDbContext db)
    {
        _db = db;
    }
    // >>> GetPayment
    /// <summary>Returns the payment for an enrolment.</summary>
    /// <remarks>Access: the Participant who owns the enrolment, or the Organiser who owns the event. No body.</remarks>
    /// <param name="enrolmentId">The enrolment id.</param>
    /// <response code="200">The payment.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the owner of this enrolment or its event.</response>
    /// <response code="404">Enrolment or payment not found.</response>
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]

    // >>> SubmitPayment
    /// <summary>Pays the entry fee for an enrolment.</summary>
    /// <remarks>
    /// Access: the Participant who owns the enrolment.
    /// Body: amount (must equal the entry fee), paymentMethod and transactionRef (optional). The payment becomes Paid.
    /// </remarks>
    /// <param name="enrolmentId">The enrolment id.</param>
    /// <response code="201">Payment recorded as Paid.</response>
    /// <response code="400">Validation failed, or the amount does not equal the entry fee.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not the owner of this enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Already paid, or the enrolment is cancelled.</response>
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]

    // >>> UpdatePaymentStatus
    /// <summary>Changes a payment's status, for example to issue a refund.</summary>
    /// <remarks>Access: the Organiser who owns the event. Body: paymentStatus (Pending, Paid or Refunded).</remarks>
    /// <param name="id">The payment id.</param>
    /// <response code="200">The updated payment.</response>
    /// <response code="400">The status is not Pending, Paid or Refunded.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Payment not found.</response>
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    // Participant (own) or Organiser (owner).
    [HttpGet("api/enrolments/{enrolmentId:int}/payment")]
    public async Task<IActionResult> GetPayment(int enrolmentId)
    {
        var enrolment = await _db.Enrolments
            .AsNoTracking()
            .Include(en => en.Payment)
            .Include(en => en.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (!User.CanAccess(enrolment))
            return Forbid();

        if (enrolment.Payment is null)
            return NotFound(new { message = "No payment record exists for this enrolment." });

        return Ok(PaymentResponse.From(enrolment.Payment));
    }

    [HttpPost("api/enrolments/{enrolmentId:int}/payment")]
    [Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> SubmitPayment(int enrolmentId, PaymentRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Payment)
            .Include(en => en.Category)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (!User.OwnsEnrolment(enrolment))
            return Forbid();

        if (enrolment.Status != EnrolmentStatuses.Active)
            return Conflict(new { message = "A cancelled enrolment cannot be paid for." });

        var payment = enrolment.Payment;
        if (payment is null)
        {
            payment = new Payment
            {
                EnrolmentId = enrolment.EnrolmentId,
                Amount = enrolment.Category.EntryFee,
                PaymentStatus = PaymentStatuses.Pending
            };
            _db.Payments.Add(payment);
        }

        if (payment.PaymentStatus == PaymentStatuses.Paid)
            return Conflict(new { message = "This enrolment has already been paid." });

        if (request.Amount != payment.Amount)
            return BadRequest(new { message = $"The amount must equal the entry fee of {payment.Amount:0.00}." });

        payment.PaymentMethod = request.PaymentMethod;
        payment.TransactionRef = request.TransactionRef;
        payment.PaymentStatus = PaymentStatuses.Paid;
        payment.PaymentDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayment), new { enrolmentId }, PaymentResponse.From(payment));
    }

    [HttpPut("api/payments/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UpdatePaymentStatus(int id, UpdatePaymentStatusRequest request)
    {
        var payment = await _db.Payments
            .Include(p => p.Enrolment)
            .ThenInclude(en => en.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment is null)
            return NotFound(new { message = "Payment not found." });

        if (payment.Enrolment.Category.Event.OrganiserId != User.GetProfileId())
            return Forbid();

        var status = PaymentStatuses.All.FirstOrDefault(
            s => string.Equals(s, request.PaymentStatus, StringComparison.OrdinalIgnoreCase));

        if (status is null)
            return BadRequest(new { message = $"Payment status must be one of: {string.Join(", ", PaymentStatuses.All)}." });

        payment.PaymentStatus = status;

        if (status == PaymentStatuses.Paid && payment.PaymentDate is null)
            payment.PaymentDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(PaymentResponse.From(payment));
    }
}