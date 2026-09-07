using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HowToSoftware.Hosting.Infrastructure.Database;

/// <summary>Safe connectivity check that never includes connection details or credentials.</summary>
public sealed class SqlServerHealthCheck(SqlServerOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsDatabaseConfigured)
        {
            // The application deliberately serves its marketing pages with no database at all.
            // That is a healthy operating mode, not a degraded remote dependency.
            return HealthCheckResult.Healthy("No database configured");
        }

        try
        {
            await using var connection = new SqlConnection(options.GetConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy("SQL Server connected");
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection error");
        }
    }
}
