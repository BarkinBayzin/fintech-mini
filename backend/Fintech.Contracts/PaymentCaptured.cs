namespace Fintech.Contracts;

public class PaymentCaptured
{
    public Guid PaymentIntentId { get; set; }
    public string MerchantId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string? Reference { get; set; }
    public string? CorrelationId { get; set; }

    public PaymentCaptured() { }
}
