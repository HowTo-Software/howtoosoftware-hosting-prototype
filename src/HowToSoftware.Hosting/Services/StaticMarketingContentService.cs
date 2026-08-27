using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Localization;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Prototype implementation of <see cref="IMarketingContentService"/> backed by compiled copy.
/// </summary>
/// <remarks>
/// <para>
/// Every collection here is placeholder content for the frontend prototype. Swap this
/// registration in <c>Program.cs</c> for a CMS- or database-backed implementation later; no
/// component needs to change.
/// </para>
/// <para>
/// The copy itself lives in <c>Localization/HomeText.resx</c> and its <c>pt-BR</c> sibling.
/// What stays in this file is structure - which items exist, in what order, with which
/// identifiers and figures - because that is the part a translation must not be able to break.
/// The lists are therefore built on access rather than once at construction: the culture is
/// chosen per request.
/// </para>
/// </remarks>
public sealed class StaticMarketingContentService : IMarketingContentService
{
    private readonly IStringLocalizer<CommonText> _text;
    private readonly IStringLocalizer<HomeText> _home;

    /// <summary>Creates the service.</summary>
    /// <param name="text">
    /// Shared UI strings, already resolved to the culture chosen for this request by the
    /// localisation middleware.
    /// </param>
    /// <param name="home">Homepage copy for the same culture.</param>
    public StaticMarketingContentService(IStringLocalizer<CommonText> text, IStringLocalizer<HomeText> home)
    {
        _text = text;
        _home = home;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Targets are structure and never change; only the labels follow the language. Section
    /// anchors are written as <c>/#section</c> because the header now rides on the sign-in and
    /// infrastructure pages too, where a bare <c>#section</c> would point at nothing.
    /// </remarks>
    public IReadOnlyList<NavigationLink> PrimaryNavigation =>
    [
        new(_text["Nav.World"], "/#world"),
        new(_text["Nav.Workshop"], "/#workshop"),
        new(_text["Nav.Provisioning"], "/#provisioning"),
        new(_text["Nav.ControlPanel"], "/#control-panel"),
        new(_text["Nav.Infrastructure"], "/infrastructure"),
        new(_text["Nav.Plans"], "/project-zomboid"),
        new(_text["Nav.Faq"], "/#faq")
    ];

    /// <inheritdoc />
    public IReadOnlyList<NavigationLink> ProductLinks =>
    [
        new(_text["Product.PersistentWorlds"], "/#world"),
        new(_text["Product.WorkshopSync"], "/#workshop"),
        new(_text["Product.ProvisioningPipeline"], "/#provisioning"),
        new(_text["Product.ControlPanel"], "/#control-panel"),
        new(_text["Product.Infrastructure"], "/infrastructure"),
        new(_text["Product.Plans"], "/project-zomboid")
    ];

    /// <inheritdoc />
    public IReadOnlyList<NavigationLink> CompanyLinks =>
    [
        new(_text["Company.WhatWeBuild"], "/#capabilities"),
        new(_text["Company.Faq"], "/#faq"),
        new(_text["Company.SignIn"], "/login"),
        new(_text["Company.GetStarted"], "/#get-started"),
        new(_text["Company.Contact"], "/#contact")
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> LegalPlaceholders =>
    [
        _text["Legal.Terms"],
        _text["Legal.Privacy"],
        _text["Legal.AcceptableUse"],
        _text["Legal.Status"]
    ];

    /// <inheritdoc />
    public IReadOnlyList<TelemetrySignal> HeroSignals =>
    [
        new(_home["Signal.Survivors"], "27 / 64", SignalTone.Data),
        new(_home["Signal.Workshop"], _home["Signal.Mods"], SignalTone.Routing),
        new(_home["Signal.Uptime"], "06D 14H", SignalTone.Primary)
    ];

    /// <inheritdoc />
    public IReadOnlyList<TelemetrySignal> TelemetryStrip =>
    [
        new(_home["Telemetry.GameBuild"], "42", SignalTone.Primary),
        new(_home["Telemetry.Systems"], _text["Status.Operational"], SignalTone.Data, ShowPulse: true),
        new(_home["Telemetry.SurvivorsOnline"], "27", SignalTone.Data),
        new(_home["Telemetry.WorkshopSynced"], "82 / 82", SignalTone.Routing),
        new(_home["Telemetry.WorldUptime"], "06D 14H", SignalTone.Primary),
        new(_home["Telemetry.NodeStatus"], _text["Status.Healthy"], SignalTone.Data, ShowPulse: true),
        new(_home["Telemetry.Allocation"], "203.0.113.24:16261", SignalTone.Routing),
        new(_home["Telemetry.Backup"], _home["Telemetry.BackupValue"], SignalTone.Primary)
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Every one of these is something the section copy already states happens - restarts,
    /// build changes, rebuilds of the instance, mod rollbacks and scheduled restore points.
    /// The figure illustrates that copy; it does not add a claim to it.
    /// </remarks>
    public IReadOnlyList<WorldEvent> WorldEvents =>
    [
        new(_home["Event.Restart.Tag"], _home["Event.Restart.Detail"],
            WorldEventKind.InstanceInterrupted),
        new(_home["Event.Build.Tag"], _home["Event.Build.Detail"],
            WorldEventKind.InstanceInterrupted),
        new(_home["Event.Restore.Tag"], _home["Event.Restore.Detail"],
            WorldEventKind.WorldCheckpoint),
        new(_home["Event.Rollback.Tag"], _home["Event.Rollback.Detail"],
            WorldEventKind.InstanceInterrupted),
        new(_home["Event.Rebuild.Tag"], _home["Event.Rebuild.Detail"],
            WorldEventKind.InstanceInterrupted)
    ];

    /// <inheritdoc />
    /// <remarks>Workshop items carry the mods' own names, which are never translated.</remarks>
    public IReadOnlyList<WorkshopItem> WorkshopItems { get; } =
    [
        new("More Traits", "2685168362", "4.2 MB", ModSyncState.Synced, 100),
        new("Common Sense", "2875848298", "11.8 MB", ModSyncState.Synced, 100),
        new("Vehicle Pack", "2735294323", "184 MB", ModSyncState.Synced, 100),
        new("Brita's Weapon Pack", "2857548524", "612 MB", ModSyncState.Syncing, 74),
        new("Authentic Z", "2882031000", "96 MB", ModSyncState.Syncing, 38),
        new("Eggon's Map Pack", "2463499011", "248 MB", ModSyncState.Queued, 0)
    ];

    /// <inheritdoc />
    /// <remarks>
    /// The diagram key is an identifier the provisioning diagram matches on, not a label, so it
    /// stays in English regardless of culture.
    /// </remarks>
    public IReadOnlyList<ProvisioningStage> ProvisioningStages =>
    [
        new("01", _home["Stage01.Title"], _home["Stage01.Detail"], "order"),
        new("02", _home["Stage02.Title"], _home["Stage02.Detail"], "billing"),
        new("03", _home["Stage03.Title"], _home["Stage03.Detail"], "node"),
        new("04", _home["Stage04.Title"], _home["Stage04.Detail"], "panel"),
        new("05", _home["Stage05.Title"], _home["Stage05.Detail"], "world")
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Two machines, because there are two. Neither carries a load percentage: that is a live
    /// figure a statically-rendered marketing page cannot keep current, and a number frozen at
    /// build time reads as a claim about the platform right now.
    /// </remarks>
    public IReadOnlyList<InfrastructureNode> Nodes =>
    [
        new("NODE / 01", _home["Node.RegionPrimary"], NodeSpecs, NodeHealth.Healthy),
        new("NODE / 02", _home["Node.RegionSecondary"], NodeSpecs, NodeHealth.Healthy)
    ];

    /// <inheritdoc />
    public IReadOnlyList<CapabilityStatement> Capabilities =>
    [
        new("01", _home["Cap01.Kicker"], _home["Cap01.Headline"], _home["Cap01.Body"],
            [
                new(_home["Cap01.Key1"], _home["Cap01.Value1"]),
                new(_home["Cap01.Key2"], _home["Cap01.Value2"])
            ]),
        new("02", _home["Cap02.Kicker"], _home["Cap02.Headline"], _home["Cap02.Body"],
            [
                new(_home["Cap02.Key1"], _home["Cap02.Value1"]),
                new(_home["Cap02.Key2"], _home["Cap02.Value2"])
            ]),
        new("03", _home["Cap03.Kicker"], _home["Cap03.Headline"], _home["Cap03.Body"],
            [
                new(_home["Cap03.Key1"], _home["Cap03.Value1"]),
                new(_home["Cap03.Key2"], _home["Cap03.Value2"])
            ]),
        new("04", _home["Cap04.Kicker"], _home["Cap04.Headline"], _home["Cap04.Body"],
            [
                new(_home["Cap04.Key1"], _home["Cap04.Value1"]),
                new(_home["Cap04.Key2"], _home["Cap04.Value2"])
            ])
    ];

    /// <inheritdoc />
    /// <remarks>Ids are stable slugs, not copy: they build the accordion's aria wiring.</remarks>
    public IReadOnlyList<FaqItem> Faqs =>
    [
        new("mods", _home["Faq.Mods.Question"], _home["Faq.Mods.Answer"]),
        new("build-42", _home["Faq.Build42.Question"], _home["Faq.Build42.Answer"]),
        new("panel", _home["Faq.Panel.Question"], _home["Faq.Panel.Answer"]),
        new("restart", _home["Faq.Restart.Question"], _home["Faq.Restart.Answer"]),
        new("automatic", _home["Faq.Automatic.Question"], _home["Faq.Automatic.Answer"]),
        new("pricing", _home["Faq.Pricing.Question"], _home["Faq.Pricing.Answer"]),
        new("migrate", _home["Faq.Migrate.Question"], _home["Faq.Migrate.Answer"])
    ];

    /// <summary>
    /// The specification lines every node panel shows. Identical across nodes because the two
    /// machines are identically specified - same board, same CPU, same memory, same storage
    /// class. Read from the running hosts rather than from a datasheet.
    /// </summary>
    private IReadOnlyList<PlanSpec> NodeSpecs =>
    [
        new(_home["Spec.Cpu"], _home["Node.CpuValue"]),
        new(_home["Spec.Memory"], _home["Node.MemoryValue"]),
        new(_home["Spec.Storage"], _home["Node.StorageValue"]),
        new(_home["Spec.Network"], _home["Node.NetworkValue"])
    ];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
