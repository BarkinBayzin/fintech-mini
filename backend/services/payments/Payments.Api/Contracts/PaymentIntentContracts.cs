using Payments.Api.Models;

namespace Payments.Api.Contracts;

public sealed record CreatePaymentIntentRequest(
    string MerchantId,
    decimal Amount,
    string Currency,
    string CustomerId,
    string? Reference);

public sealed record PaymentIntentResponse(
    Guid Id,
    string MerchantId,
    decimal Amount,
    string Currency,
    string CustomerId,
    string? Reference,
    PaymentIntentStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static PaymentIntentResponse From(PaymentIntent intent)
    {
        return new PaymentIntentResponse(
            intent.Id,
            intent.MerchantId,
            intent.Amount,
            intent.Currency,
            intent.CustomerId,
            intent.Reference,
            intent.Status,
            intent.CreatedAtUtc,
            intent.UpdatedAtUtc);
    }
}
