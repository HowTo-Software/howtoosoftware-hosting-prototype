namespace HowToSoftware.Hosting.Models;

/// <summary>
/// The four hardware dimensions the infrastructure page reports on.
/// </summary>
public enum HardwareSpecKind
{
    Cpu,
    Memory,
    Storage,
    Network
}

/// <summary>
/// One specification line.
/// </summary>
/// <param name="Kind">Which dimension this line describes.</param>
/// <param name="Value">
/// The confirmed value, or <see langword="null"/> while the specification is still being
/// decided. Nothing invents a value here: an unknown specification renders as the visible
/// <c>TEXT ABOUT HERE</c> placeholder instead of a plausible-looking number.
/// </param>
public sealed record HardwareSpec(HardwareSpecKind Kind, string? Value = null)
{
    /// <summary>Whether the value is still to be supplied.</summary>
    public bool IsPending => string.IsNullOrWhiteSpace(Value);
}

/// <summary>
/// A chapter of the infrastructure story, in page order.
/// </summary>
public enum InfrastructureTopic
{
    Compute,
    Memory,
    Storage,
    Network,
    Allocation,
    Reliability
}

/// <summary>
/// Which composition a chapter is rendered with.
/// </summary>
/// <remarks>
/// Recorded as data rather than hard-coded per section so the page keeps a deliberate rhythm -
/// no two consecutive chapters share a layout, and none of them is "another identical card".
/// </remarks>
public enum InfrastructureLayout
{
    /// <summary>Text on one side, a technical readout on the other.</summary>
    EditorialSplit,

    /// <summary>An oversized figure area with the copy set against it.</summary>
    SpecFigure,

    /// <summary>Stacked layers, drawn to scale.</summary>
    LayerDiagram,

    /// <summary>A left-to-right path with labelled hops.</summary>
    FlowDiagram,

    /// <summary>A capacity bar carved into instances.</summary>
    AllocationMap,

    /// <summary>A vertical rail of operational signals.</summary>
    OperationsRail
}

/// <summary>
/// One chapter of the infrastructure page.
/// </summary>
/// <param name="Id">Slug used for the section element id and in-page links.</param>
/// <param name="Index">Two-digit ordinal shown in the kicker.</param>
/// <param name="Topic">Which subject the chapter covers.</param>
/// <param name="Layout">The composition to render it with.</param>
/// <param name="Mirrored">Flips an asymmetric layout, so alternating chapters do not line up.</param>
public sealed record InfrastructureChapter(
    string Id,
    string Index,
    InfrastructureTopic Topic,
    InfrastructureLayout Layout,
    bool Mirrored = false);

/// <summary>
/// A node in the platform topology, as drawn on the infrastructure page.
/// </summary>
/// <param name="Ordinal">Two-digit node number, e.g. <c>01</c>.</param>
/// <param name="Health">Commissioning state.</param>
/// <param name="Specs">Specification lines, pending until hardware is confirmed.</param>
/// <param name="LoadPercent">
/// Allocation between 0 and 100, or <see langword="null"/> while the node is not commissioned
/// and there is no real figure to show.
/// </param>
public sealed record HardwareNode(
    string Ordinal,
    NodeHealth Health,
    IReadOnlyList<HardwareSpec> Specs,
    int? LoadPercent = null)
{
    /// <summary>Whether a real allocation figure is available.</summary>
    public bool HasKnownLoad => LoadPercent is not null;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
