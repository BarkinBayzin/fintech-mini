using Ledger.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Data;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JournalEntry>(builder =>
        {
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.ReferenceId).HasMaxLength(200).IsRequired();
            builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();
            builder.Property(entry => entry.CreatedAtUtc).IsRequired();
            builder.HasIndex(entry => entry.ReferenceId).IsUnique();
            builder.HasMany(entry => entry.Lines)
                .WithOne(line => line.JournalEntry)
                .HasForeignKey(line => line.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(entry => entry.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<JournalLine>(builder =>
        {
            builder.HasKey(line => line.Id);
            builder.Property(line => line.AccountId).HasMaxLength(100).IsRequired();
            builder.Property(line => line.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(line => line.Amount).HasPrecision(18, 2).IsRequired();
        });
    }
}
