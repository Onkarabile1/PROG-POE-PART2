using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class PaymentRequest
{
    [Range(0, 100000)]
    public decimal Amount { get; set; }

    [Required, MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TransactionRef { get; set; }
}

public class UpdatePaymentStatusRequest
{
    [Required]
    public string PaymentStatus { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int EnrolmentId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public DateTime? PaymentDate { get; set; }

    public static PaymentResponse From(Payment p) => new()
    {
        PaymentId = p.PaymentId,
        EnrolmentId = p.EnrolmentId,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod,
        PaymentStatus = p.PaymentStatus,
        TransactionRef = p.TransactionRef,
        PaymentDate = p.PaymentDate
    };
}