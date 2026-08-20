using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Supplies the structured content rendered by the marketing site.
/// </summary>
/// <remarks>
/// Components depend on this abstraction rather than on literal arrays so the prototype's
/// compiled copy can later be replaced by a CMS-, database- or localisation-backed
/// implementation without touching any markup.
/// </remarks>
public interface IMarketingContentService
{
    /// <summary>Links shown in the header navigation.</summary>
    IReadOnlyList<NavigationLink> PrimaryNavigation { get; }

    /// <summary>Product-related links shown in the footer.</summary>
    IReadOnlyList<NavigationLink> ProductLinks { get; }

    /// <summary>Company-related links shown in the footer.</summary>
    IReadOnlyList<NavigationLink> CompanyLinks { get; }

    /// <summary>Legal documents that are not published yet; rendered as placeholders.</summary>
    IReadOnlyList<string> LegalPlaceholders { get; }

    /// <summary>Readouts attached to the hero's connected UI fragments.</summary>
    IReadOnlyList<TelemetrySignal> HeroSignals { get; }

    /// <summary>Readings that scroll through the telemetry strip under the hero.</summary>
    IReadOnlyList<TelemetrySignal> TelemetryStrip { get; }

    /// <summary>Cells that make up the stylised world grid.</summary>
    IReadOnlyList<WorldCell> WorldCells { get; }

    /// <summary>Workshop items shown in the mod-synchronisation panel.</summary>
    IReadOnlyList<WorkshopItem> WorkshopItems { get; }

    /// <summary>Ordered stages of the provisioning story.</summary>
    IReadOnlyList<ProvisioningStage> ProvisioningStages { get; }

    /// <summary>Abstract node panels shown in the hardware section.</summary>
    IReadOnlyList<InfrastructureNode> Nodes { get; }

    /// <summary>Numbered editorial statements replacing the old feature-card grid.</summary>
    IReadOnlyList<CapabilityStatement> Capabilities { get; }

    /// <summary>Frequently asked questions.</summary>
    IReadOnlyList<FaqItem> Faqs { get; }
}
