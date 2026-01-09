namespace Ledger.Api.Models;

public class JournalLine
{
    private JournalLine()
    {
    }

    internal JournalLine(string accountId, JournalLineDirection direction, decimal amount)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Direction = direction;
        Amount = amount;
    }

    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public JournalEntry? JournalEntry { get; private set; }
    public string AccountId { get; private set; } = string.Empty;
    public JournalLineDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
}
