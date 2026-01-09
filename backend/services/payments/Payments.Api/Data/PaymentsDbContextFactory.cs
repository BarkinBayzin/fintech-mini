using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payments.Api.Data;

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("PAYMENTS_CONNECTION_STRING") ??
            "Host=localhost;Port=5433;Database=paymentsdb;Username=payments;Password=payments";

        optionsBuilder.UseNpgsql(connectionString);
        return new PaymentsDbContext(optionsBuilder.Options);
    }
}
