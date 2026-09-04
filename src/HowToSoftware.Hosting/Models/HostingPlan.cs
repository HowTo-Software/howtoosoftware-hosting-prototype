namespace HowToSoftware.Hosting.Models;

/// <summary>
/// A single labelled specification line on a plan or node panel.
/// </summary>
/// <param name="Label">Specification name, e.g. <c>MEMORY</c>.</param>
/// <param name="Value">Specification value, e.g. <c>10 GB</c>.</param>
public sealed record PlanSpec(string Label, string Value);

/// <summary>
/// A Project Zomboid hosting plan: the commercial product definition, and the single source of
/// truth for the resources a server on that plan is created with.
/// </summary>
/// <remarks>
/// <para>
/// The resource fields are stored in the units the Pterodactyl Application API expects, so
/// provisioning passes them straight through with no arithmetic in between - a conversion
/// applied in one place and forgotten in another is how a customer ends up with a 4 MB server.
/// Human-readable figures are derived from them (<see cref="MemoryGb"/>, <see cref="DiskGb"/>),
/// never stored alongside them.
/// </para>
/// <para>
/// <b>CPU is a share, not a core count.</b> <see cref="CpuPercent"/> is Pterodactyl's cgroup CPU
/// limit, where 100 is the equivalent of one logical thread. It does not pin cores, so it must
/// never be presented as "3 dedicated cores".
/// </para>
/// </remarks>
public sealed record HostingPlan
{
    /// <summary>Stable slug, used for element ids, selection state and configuration keys.</summary>
    public required string Slug { get; init; }

    /// <summary>Plan name shown to customers.</summary>
    public required string Name { get; init; }

    /// <summary>One-line positioning for the plan.</summary>
    public required string Tagline { get; init; }

    /// <summary>Memory limit in <b>MiB</b>, passed to Pterodactyl as <c>limits.memory</c>.</summary>
    public required int MemoryMib { get; init; }

    /// <summary>
    /// CPU limit as a percentage, passed to Pterodactyl as <c>limits.cpu</c>. 100 is the
    /// equivalent of one logical thread; 300 lets the container burst across three.
    /// </summary>
    public required int CpuPercent { get; init; }

    /// <summary>Disk limit in <b>MiB</b>, passed to Pterodactyl as <c>limits.disk</c>.</summary>
    public required int DiskMib { get; init; }

    /// <summary>Backups the customer may keep, passed as <c>feature_limits.backups</c>.</summary>
    public required int BackupLimit { get; init; }

    /// <summary>Databases the customer may create, passed as <c>feature_limits.databases</c>.</summary>
    public required int DatabaseLimit { get; init; }

    /// <summary>Ports the server may hold, passed as <c>feature_limits.allocations</c>.</summary>
    public required int AllocationLimit { get; init; }

    /// <summary>Identifier of the <see cref="GameTemplate"/> a server on this plan is built from.</summary>
    public required string GameTemplateId { get; init; }

    /// <summary>
    /// Headline rate per month, or <see langword="null"/> while pricing has not been decided.
    /// </summary>
    /// <remarks>
    /// Prices come from configuration rather than being compiled in, so the commercial decision
    /// does not need a code change - and so an unset price is visibly unset rather than quietly
    /// defaulting to a number nobody agreed to.
    /// </remarks>
    public decimal? PriceMonthly { get; init; }

    /// <summary>
    /// Player slots this plan is sold with, or <see langword="null"/> while undecided.
    /// </summary>
    /// <remarks>
    /// Project Zomboid's player limit is a server-configuration value, not a Pterodactyl limit,
    /// so it is not derived from the memory allocation. It stays unset until the figures are
    /// agreed rather than being inferred from RAM.
    /// </remarks>
    public int? PlayerSlots { get; init; }

    /// <summary>
    /// Who the plan is for, in one or two plain sentences, shown at the review step before
    /// payment.
    /// </summary>
    /// <remarks>
    /// Qualitative on purpose - "a medium-sized server with mods and regular activity" - because
    /// a player count is a server-configuration value this catalogue does not set, and a figure
    /// printed here would read as a promise.
    /// </remarks>
    public required string Audience { get; init; }

    /// <summary>Marks the plan the catalogue highlights by default.</summary>
    public bool IsRecommended { get; init; }

    /// <summary>Whether a price has been set for this plan.</summary>
    public bool IsPriced => PriceMonthly is > 0m;

    /// <summary>Memory in whole GB, for display.</summary>
    public int MemoryGb => MemoryMib / 1024;

    /// <summary>
    /// Whether the HowToSoftware server bot is offered on this tier.
    /// </summary>
    /// <remarks>
    /// Derived from the tier's own memory rather than flagged per plan, so a card can never
    /// advertise the bot on a server too small to be offered it - the one way this could go
    /// wrong is somebody ticking a box on the wrong row, and there is no box.
    /// </remarks>
    public bool HasServerBot => ServerBot.IsEligible(MemoryGb);

    /// <summary>Disk in whole GB, for display.</summary>
    public int DiskGb => DiskMib / 1024;

    /// <summary>The plan's resources, in the shape the server-creation endpoint expects.</summary>
    public ServerResourceLimits ToResourceLimits() => new(MemoryMib, CpuPercent, DiskMib);

    /// <summary>The plan's feature caps, in the shape the server-creation endpoint expects.</summary>
    public ServerFeatureLimits ToFeatureLimits() => new(DatabaseLimit, AllocationLimit, BackupLimit);
}

/// <summary>
/// Container resource limits, in Pterodactyl's units.
/// </summary>
/// <param name="MemoryMib">Memory ceiling in MiB.</param>
/// <param name="CpuPercent">CPU ceiling as a percentage, 100 per logical thread.</param>
/// <param name="DiskMib">Disk ceiling in MiB.</param>
public readonly record struct ServerResourceLimits(int MemoryMib, int CpuPercent, int DiskMib);

/// <summary>
/// Per-server feature caps, in Pterodactyl's units.
/// </summary>
/// <param name="Databases">Databases the customer may create.</param>
/// <param name="Allocations">Ports the server may hold.</param>
/// <param name="Backups">Backups the customer may keep.</param>
public readonly record struct ServerFeatureLimits(int Databases, int Allocations, int Backups);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
