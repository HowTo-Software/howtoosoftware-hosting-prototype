using Npgsql;

namespace HowToSoftware.Hosting.Infrastructure.Supabase;

/// <summary>Server-side configuration for the independent Supabase persistence provider.</summary>
public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    /// <summary>Supabase project URL. Needed only by server-side Data API integrations.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Low-privilege publishable key. The current backend-only design does not expose it.</summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>Elevated server-only API key. Never rendered or logged.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Direct or session-pooler PostgreSQL connection string.</summary>
    public string DbConnectionString { get; set; } = string.Empty;

    /// <summary>Whether a real PostgreSQL connection string was supplied.</summary>
    public bool IsDatabaseConfigured => !IsPlaceholder(DbConnectionString)
        && TryBuildNpgsqlConnectionString(DbConnectionString, out _);

    /// <summary>Whether the optional Supabase Data API credentials are usable server-side.</summary>
    public bool IsDataApiConfigured => Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !IsPlaceholder(Url)
        && SecretKey.StartsWith("sb_secret_", StringComparison.Ordinal)
        && !IsPlaceholder(SecretKey);

    /// <summary>Builds options from the documented environment names and ASP.NET section names.</summary>
    public static SupabaseOptions FromConfiguration(IConfiguration configuration) => new()
    {
        Url = First(configuration["SUPABASE_URL"], configuration[$"{SectionName}:Url"]),
        PublishableKey = First(
            configuration["SUPABASE_PUBLISHABLE_KEY"],
            configuration[$"{SectionName}:PublishableKey"]),
        SecretKey = First(
            configuration["SUPABASE_SECRET_KEY"],
            configuration[$"{SectionName}:SecretKey"]),
        DbConnectionString = First(
            configuration["SUPABASE_DB_CONNECTION_STRING"],
            configuration[$"{SectionName}:DbConnectionString"])
    };

    /// <summary>Returns an Npgsql connection string without exposing it to callers as diagnostics.</summary>
    public string GetNpgsqlConnectionString()
    {
        if (!TryBuildNpgsqlConnectionString(DbConnectionString, out var connectionString))
        {
            throw new InvalidOperationException(
                "Supabase PostgreSQL is not configured. Set SUPABASE_DB_CONNECTION_STRING to a real Supabase connection string.");
        }

        return connectionString;
    }

    internal static bool TryBuildNpgsqlConnectionString(string? raw, out string connectionString)
    {
        connectionString = string.Empty;

        if (string.IsNullOrWhiteSpace(raw) || IsPlaceholder(raw))
        {
            return false;
        }

        try
        {
            if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(raw);
                var credentials = uri.UserInfo.Split(':', 2);
                if (credentials.Length != 2)
                {
                    return false;
                }

                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.IsDefaultPort ? 5432 : uri.Port,
                    Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                    Username = Uri.UnescapeDataString(credentials[0]),
                    Password = Uri.UnescapeDataString(credentials[1]),
                    SslMode = SslMode.Require,
                    Timeout = 15,
                    CommandTimeout = 30,
                    ApplicationName = "HowToSoftware.Hosting"
                };

                connectionString = builder.ConnectionString;
                return true;
            }

            var parsed = new NpgsqlConnectionStringBuilder(raw);
            if (string.IsNullOrWhiteSpace(parsed.Host)
                || string.IsNullOrWhiteSpace(parsed.Database)
                || string.IsNullOrWhiteSpace(parsed.Username))
            {
                return false;
            }

            if (parsed.SslMode is SslMode.Disable)
            {
                parsed.SslMode = SslMode.Require;
            }

            parsed.ApplicationName = "HowToSoftware.Hosting";
            connectionString = parsed.ConnectionString;
            return true;
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException)
        {
            return false;
        }
    }

    internal static bool IsPlaceholder(string? value) => string.IsNullOrWhiteSpace(value)
        || value.Contains("API_AQUI", StringComparison.OrdinalIgnoreCase)
        || value.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase)
        || value.Contains("PROJECT_ID", StringComparison.OrdinalIgnoreCase)
        || value.Contains("USER:PASSWORD", StringComparison.OrdinalIgnoreCase)
        || value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase)
        || value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

    private static string First(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : fallback?.Trim() ?? string.Empty;

    /// <summary>Prevents accidental interpolation from printing credentials.</summary>
    public override string ToString() => nameof(SupabaseOptions);
}
