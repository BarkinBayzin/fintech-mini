using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ledger.Api.Data;

public sealed class LedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LedgerDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("LEDGER_CONNECTION_STRING") ??
            "Host=localhost;Port=5434;Database=ledgerdb;Username=ledger;Password=ledger";

        optionsBuilder.UseNpgsql(connectionString);
        return new LedgerDbContext(optionsBuilder.Options);
    }
}
