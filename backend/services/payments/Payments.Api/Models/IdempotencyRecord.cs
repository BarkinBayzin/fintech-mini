namespace Payments.Api.Models;

public class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    private IdempotencyRecord(
        string key,
        string requestHash,
        string responsePayload,
        Guid paymentIntentId)
    {
        Id = Guid.NewGuid();
        Key = key;
        RequestHash = requestHash;
        ResponsePayload = responsePayload;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        PaymentIntentId = paymentIntentId;
    }

    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string ResponsePayload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid PaymentIntentId { get; private set; }
    public PaymentIntent? PaymentIntent { get; private set; }

    public static IdempotencyRecord Create(
        string key,
        string requestHash,
        string responsePayload,
        Guid paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        return new IdempotencyRecord(key, requestHash, responsePayload, paymentIntentId);
    }
}
