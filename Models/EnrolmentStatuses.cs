namespace RaceDay.API.Models;

public static class EnrolmentStatuses
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Refunded = "Refunded";

    public static readonly string[] All = { Pending, Paid, Refunded };
}