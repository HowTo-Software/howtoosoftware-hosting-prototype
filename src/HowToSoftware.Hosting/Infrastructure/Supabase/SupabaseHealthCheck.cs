using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace HowToSoftware.Hosting.Infrastructure.Supabase;

/// <summary>Safe connectivity check that never includes connection details or credentials.</summary>
public sealed class SupabaseHealthCheck(SupabaseOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsDatabaseConfigured)
        {
            return HealthCheckResult.Degraded("Supabase not configured");
        }

        try
        {
            await using var connection = new NpgsqlConnection(options.GetNpgsqlConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy("Supabase connected");
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("Supabase connection error");
        }
    }
}
