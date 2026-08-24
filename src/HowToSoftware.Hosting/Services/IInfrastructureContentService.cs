using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Supplies the structure of the infrastructure page: which specification lines exist, which
/// nodes are drawn, and in what order the chapters run.
/// </summary>
/// <remarks>
/// It supplies structure only - never prose. Headings and labels come from the localisation
/// resources, and any value that has not been confirmed yet stays <see langword="null"/> so the
/// page renders a visible placeholder rather than a convincing invention. When real node data
/// arrives, a panel-backed implementation replaces this one and the page starts showing
/// figures without a markup change.
/// </remarks>
public interface IInfrastructureContentService
{
    /// <summary>The specification lines reported for the platform as a whole.</summary>
    IReadOnlyList<HardwareSpec> SpecSheet { get; }

    /// <summary>Nodes drawn in the topology.</summary>
    IReadOnlyList<HardwareNode> Nodes { get; }

    /// <summary>Chapters of the page, in the order they are rendered.</summary>
    IReadOnlyList<InfrastructureChapter> Chapters { get; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
