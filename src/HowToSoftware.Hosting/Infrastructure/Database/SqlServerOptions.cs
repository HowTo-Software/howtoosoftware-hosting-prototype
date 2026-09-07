using Microsoft.Data.SqlClient;

namespace HowToSoftware.Hosting.Infrastructure.Database;

/// <summary>Server-side configuration for the SQL Server commerce database.</summary>
public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    /// <summary>Environment name carrying the connection string, credentials included.</summary>
    public const string EnvironmentVariableName = "SQLSERVER_CONNECTION_STRING";

    /// <summary>ADO.NET connection string for the commerce database.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Whether a real SQL Server connection string was supplied.</summary>
    public bool IsDatabaseConfigured => !IsPlaceholder(ConnectionString)
        && TryBuildConnectionString(ConnectionString, out _);

    /// <summary>Builds options from the documented environment and ASP.NET section names.</summary>
    public static SqlServerOptions FromConfiguration(IConfiguration configuration) => new()
    {
        ConnectionString = First(
            configuration[EnvironmentVariableName],
            configuration[$"{SectionName}:ConnectionString"],
            configuration.GetConnectionString(CommerceConnectionName))
    };

    /// <summary>Name of the connection string in configuration.</summary>
    public const string CommerceConnectionName = "Commerce";

    /// <summary>Returns the hardened connection string without exposing it as diagnostics.</summary>
    public string GetConnectionString()
    {
        if (!TryBuildConnectionString(ConnectionString, out var connectionString))
        {
            throw new InvalidOperationException(
                $"SQL Server is not configured. Set {EnvironmentVariableName} to a connection string naming a server and a database.");
        }

        return connectionString;
    }

    /// <summary>Server and database only, safe to log. Never includes credentials.</summary>
    public string DescribeTarget()
    {
        if (!TryBuildConnectionString(ConnectionString, out var connectionString))
        {
            return "unconfigured";
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        return $"{builder.DataSource}/{builder.InitialCatalog}";
    }

    internal static bool TryBuildConnectionString(string? raw, out string connectionString)
    {
        connectionString = string.Empty;

        if (string.IsNullOrWhiteSpace(raw) || IsPlaceholder(raw))
        {
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(raw);

            if (string.IsNullOrWhiteSpace(builder.DataSource)
                || string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                return false;
            }

            // Encryption is not negotiable: without it the credentials and every row cross the
            // network in the clear. Certificate validation stays on unless the operator turns it
            // off deliberately in the supplied string, because trusting any presented certificate
            // still permits a machine-in-the-middle.
            builder.Encrypt = true;
            builder.PersistSecurityInfo = false;
            builder.ConnectTimeout = 15;
            builder.CommandTimeout = 30;
            builder.ApplicationName = "HowToSoftware.Hosting";

            connectionString = builder.ConnectionString;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            return false;
        }
    }

    internal static bool IsPlaceholder(string? value) => string.IsNullOrWhiteSpace(value)
        || value.Contains("API_AQUI", StringComparison.OrdinalIgnoreCase)
        || value.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase)
        || value.Contains("SENHA_AQUI", StringComparison.OrdinalIgnoreCase)
        || value.Contains("USER:PASSWORD", StringComparison.OrdinalIgnoreCase)
        || value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase)
        || value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

    private static string First(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>Prevents accidental interpolation from printing credentials.</summary>
    public override string ToString() => nameof(SqlServerOptions);
}
