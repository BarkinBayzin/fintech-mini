using Ledger.Api.Models;

namespace Ledger.Api.Contracts;

public sealed record JournalLineRequest(
    string AccountId,
    JournalLineDirection Direction,
    decimal Amount);

public sealed record CreateJournalEntryRequest(
    string ReferenceId,
    string Currency,
    IReadOnlyList<JournalLineRequest> Lines);

public sealed record JournalLineResponse(
    string AccountId,
    JournalLineDirection Direction,
    decimal Amount);

public sealed record JournalEntryResponse(
    Guid Id,
    string ReferenceId,
    string Currency,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<JournalLineResponse> Lines)
{
    public static JournalEntryResponse From(Models.JournalEntry entry)
    {
        var lines = entry.Lines
            .Select(line => new JournalLineResponse(
                line.AccountId,
                line.Direction,
                line.Amount))
            .ToArray();

        return new JournalEntryResponse(
            entry.Id,
            entry.ReferenceId,
            entry.Currency,
            entry.CreatedAtUtc,
            lines);
    }
}

public sealed record AccountBalanceResponse(
    string AccountId,
    string Currency,
    decimal Balance);
