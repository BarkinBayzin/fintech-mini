using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Payments.Api.BackgroundServices;
using Payments.Api.Contracts;
using Payments.Api.Data;
using Fintech.Contracts;
using Payments.Api.Infrastructure;
using Payments.Api.Models;
using Payments.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8081/realms/fintech";
        options.Audience = "fintech-ui";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new()
        {
            RoleClaimType = ClaimTypes.Role,
            ValidateAudience = false,
            ValidIssuer = "http://localhost:8081/realms/fintech"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity is null)
                {
                    return Task.CompletedTask;
                }

                var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;
                if (string.IsNullOrWhiteSpace(realmAccessClaim))
                {
                    return Task.CompletedTask;
                }

                try
                {
                    using var doc = JsonDocument.Parse(realmAccessClaim);
                    if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
                    {
                        return Task.CompletedTask;
                    }

                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrWhiteSpace(roleName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }
                catch (JsonException)
                {
                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OpsOnly", policy => policy.RequireRole("ops"));
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5672, "/", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });

    });
});

builder.Services.AddHostedService<OutboxPublisherWorker>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId.ToString()
               }))
        {
            await next();
            return;
        }
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

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/payments/intents", async (
    CreatePaymentIntentRequest request,
    HttpContext httpContext,
    PaymentsDbContext dbContext) =>
{
    if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
    {
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });
    }

    var idempotencyKey = keyValues.ToString();
    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });
    }

    PaymentIntent intent;
    try
    {
        intent = PaymentIntent.Create(
            request.MerchantId,
            request.Amount,
            request.Currency,
            request.CustomerId,
            request.Reference);
    }
    catch (DomainValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    var requestHash = RequestHasher.ComputeHash(request);
    var existingRecord = await dbContext.IdempotencyRecords
        .AsNoTracking()
        .FirstOrDefaultAsync(record => record.Key == idempotencyKey);

    if (existingRecord is not null)
    {
        if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Results.Conflict(new { error = "Idempotency key reuse with different request payload." });
        }

        return Results.Content(existingRecord.ResponsePayload, "application/json");
    }

    var response = PaymentIntentResponse.From(intent);
    var responsePayload = RequestHasher.SerializeResponse(response);

    dbContext.PaymentIntents.Add(intent);
    dbContext.IdempotencyRecords.Add(IdempotencyRecord.Create(
        idempotencyKey,
        requestHash,
        responsePayload,
        intent.Id));

    try
    {
        await dbContext.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        var retryRecord = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Key == idempotencyKey);

        if (retryRecord is null)
        {
            throw;
        }

        if (!string.Equals(retryRecord.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Results.Conflict(new { error = "Idempotency key reuse with different request payload." });
        }

        return Results.Content(retryRecord.ResponsePayload, "application/json");
    }

    return Results.Created($"/api/payments/intents/{intent.Id}", response);
})
.WithName("CreatePaymentIntent")
.WithSummary("Create a payment intent with idempotency support.")
.Produces<PaymentIntentResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status409Conflict)
.ProducesValidationProblem();

app.MapPost("/api/payments/intents/{id:guid}/capture", async (
    Guid id,
    HttpContext httpContext,
    PaymentsDbContext dbContext) =>
{
    var intent = await dbContext.PaymentIntents.FirstOrDefaultAsync(item => item.Id == id);
    if (intent is null)
    {
        return Results.NotFound();
    }

    try
    {
        intent.Capture();
    }
    catch (DomainValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    var correlationId = httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var values)
        ? values.ToString()
        : null;

    var payload = new PaymentCaptured
    {
        PaymentIntentId = intent.Id,
        MerchantId = intent.MerchantId,
        Amount = intent.Amount,
        Currency = intent.Currency,
        Reference = intent.Reference,
        CorrelationId = correlationId
    };

    var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    });

    await using var transaction = await dbContext.Database.BeginTransactionAsync();
    dbContext.OutboxMessages.Add(OutboxMessage.Create(
        "PaymentCaptured",
        payloadJson,
        correlationId));

    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(PaymentIntentResponse.From(intent));
})
.WithName("CapturePaymentIntent")
.WithSummary("Capture a payment intent and enqueue an outbox event.")
.Produces<PaymentIntentResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.RequireAuthorization("OpsOnly");

app.MapGet("/api/payments/intents/{id:guid}", async (Guid id, PaymentsDbContext dbContext) =>
{
    var intent = await dbContext.PaymentIntents.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
    return intent is null
        ? Results.NotFound()
        : Results.Ok(PaymentIntentResponse.From(intent));
})
.WithName("GetPaymentIntent")
.WithSummary("Get a payment intent by id.")
.Produces<PaymentIntentResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();
