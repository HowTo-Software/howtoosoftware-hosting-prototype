using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// Checks the panel settings, without demanding that they exist.
/// </summary>
/// <remarks>
/// <para>
/// The marketing site has to keep serving on a host with no panel credentials - most deployments
/// of it will never provision anything. So an empty configuration is valid, and provisioning
/// reports <see cref="PterodactylFailure.NotConfigured"/> when someone actually tries to use it.
/// </para>
/// <para>
/// What is <em>not</em> tolerated is a half-configured panel: a base URL that is not a URL, a key
/// that is a client key rather than an application one, or a location id of zero. Those fail
/// loudly at startup, because each of them produces a confusing runtime error much later.
/// </para>
/// </remarks>
public sealed class PterodactylOptionsValidator : IValidateOptions<PterodactylOptions>
{
    /// <summary>Prefix Panel 1.x gives Application API keys.</summary>
    private const string ApplicationKeyPrefix = "ptla_";

    /// <summary>Prefix Panel 1.x gives Client API keys, which cannot reach these endpoints.</summary>
    private const string ClientKeyPrefix = "ptlc_";

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PterodactylOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var hasUrl = !string.IsNullOrWhiteSpace(options.BaseUrl);
        var hasKey = !string.IsNullOrWhiteSpace(options.ApiKey);

        // Nothing configured at all: a perfectly normal marketing-only deployment.
        if (!hasUrl && !hasKey)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (!hasUrl)
        {
            failures.Add("Pterodactyl:ApiKey is set but Pterodactyl:BaseUrl is not.");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            failures.Add(
                "Pterodactyl:BaseUrl must be an absolute http(s) URL pointing at the panel root, "
                    + "e.g. https://panel.example.com - without the /api/application suffix.");
        }
        else if (uri.AbsolutePath.Contains("/api", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                "Pterodactyl:BaseUrl must be the panel root only. The /api/application prefix is "
                    + "added by the client.");
        }

        if (!hasKey)
        {
            failures.Add(
                "Pterodactyl:BaseUrl is set but Pterodactyl:ApiKey is not. Supply it through the "
                    + "environment variable Pterodactyl__ApiKey or dotnet user-secrets - never in appsettings.json.");
        }
        else if (options.ApiKey.StartsWith(ClientKeyPrefix, StringComparison.Ordinal))
        {
            // Worth catching here: a client key authenticates and then 403s on everything, which
            // reads like a permissions problem rather than the wrong key entirely.
            failures.Add(
                "Pterodactyl:ApiKey looks like a Client API key (ptlc_). Provisioning needs an "
                    + "Application API key (ptla_), created under Admin → Application API.");
        }
        else if (!options.ApiKey.StartsWith(ApplicationKeyPrefix, StringComparison.Ordinal))
        {
            failures.Add(
                "Pterodactyl:ApiKey does not carry the ptla_ prefix of a Panel 1.x Application API key. "
                    + "Check it was copied whole.");
        }

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("Pterodactyl:TimeoutSeconds must be between 1 and 300.");
        }

        foreach (var (field, value) in new[]
                 {
                     ("LocationId", options.LocationId),
                     ("NestId", options.NestId),
                     ("EggId", options.EggId)
                 })
        {
            if (value < 0)
            {
                failures.Add($"Pterodactyl:{field} must not be negative.");
            }
        }

        foreach (var range in options.PortRange)
        {
            if (!IsValidPortRange(range))
            {
                failures.Add(
                    $"Pterodactyl:PortRange entry '{range}' is not a port or a port range. "
                        + "Use \"16261\" or \"16261-16281\".");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Whether an entry is a bare port or a <c>low-high</c> range the panel will accept.
    /// </summary>
    /// <param name="range">Entry to check.</param>
    /// <remarks>
    /// The panel matches ranges against <c>/^(\d{4,5})-(\d{4,5})$/</c> and rejects a span wider
    /// than 1000 ports, so both are checked here rather than at deployment time.
    /// </remarks>
    internal static bool IsValidPortRange(string? range)
    {
        if (string.IsNullOrWhiteSpace(range))
        {
            return false;
        }

        var parts = range.Split('-');

        if (parts.Length == 1)
        {
            return ushort.TryParse(parts[0], out var single) && single > 0;
        }

        if (parts.Length != 2)
        {
            return false;
        }

        if (!ushort.TryParse(parts[0], out var low) || !ushort.TryParse(parts[1], out var high))
        {
            return false;
        }

        return low > 0 && high >= low && high - low < 1000;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
