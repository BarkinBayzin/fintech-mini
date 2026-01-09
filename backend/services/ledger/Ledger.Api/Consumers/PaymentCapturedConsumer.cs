using Ledger.Api.Data;
using Ledger.Api.Models;
using Ledger.Api.Validation;
using Fintech.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Consumers;

public sealed class PaymentCapturedConsumer : IConsumer<PaymentCaptured>
{
    private readonly LedgerDbContext _dbContext;
    private readonly ILogger<PaymentCapturedConsumer> _logger;

    public PaymentCapturedConsumer(LedgerDbContext dbContext, ILogger<PaymentCapturedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCaptured> context)
    {
         _logger.LogInformation("PaymentCaptured received: PaymentIntentId={PaymentIntentId}, MerchantId={MerchantId}, Amount={Amount}, Currency={Currency}, CorrelationId={CorrelationId}",
            context.Message.PaymentIntentId, context.Message.MerchantId, context.Message.Amount, context.Message.Currency,
            context.Headers.TryGetHeader("X-Correlation-Id", out var cid) ? cid?.ToString() : null);
            
        _logger.LogInformation(
            "Received PaymentCaptured PaymentIntentId {PaymentIntentId} MerchantId {MerchantId} Amount {Amount} Currency {Currency}",
            context.Message.PaymentIntentId,
            context.Message.MerchantId,
            context.Message.Amount,
            context.Message.Currency);

        var correlationId = context.Message.CorrelationId
            ?? context.Headers.Get<string>("X-Correlation-Id")
            ?? context.CorrelationId?.ToString();

        _logger.LogInformation(
            "Received PaymentCaptured for ReferenceId {ReferenceId} CorrelationId {CorrelationId}",
            context.Message.PaymentIntentId,
            correlationId ?? "(none)");

        using var scope = string.IsNullOrWhiteSpace(correlationId)
            ? null
            : _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

        var referenceId = context.Message.PaymentIntentId.ToString();
        var exists = await _dbContext.JournalEntries
            .AsNoTracking()
            .AnyAsync(entry => entry.ReferenceId == referenceId, context.CancellationToken);

        if (exists)
        {
            _logger.LogInformation("Journal entry already exists for ReferenceId {ReferenceId}", referenceId);
            return;
        }

        try
        {
            var entry = JournalEntry.Create(referenceId, context.Message.Currency);
            entry.AddLine($"merchant:{context.Message.MerchantId}:cash", JournalLineDirection.Debit, context.Message.Amount);
            entry.AddLine("clearing:payments", JournalLineDirection.Credit, context.Message.Amount);
            entry.ValidateBalanced();

            _dbContext.JournalEntries.Add(entry);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DomainValidationException ex)
        {
            _logger.LogError("Invalid PaymentCaptured message: {Errors}", ex.Errors);
        }
    }
}
