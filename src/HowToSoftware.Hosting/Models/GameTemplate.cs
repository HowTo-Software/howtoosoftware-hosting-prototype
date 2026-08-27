namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Everything about a game that is not a resource limit: which Pterodactyl egg builds it, which
/// image it runs in, how it starts, and which environment variables the egg requires.
/// </summary>
/// <remarks>
/// <para>
/// Kept out of the UI entirely. A component asks for a plan; the plan names a template; the
/// provisioner resolves it. Adding a second game later is a new template and a new set of plans
/// pointing at it - not a second code path.
/// </para>
/// <para>
/// The egg, nest and location identifiers are deployment facts, not product facts, so they come
/// from configuration rather than being compiled in: they differ between a staging panel and the
/// production one.
/// </para>
/// </remarks>
public sealed record GameTemplate
{
    /// <summary>Stable identifier a <see cref="HostingPlan"/> refers to.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable game name, for logs and the provisioning lab.</summary>
    public required string GameName { get; init; }

    /// <summary>Pterodactyl nest containing the egg.</summary>
    public required int NestId { get; init; }

    /// <summary>Pterodactyl egg the server is built from.</summary>
    public required int EggId { get; init; }

    /// <summary>
    /// Location the server is deployed into. Pterodactyl picks a node inside it that has both
    /// capacity and a free allocation.
    /// </summary>
    public required int LocationId { get; init; }

    /// <summary>
    /// Docker image override, or <see langword="null"/> to use the egg's default.
    /// </summary>
    /// <remarks>
    /// Leaving this unset is the safer default: the egg's own image is the one its author tested
    /// its startup command against.
    /// </remarks>
    public string? DockerImage { get; init; }

    /// <summary>
    /// Startup command override, or <see langword="null"/> to use the egg's default.
    /// </summary>
    public string? StartupCommand { get; init; }

    /// <summary>
    /// Egg environment variables, keyed by the egg's <c>env_variable</c> name.
    /// </summary>
    /// <remarks>
    /// The panel rejects a creation request that omits a variable the egg marks required, so
    /// these are validated against the live egg before the first deployment rather than guessed.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Port range servers may be allocated from, e.g. <c>16261-16281</c>. Empty means the panel
    /// chooses from whatever the node already has free.
    /// </summary>
    public IReadOnlyList<string> PortRange { get; init; } = [];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
