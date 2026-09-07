using HowToSoftware.Hosting.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HowToSoftware.Hosting.Data;

/// <summary>Design-time factory used exclusively by reproducible hosting migrations.</summary>
public sealed class HostingDbContextFactory : IDesignTimeDbContextFactory<HostingDbContext>
{
    public HostingDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.LoadNearest();

        var raw = Environment.GetEnvironmentVariable($"ConnectionStrings__{HostingDbContext.ConnectionName}");
        var connectionString = string.IsNullOrWhiteSpace(raw)
            ? "Server=localhost,1433;Database=HowToSoftwareHosting;Integrated Security=True;Encrypt=True"
            : raw;

        var options = new DbContextOptionsBuilder<HostingDbContext>()
            .UseSqlServer(connectionString, sqlServer =>
                sqlServer.MigrationsHistoryTable(HostingDbContext.MigrationsHistoryTable))
            .Options;

        return new HostingDbContext(options);
    }
}
