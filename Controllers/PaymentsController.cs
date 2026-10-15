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