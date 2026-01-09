using Ledger.Api.Validation;

namespace Ledger.Api.Models;

public class JournalEntry
{
    private readonly List<JournalLine> _lines = [];

    private JournalEntry()
    {
    }

    private JournalEntry(string referenceId, string currency)
    {
        Id = Guid.NewGuid();
        ReferenceId = referenceId;
        Currency = currency;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    public static JournalEntry Create(string referenceId, string currency)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            errors["referenceId"] = ["Reference id is required."];
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

        return new JournalEntry(referenceId.Trim(), normalizedCurrency!.ToUpperInvariant());
    }

    public void AddLine(string accountId, JournalLineDirection direction, decimal amount)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(accountId))
        {
            errors["accountId"] = ["Account id is required."];
        }

        if (amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        _lines.Add(new JournalLine(accountId.Trim(), direction, amount));
    }

    public void ValidateBalanced()
    {
        if (_lines.Count == 0)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["lines"] = ["At least one line is required."]
            });
        }

        var totalDebit = _lines
            .Where(line => line.Direction == JournalLineDirection.Debit)
            .Sum(line => line.Amount);
        var totalCredit = _lines
            .Where(line => line.Direction == JournalLineDirection.Credit)
            .Sum(line => line.Amount);

        if (totalDebit != totalCredit)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["lines"] = ["Total debits must equal total credits."]
            });
        }
    }
}
