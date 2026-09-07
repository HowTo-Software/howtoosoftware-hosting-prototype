using HowToSoftware.Hosting.Infrastructure.Configuration;
using HowToSoftware.Hosting.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HowToSoftware.Hosting.Data;

/// <summary>Design-time factory used exclusively by reproducible commerce migrations.</summary>
public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    public CommerceDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.LoadNearest();

        // Scaffolding a migration only needs a well-formed string; it never opens the connection.
        // The placeholder keeps `dotnet ef migrations add` working on a machine with no server.
        var raw = Environment.GetEnvironmentVariable(SqlServerOptions.EnvironmentVariableName);
        var connectionString = SqlServerOptions.TryBuildConnectionString(raw, out var configured)
            ? configured
            : "Server=localhost,1433;Database=HowToSoftwareHosting;Integrated Security=True;Encrypt=True";

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer(connectionString, sqlServer =>
                sqlServer.MigrationsHistoryTable(CommerceDbContext.MigrationsHistoryTable))
            .Options;

        return new CommerceDbContext(options);
    }
}
