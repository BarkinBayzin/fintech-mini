using Payments.Api.Validation;

namespace Payments.Api.Models;

public class PaymentIntent
{
    private PaymentIntent()
    {
    }

    private PaymentIntent(
        string merchantId,
        decimal amount,
        string currency,
        string customerId,
        string? reference)
    {
        Id = Guid.NewGuid();
        MerchantId = merchantId;
        Amount = amount;
        Currency = currency;
        CustomerId = customerId;
        Reference = reference;
        Status = PaymentIntentStatus.Created;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public PaymentIntentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PaymentIntent Create(
        string merchantId,
        decimal amount,
        string currency,
        string customerId,
        string? reference)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            errors["merchantId"] = ["Merchant id is required."];
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            errors["customerId"] = ["Customer id is required."];
        }

        if (amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        var normalizedCurrency = currency?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCurrency))
        {
            errors["currency"] = ["Currency is required."];
        }
        else if (normalizedCurrency.Length != 3)
        {
            errors["currency"] = ["Currency must be a 3-letter ISO code."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return new PaymentIntent(
            merchantId.Trim(),
            amount,
            normalizedCurrency!.ToUpperInvariant(),
            customerId.Trim(),
            string.IsNullOrWhiteSpace(reference) ? null : reference.Trim());
    }

    public void Authorize()
    {
        EnsureStatusTransition(PaymentIntentStatus.Created, PaymentIntentStatus.Authorized);
        Status = PaymentIntentStatus.Authorized;
        Touch();
    }

    public void Capture()
    {
        if (Status != PaymentIntentStatus.Authorized && Status != PaymentIntentStatus.Created)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Cannot transition from {Status} to {PaymentIntentStatus.Captured}."]
            });
        }

        Status = PaymentIntentStatus.Captured;
        Touch();
    }

    public void Fail(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["Failure reason is required."]
            });
        }

        if (Status is not (PaymentIntentStatus.Created or PaymentIntentStatus.Authorized))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Cannot fail a payment intent from status {Status}."]
            });
        }

        Status = PaymentIntentStatus.Failed;
        Touch();
    }

    public void Refund(decimal? amount = null)
    {
        if (Status != PaymentIntentStatus.Captured)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Refund is only allowed for captured intents."]
            });
        }

        if (amount.HasValue && amount.Value <= 0)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["amount"] = ["Refund amount must be greater than zero."]
            });
        }

        if (amount.HasValue && amount.Value > Amount)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["amount"] = ["Refund amount cannot exceed original amount."]
            });
        }

        Status = PaymentIntentStatus.Refunded;
        Touch();
    }

    private void EnsureStatusTransition(PaymentIntentStatus expected, PaymentIntentStatus next)
    {
        if (Status != expected)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Cannot transition from {Status} to {next}."]
            });
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
