using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payments.Api.Data;
using Fintech.Contracts;

namespace Payments.Api.BackgroundServices;

public class OutboxPublisherWorker : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publish loop failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var sendEndpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
        var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:ledger-payment-captured"));

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.PublishedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in pendingMessages)
        {
            using var scopeState = string.IsNullOrWhiteSpace(message.CorrelationId)
                ? null
                : _logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = message.CorrelationId
                });

            try
            {
                if (message.Type == "PaymentCaptured")
                {
                    var payload = JsonSerializer.Deserialize<PaymentCaptured>(
                        message.PayloadJson,
                        JsonOptions);

                    if (payload is null)
                    {
                        throw new InvalidOperationException("Outbox payload is invalid.");
                    }

                    await sendEndpoint.Send(payload, sendContext =>
                    {
                        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
                        {
                            sendContext.Headers.Set("X-Correlation-Id", message.CorrelationId);
                        }
                    }, stoppingToken);
                    message.MarkPublished();
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Unknown outbox message type {MessageType}", message.Type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
            }
        }
    }
}
