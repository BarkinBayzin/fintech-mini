using Microsoft.EntityFrameworkCore;
using Payments.Api.Models;

namespace Payments.Api.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentIntent>(builder =>
        {
            builder.HasKey(intent => intent.Id);
            builder.Property(intent => intent.MerchantId).HasMaxLength(100).IsRequired();
            builder.Property(intent => intent.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(intent => intent.Currency).HasMaxLength(3).IsRequired();
            builder.Property(intent => intent.CustomerId).HasMaxLength(100).IsRequired();
            builder.Property(intent => intent.Reference).HasMaxLength(200);
            builder.Property(intent => intent.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(intent => intent.CreatedAtUtc).IsRequired();
            builder.Property(intent => intent.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.HasKey(record => record.Id);
            builder.Property(record => record.Key).HasMaxLength(200).IsRequired();
            builder.Property(record => record.RequestHash).HasMaxLength(64).IsRequired();
            builder.Property(record => record.ResponsePayload).HasColumnType("jsonb").IsRequired();
            builder.Property(record => record.CreatedAtUtc).IsRequired();
            builder.HasIndex(record => record.Key).IsUnique();
            builder.HasOne(record => record.PaymentIntent)
                .WithMany()
                .HasForeignKey(record => record.PaymentIntentId);
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
            builder.Property(message => message.PayloadJson).HasColumnType("jsonb").IsRequired();
            builder.Property(message => message.OccurredAtUtc).IsRequired();
            builder.Property(message => message.PublishedAtUtc);
            builder.Property(message => message.CorrelationId).HasMaxLength(200);
            builder.HasIndex(message => message.OccurredAtUtc);
        });
    }
}
