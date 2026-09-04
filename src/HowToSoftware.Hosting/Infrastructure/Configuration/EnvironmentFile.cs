namespace HowToSoftware.Hosting.Infrastructure.Configuration;

/// <summary>
/// Loads a repository-local <c>.env</c> file for development without overriding variables that
/// the process, container or secret store already supplied.
/// </summary>
public static class EnvironmentFile
{
    private static readonly IReadOnlyDictionary<string, string> AspNetCoreAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["STRIPE_PUBLISHABLE_KEY"] = "Stripe__PublishableKey",
            ["STRIPE_SECRET_KEY"] = "Stripe__SecretKey",
            ["STRIPE_WEBHOOK_SECRET"] = "Stripe__WebhookSecret",
            ["STRIPE_SUCCESS_URL"] = "Stripe__SuccessUrl",
            ["STRIPE_CANCEL_URL"] = "Stripe__CancelUrl",
            ["STRIPE_CURRENCY"] = "HostingPlans__CurrencyCode",
            ["STRIPE_WEBHOOK_TOLERANCE_SECONDS"] = "Stripe__WebhookToleranceSeconds",
            ["PTERODACTYL_PANEL_URL"] = "Pterodactyl__BaseUrl",
            ["PTERODACTYL_APPLICATION_API_KEY"] = "Pterodactyl__ApiKey",
            ["PTERODACTYL_LOCATION_ID"] = "Pterodactyl__LocationId",
            ["PTERODACTYL_NEST_ID"] = "Pterodactyl__NestId",
            ["PTERODACTYL_EGG_ID"] = "Pterodactyl__EggId",
            ["PTERODACTYL_DOCKER_IMAGE"] = "Pterodactyl__DockerImage",
            ["PTERODACTYL_STARTUP_COMMAND"] = "Pterodactyl__StartupCommand",
            ["PTERODACTYL_TIMEOUT_SECONDS"] = "Pterodactyl__TimeoutSeconds",
            ["PTERODACTYL_DEPLOY_TESTS_ENABLED"] = "ProvisioningTest__Enabled",
            ["APP_BASE_URL"] = "Site__BaseUrl",
            ["APP_ENVIRONMENT"] = "ASPNETCORE_ENVIRONMENT"
        };

    /// <summary>Finds and loads the nearest <c>.env</c> at or above the current directory.</summary>
    public static void LoadNearest()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                LoadIfPresent(candidate);
                return;
            }

            directory = directory.Parent;
        }
    }

    /// <summary>Loads <paramref name="path"/> when it exists.</summary>
    public static void LoadIfPresent(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var sourceLine in File.ReadLines(path))
        {
            var line = sourceLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line[7..].TrimStart();
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null)
            {
                continue;
            }

            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"')
                    || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(name, value);
        }
    }

    /// <summary>
    /// Maps the portable names documented in <c>.env.example</c> to ASP.NET Core configuration
    /// paths. Explicit ASP.NET Core variables win, so existing deployments using double
    /// underscores continue to work unchanged.
    /// </summary>
    public static void ApplyAspNetCoreAliases()
    {
        foreach (var (portableName, aspNetCoreName) in AspNetCoreAliases)
        {
            if (Environment.GetEnvironmentVariable(aspNetCoreName) is not null)
            {
                continue;
            }

            var value = Environment.GetEnvironmentVariable(portableName);
            if (value is not null)
            {
                Environment.SetEnvironmentVariable(aspNetCoreName, value);
            }
        }
    }

    /// <summary>Whether a documented example value is still a placeholder.</summary>
    public static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Contains("API_AQUI", StringComparison.OrdinalIgnoreCase)
            || value.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase)
            || value.Contains("YOUR_DOMAIN", StringComparison.OrdinalIgnoreCase)
            || value.Contains("SITE_AQUI", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PANEL_AQUI", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PROJECT_ID", StringComparison.OrdinalIgnoreCase)
            || value.Contains("USER:PASSWORD", StringComparison.OrdinalIgnoreCase);
    }
}
