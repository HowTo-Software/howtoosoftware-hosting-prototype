using HowToSoftware.Hosting.Infrastructure.Configuration;
using HowToSoftware.Hosting.Infrastructure.Supabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HowToSoftware.Hosting.Data;

/// <summary>Design-time factory used exclusively by reproducible commerce migrations.</summary>
public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    public CommerceDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.LoadNearest();

        var raw = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION_STRING");
        var connectionString = SupabaseOptions.TryBuildNpgsqlConnectionString(raw, out var configured)
            ? configured
            : "Host=localhost;Port=5432;Database=hts_commerce;Username=postgres;Password=postgres;SSL Mode=Disable";

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(connectionString, postgres =>
                postgres.MigrationsHistoryTable("__ef_migrations_history"))
            .Options;

        return new CommerceDbContext(options);
    }
}
