using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Localization;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The infrastructure page's structure and the specifications behind it.
/// </summary>
/// <remarks>
/// <para>
/// The four platform specifications are now real: they were read from the running hosts rather
/// than taken from a datasheet, and nothing here is invented. Anything still undecided stays
/// <see langword="null"/> so the page renders the visible <c>TEXT ABOUT HERE</c> placeholder
/// instead of a plausible-looking figure.
/// </para>
/// <para>
/// What is deliberately absent: hostnames, addresses, serial numbers and live utilisation. This
/// page describes the class of machine a customer's world lands on; it is not an inventory of
/// the estate.
/// </para>
/// </remarks>
public sealed class StaticInfrastructureContentService : IInfrastructureContentService
{
    private readonly IStringLocalizer<HardwareText> _text;

    /// <summary>Creates the service.</summary>
    /// <param name="text">Hardware copy for the culture chosen for this request.</param>
    public StaticInfrastructureContentService(IStringLocalizer<HardwareText> text) => _text = text;

    /// <inheritdoc />
    public IReadOnlyList<HardwareSpec> SpecSheet =>
    [
        new(HardwareSpecKind.Cpu, _text["Node.CpuValue"]),
        new(HardwareSpecKind.Memory, _text["Node.MemoryValue"]),
        new(HardwareSpecKind.Storage, _text["Node.StorageValue"]),
        new(HardwareSpecKind.Network, _text["Node.NetworkValue"])
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Two machines, identically specified - same board, same CPU, same memory, same storage
    /// class. Neither carries an allocation figure: that is live data a statically rendered page
    /// cannot keep current, so the page draws an indeterminate bar rather than a number that
    /// would be stale the moment it shipped.
    /// </remarks>
    public IReadOnlyList<HardwareNode> Nodes =>
    [
        new("01", NodeHealth.Healthy, SpecSheet),
        new("02", NodeHealth.Healthy, SpecSheet)
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Indices start at 04: the hero, the specification sheet and the machines take 01-03.
    /// </remarks>
    public IReadOnlyList<InfrastructureChapter> Chapters { get; } =
    [
        new("compute", "04", InfrastructureTopic.Compute, InfrastructureLayout.SpecFigure),
        new("memory", "05", InfrastructureTopic.Memory, InfrastructureLayout.LayerDiagram, Mirrored: true),
        new("storage", "06", InfrastructureTopic.Storage, InfrastructureLayout.EditorialSplit),
        new("network", "07", InfrastructureTopic.Network, InfrastructureLayout.FlowDiagram),

        // The panel's state store and its release process are as much a part of how this
        // platform is arranged as the machines are, and a page that described the metal but not
        // how software reaches it was only telling half of it.
        new("database", "08", InfrastructureTopic.Database, InfrastructureLayout.LayerDiagram, Mirrored: true),
        new("deployment", "09", InfrastructureTopic.Deployment, InfrastructureLayout.FlowDiagram),

        new("allocation", "10", InfrastructureTopic.Allocation, InfrastructureLayout.AllocationMap, Mirrored: true),
        new("reliability", "11", InfrastructureTopic.Reliability, InfrastructureLayout.OperationsRail)
    ];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
