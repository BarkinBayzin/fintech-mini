using Payments.Api.Contracts;

namespace Payments.Api.Validation;

public static class PaymentIntentValidation
{
    public static Dictionary<string, string[]> Validate(CreatePaymentIntentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.MerchantId))
        {
            errors["merchantId"] = ["Merchant id is required."];
        }

        if (request.Amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        var currency = request.Currency?.Trim();

        if (string.IsNullOrWhiteSpace(currency))
        {
            errors["currency"] = ["Currency is required."];
        }
        else if (currency.Length != 3)
        {
            errors["currency"] = ["Currency must be a 3-letter ISO code."];
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            errors["customerId"] = ["Customer id is required."];
        }

        return errors;
    }
}
