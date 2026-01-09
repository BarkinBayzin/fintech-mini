namespace Payments.Api.Models;

public class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        string type,
        string payloadJson,
        string? correlationId)
    {
        Id = Guid.NewGuid();
        OccurredAtUtc = DateTimeOffset.UtcNow;
        Type = type;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    public static OutboxMessage Create(string type, string payloadJson, string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("PayloadJson is required.", nameof(payloadJson));
        }

        return new OutboxMessage(type.Trim(), payloadJson, correlationId);
    }

    public void MarkPublished()
    {
        if (PublishedAtUtc is null)
        {
            PublishedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
