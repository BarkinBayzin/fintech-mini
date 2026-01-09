using System.Text.Json;
using System.Text.Json.Serialization;
using Ledger.Api.Consumers;
using Ledger.Api.Contracts;
using Ledger.Api.Data;
using Ledger.Api.Models;
using Microsoft.EntityFrameworkCore;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LedgerDb")));

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentCapturedConsumer>();

    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5672, "/", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });

        cfg.UseJsonSerializer();
        cfg.ReceiveEndpoint("ledger-payment-captured", endpoint =>
        {
            endpoint.ConfigureConsumer<PaymentCapturedConsumer>(context);
        });
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200" // Angular
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) &&
        !string.IsNullOrWhiteSpace(correlationId))
    {
        logger.LogInformation("CorrelationId {CorrelationId}", correlationId.ToString());
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapPost("/api/ledger/journal-entries", async (
    CreateJournalEntryRequest request,
    LedgerDbContext dbContext) =>
{
    JournalEntry entry;
    try
    {
        entry = JournalEntry.Create(request.ReferenceId, request.Currency);

        if (request.Lines is null)
        {
            throw new Ledger.Api.Validation.DomainValidationException(
                new Dictionary<string, string[]> { ["lines"] = ["At least one line is required."] });
        }

        foreach (var line in request.Lines)
        {
            entry.AddLine(line.AccountId, line.Direction, line.Amount);
        }

        entry.ValidateBalanced();
    }
    catch (Ledger.Api.Validation.DomainValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    dbContext.JournalEntries.Add(entry);

    try
    {
        await dbContext.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new { error = "ReferenceId already exists." });
    }

    return Results.Created($"/api/ledger/journal-entries/{entry.Id}", JournalEntryResponse.From(entry));
})
.WithName("CreateJournalEntry")
.WithSummary("Create a journal entry with balanced debit and credit lines.")
.Produces<JournalEntryResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict)
.ProducesValidationProblem();

app.MapGet("/api/ledger/accounts/{accountId}/balance", async (
    string accountId,
    string? currency,
    LedgerDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
    {
        return Results.BadRequest(new { error = "currency query parameter is required and must be a 3-letter code." });
    }

    var normalizedCurrency = currency.Trim().ToUpperInvariant();
    var normalizedAccountId = accountId.Trim();

    var balance = await dbContext.JournalLines
        .AsNoTracking()
        .Where(line =>
            line.AccountId == normalizedAccountId &&
            line.JournalEntry != null &&
            line.JournalEntry.Currency == normalizedCurrency)
        .Select(line => line.Direction == JournalLineDirection.Credit ? line.Amount : -line.Amount)
        .SumAsync();

    return Results.Ok(new AccountBalanceResponse(normalizedAccountId, normalizedCurrency, balance));
})
.WithName("GetAccountBalance")
.WithSummary("Get the account balance for a specific currency.")
.Produces<AccountBalanceResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.Run();
