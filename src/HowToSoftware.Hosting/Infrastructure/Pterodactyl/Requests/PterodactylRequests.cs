using System.Text.Json.Serialization;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl.Requests;

/// <summary>
/// Body for <c>POST /api/application/users</c>.
/// </summary>
/// <remarks>
/// Required by the panel: <c>email</c>, <c>username</c>, <c>first_name</c>, <c>last_name</c>.
/// Everything else is optional. <c>root_admin</c> must never be sent as null - the panel's
/// boolean rule rejects an explicit null while happily accepting an omitted key.
/// </remarks>
public sealed record CreateUserRequest
{
    /// <summary>Our correlation id, so the same customer resolves to the same panel user.</summary>
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; init; }

    /// <summary>Email address. The panel enforces strict validation and uniqueness.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Panel username. The panel applies its own regex on the lower-cased value:
    /// alphanumeric first and last character, <c>[A-Za-z0-9_.-]</c> in between, minimum three
    /// characters. <see cref="PterodactylNaming"/> produces one that satisfies it.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>Given name.</summary>
    [JsonPropertyName("first_name")]
    public required string FirstName { get; init; }

    /// <summary>Family name.</summary>
    [JsonPropertyName("last_name")]
    public required string LastName { get; init; }

    /// <summary>
    /// Initial password. The Application API enforces no length or complexity of its own, so a
    /// cryptographically random one is generated rather than relying on the panel to insist.
    /// It is never logged, never returned to the browser and never persisted by this application.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; init; }

    /// <summary>
    /// Panel UI language. Only <c>en</c> ships with Panel 1.x; anything else is a 422.
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; init; } = "en";

    /// <summary>
    /// Always false. A provisioned customer must never be a panel administrator.
    /// </summary>
    [JsonPropertyName("root_admin")]
    public bool RootAdmin { get; init; }
}

/// <summary>
/// Container resource limits, in the panel's own units.
/// </summary>
/// <param name="Memory">Ceiling in <b>MiB</b>. 0 means unlimited.</param>
/// <param name="Swap">
/// Additional swap in MiB on top of <paramref name="Memory"/>. 0 disables swap, -1 is unlimited.
/// </param>
/// <param name="Disk">Ceiling in <b>MiB</b>. 0 means unlimited.</param>
/// <param name="Io">Block IO weight relative to other containers. The panel accepts 10-1000.</param>
/// <param name="Cpu">
/// Percentage of one logical thread. 100 is one thread; 300 lets the container burst across
/// three. 0 means unlimited. It is a ceiling on a shared pool, not pinned cores.
/// </param>
public sealed record ServerLimitsPayload(
    [property: JsonPropertyName("memory")] int Memory,
    [property: JsonPropertyName("swap")] int Swap,
    [property: JsonPropertyName("disk")] int Disk,
    [property: JsonPropertyName("io")] int Io,
    [property: JsonPropertyName("cpu")] int Cpu);

/// <summary>
/// Per-server feature caps. All three are sent as integers on every request: the panel requires
/// <c>databases</c> and <c>backups</c> to be present, and coerces a null to zero anyway.
/// </summary>
/// <param name="Databases">Databases the customer may create.</param>
/// <param name="Allocations">Ports the server may hold.</param>
/// <param name="Backups">Backups the customer may keep.</param>
public sealed record ServerFeatureLimitsPayload(
    [property: JsonPropertyName("databases")] int Databases,
    [property: JsonPropertyName("allocations")] int Allocations,
    [property: JsonPropertyName("backups")] int Backups);

/// <summary>
/// Automatic placement. The panel picks a public node in one of the given locations that has
/// both memory and disk headroom, then a free allocation on it.
/// </summary>
/// <param name="Locations">
/// Location ids to search. An empty array searches every location.
/// </param>
/// <param name="DedicatedIp">
/// Whether the chosen allocation's IP must not already host another server. Sent as a real JSON
/// boolean: the panel's rule for this field is malformed and does not actually type-check it,
/// so a string would be silently coerced.
/// </param>
/// <param name="PortRange">
/// Port ranges to choose from, as strings - a bare port, or <c>"16261-16281"</c>. An empty array
/// lets the panel use any free port on the node.
/// </param>
/// <remarks>
/// <c>locations</c> and <c>port_range</c> must both be present when <c>deploy</c> is, even if
/// empty, so neither is ever omitted from the serialised body.
/// </remarks>
public sealed record ServerDeploymentPayload(
    [property: JsonPropertyName("locations")] IReadOnlyList<int> Locations,
    [property: JsonPropertyName("dedicated_ip")] bool DedicatedIp,
    [property: JsonPropertyName("port_range")] IReadOnlyList<string> PortRange);

/// <summary>
/// Body for <c>POST /api/application/servers</c>.
/// </summary>
/// <remarks>
/// <para>
/// Note what is <b>not</b> here: there is no <c>nest</c> field on this endpoint in Panel 1.x.
/// The panel derives the nest from the egg, and third-party guides that list it are wrong.
/// </para>
/// <para>
/// <c>docker_image</c> and <c>startup</c> are required, which is why the provisioner reads the
/// egg first instead of hoping the panel will fill them in.
/// </para>
/// <para>
/// <c>oom_disabled</c> is deliberately not sent, so the panel's own default applies. Sending it
/// would mean picking a memory-kill policy here rather than in the panel where an operator can
/// see it - and the field's meaning is inverted between Pterodactyl and the Pelican fork.
/// </para>
/// </remarks>
public sealed record CreateServerRequest
{
    /// <summary>Our correlation id. Unique across servers, at most 191 characters, no slashes.</summary>
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; init; }

    /// <summary>Server name shown in the panel.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Numeric id of the owning panel user.</summary>
    [JsonPropertyName("user")]
    public required int User { get; init; }

    /// <summary>Numeric id of the egg to build from.</summary>
    [JsonPropertyName("egg")]
    public required int Egg { get; init; }

    /// <summary>Container image. Required; taken from the egg unless overridden.</summary>
    [JsonPropertyName("docker_image")]
    public required string DockerImage { get; init; }

    /// <summary>Startup command. Required; taken from the egg unless overridden.</summary>
    [JsonPropertyName("startup")]
    public required string Startup { get; init; }

    /// <summary>
    /// Egg variables keyed by <c>env_variable</c>. The key must be present even when the map is
    /// empty; omitting it entirely is a 422.
    /// </summary>
    [JsonPropertyName("environment")]
    public required IReadOnlyDictionary<string, string> Environment { get; init; }

    /// <summary>Container limits.</summary>
    [JsonPropertyName("limits")]
    public required ServerLimitsPayload Limits { get; init; }

    /// <summary>Feature caps.</summary>
    [JsonPropertyName("feature_limits")]
    public required ServerFeatureLimitsPayload FeatureLimits { get; init; }

    /// <summary>
    /// Automatic placement. Mutually exclusive with pinning an explicit allocation - and this
    /// client never sends an allocation, so the panel's node selection is always in charge.
    /// </summary>
    [JsonPropertyName("deploy")]
    public required ServerDeploymentPayload Deploy { get; init; }

    /// <summary>Whether the panel should boot the server once installation finishes.</summary>
    [JsonPropertyName("start_on_completion")]
    public bool StartOnCompletion { get; init; } = true;

    /// <summary>Optional free-text description stored on the server.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
