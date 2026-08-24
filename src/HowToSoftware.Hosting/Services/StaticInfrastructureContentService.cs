using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The prototype's infrastructure structure.
/// </summary>
/// <remarks>
/// <para>
/// <b>No hardware figures appear in this file, and none should.</b> CPU models, memory sizes,
/// storage layout, link speeds, datacentre locations and uptime numbers have not been supplied
/// yet, so every specification is left pending and the page shows the <c>TEXT ABOUT HERE</c>
/// placeholder in its place. Filling one in here is how a placeholder quietly becomes a claim.
/// </para>
/// <para>
/// Node identity and health mirror the node panels already on the homepage, so the two pages
/// describe the same platform.
/// </para>
/// </remarks>
public sealed class StaticInfrastructureContentService : IInfrastructureContentService
{
    private static readonly HardwareSpec[] PendingSpecSheet =
    [
        new(HardwareSpecKind.Cpu),
        new(HardwareSpecKind.Memory),
        new(HardwareSpecKind.Storage),
        new(HardwareSpecKind.Network)
    ];

    /// <inheritdoc />
    public IReadOnlyList<HardwareSpec> SpecSheet => PendingSpecSheet;

    /// <inheritdoc />
    public IReadOnlyList<HardwareNode> Nodes { get; } =
    [
        new("01", NodeHealth.Healthy, PendingSpecSheet),
        new("02", NodeHealth.Healthy, PendingSpecSheet),
        new("03", NodeHealth.Reserved, PendingSpecSheet)
    ];

    /// <inheritdoc />
    public IReadOnlyList<InfrastructureChapter> Chapters { get; } =
    [
        new("compute", "03", InfrastructureTopic.Compute, InfrastructureLayout.SpecFigure),
        new("memory", "04", InfrastructureTopic.Memory, InfrastructureLayout.LayerDiagram, Mirrored: true),
        new("storage", "05", InfrastructureTopic.Storage, InfrastructureLayout.EditorialSplit),
        new("network", "06", InfrastructureTopic.Network, InfrastructureLayout.FlowDiagram),
        new("allocation", "07", InfrastructureTopic.Allocation, InfrastructureLayout.AllocationMap, Mirrored: true),
        new("reliability", "08", InfrastructureTopic.Reliability, InfrastructureLayout.OperationsRail)
    ];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
