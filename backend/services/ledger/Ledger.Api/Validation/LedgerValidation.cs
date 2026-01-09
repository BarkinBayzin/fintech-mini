using Ledger.Api.Contracts;
using Ledger.Api.Models;

namespace Ledger.Api.Validation;

public static class LedgerValidation
{
    public static Dictionary<string, string[]> Validate(CreateJournalEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.ReferenceId))
        {
            errors["referenceId"] = ["Reference id is required."];
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

        if (request.Lines is null || request.Lines.Count == 0)
        {
            errors["lines"] = ["At least one line is required."];
            return errors;
        }

        decimal debitTotal = 0;
        decimal creditTotal = 0;

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            var lineKey = $"lines[{i}]";

            if (string.IsNullOrWhiteSpace(line.AccountId))
            {
                errors[$"{lineKey}.accountId"] = ["Account id is required."];
            }

            if (line.Amount <= 0)
            {
                errors[$"{lineKey}.amount"] = ["Amount must be greater than zero."];
            }

            if (line.Direction == JournalLineDirection.Debit)
            {
                debitTotal += line.Amount;
            }
            else
            {
                creditTotal += line.Amount;
            }
        }

        if (debitTotal != creditTotal)
        {
            errors["lines"] = ["Total debits must equal total credits."];
        }

        return errors;
    }
}
