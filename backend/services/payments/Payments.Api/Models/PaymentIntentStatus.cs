namespace Payments.Api.Models;

public enum PaymentIntentStatus
{
    Created,
    Authorized,
    Captured,
    Failed,
    Refunded
}
