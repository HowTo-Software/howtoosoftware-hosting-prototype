using HowToSoftware.Hosting.Infrastructure.Configuration;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// Connection and deployment settings for the Pterodactyl Application API.
/// </summary>
/// <remarks>
/// <para>
/// <b>The API key never belongs in <c>appsettings.json</c>.</b> That file is committed, and an
/// Application key is a full administrative credential for the panel: it can read every
/// customer, create servers and delete them. Supply it from the environment or from user-secrets
/// instead:
/// </para>
/// <code>
/// dotnet user-secrets set "Pterodactyl:ApiKey" "ptla_..."      # development
/// setx Pterodactyl__ApiKey "ptla_..."                           # Windows environment
/// export Pterodactyl__ApiKey="ptla_..."                         # Linux environment
/// </code>
/// <para>
/// Everything else here is deployment topology rather than a secret and may sit in
/// configuration: which panel, which egg, which location to deploy into.
/// </para>
/// </remarks>
public sealed class PterodactylOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Pterodactyl";

    /// <summary>
    /// Panel root, e.g. <c>https://panel.example.com</c>. The <c>/api/application</c> prefix is
    /// added by the client, so it must not be included here.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Application API key. Panel 1.x issues these with a <c>ptla_</c> prefix; a <c>ptlc_</c>
    /// key is a <em>client</em> key and cannot reach these endpoints.
    /// </summary>
    /// <remarks>
    /// Never logged, never rendered, never sent to the browser. <see cref="ToString"/> on this
    /// type is overridden so an accidental interpolation cannot leak it either.
    /// </remarks>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Location servers are deployed into. Pterodactyl picks a node inside it.</summary>
    public int LocationId { get; set; }

    /// <summary>Nest containing the Project Zomboid egg.</summary>
    public int NestId { get; set; }

    /// <summary>Egg a Project Zomboid server is built from.</summary>
    public int EggId { get; set; }

    /// <summary>
    /// Docker image override, or empty to use whatever the egg declares. Leaving it empty is
    /// the safer default: the egg's own image is the one its startup command was tested against.
    /// </summary>
    public string? DockerImage { get; set; }

    /// <summary>Startup command override, or empty to use the egg's default.</summary>
    public string? StartupCommand { get; set; }

    /// <summary>
    /// Port ranges the panel may allocate from, e.g. <c>["16261-16281"]</c>. Empty lets the
    /// panel choose from whatever the selected node already has free.
    /// </summary>
    public List<string> PortRange { get; set; } = [];

    /// <summary>
    /// Egg environment variables, keyed by the egg's <c>env_variable</c> name. The panel rejects
    /// a creation request that omits one the egg marks required.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.Ordinal);

    /// <summary>How long a single panel call may take before it is abandoned.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Whether a usable key has been supplied.</summary>
    public bool IsConfigured =>
        !EnvironmentFile.IsPlaceholder(BaseUrl)
        && !EnvironmentFile.IsPlaceholder(ApiKey)
        && ApiKey.StartsWith("ptla_", StringComparison.Ordinal);

    /// <summary>
    /// The panel root with any trailing slash removed, so paths concatenate predictably.
    /// </summary>
    public string NormalisedBaseUrl => BaseUrl.TrimEnd('/');

    /// <summary>
    /// A safe description of the key for diagnostics: its prefix and length, never its value.
    /// </summary>
    /// <remarks>
    /// Enough to answer "did it read the key I set, and is it an Application key?" without ever
    /// putting the credential itself into a log or onto a page.
    /// </remarks>
    public string DescribeKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return "not set";
        }

        var prefix = ApiKey.Length >= 5 ? ApiKey[..5] : "?????";

        return $"{prefix}… ({ApiKey.Length} chars)";
    }

    /// <summary>
    /// Deliberately does not include any field values, so interpolating this object into a log
    /// message or an exception cannot leak the key.
    /// </summary>
    public override string ToString() => nameof(PterodactylOptions);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
