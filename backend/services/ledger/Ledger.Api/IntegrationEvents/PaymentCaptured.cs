using System.Text.Json.Serialization;

namespace Ledger.Api.IntegrationEvents;

public class PaymentCaptured
{
    [JsonPropertyName("paymentIntentId")]
    public Guid PaymentIntentId { get; set; }

    [JsonPropertyName("merchantId")]
    public string MerchantId { get; set; } = default!;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    public PaymentCaptured() { }
}
