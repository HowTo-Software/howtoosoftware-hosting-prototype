using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Prototype implementation of <see cref="IMarketingContentService"/> backed by compiled copy.
/// </summary>
/// <remarks>
/// Every collection here is placeholder content for the frontend prototype. Swap this
/// registration in <c>Program.cs</c> for a CMS- or database-backed implementation later; no
/// component needs to change.
/// </remarks>
public sealed class StaticMarketingContentService : IMarketingContentService
{
    /// <inheritdoc />
    public IReadOnlyList<NavigationLink> PrimaryNavigation { get; } =
    [
        new("World", "#world"),
        new("Workshop", "#workshop"),
        new("Provisioning", "#provisioning"),
        new("Control panel", "#control-panel"),
        new("Plans", "#plans"),
        new("FAQ", "#faq")
    ];

    /// <inheritdoc />
    public IReadOnlyList<NavigationLink> ProductLinks { get; } =
    [
        new("Persistent worlds", "#world"),
        new("Workshop sync", "#workshop"),
        new("Provisioning pipeline", "#provisioning"),
        new("Control panel", "#control-panel"),
        new("Nodes", "#nodes"),
        new("Plans", "#plans")
    ];

    /// <inheritdoc />
    public IReadOnlyList<NavigationLink> CompanyLinks { get; } =
    [
        new("What we build", "#capabilities"),
        new("Frequently asked questions", "#faq"),
        new("Get started", "#get-started"),
        new("Contact us", "#contact")
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> LegalPlaceholders { get; } =
    [
        "Terms of Service",
        "Privacy Policy",
        "Acceptable Use Policy",
        "Service Status"
    ];

    /// <inheritdoc />
    public IReadOnlyList<TelemetrySignal> HeroSignals { get; } =
    [
        new("SURVIVORS", "27 / 64", SignalTone.Data),
        new("WORKSHOP", "82 MODS", SignalTone.Routing),
        new("UPTIME", "06D 14H", SignalTone.Primary)
    ];

    /// <inheritdoc />
    public IReadOnlyList<TelemetrySignal> TelemetryStrip { get; } =
    [
        new("GAME BUILD", "42", SignalTone.Primary),
        new("SYSTEMS", "OPERATIONAL", SignalTone.Data, ShowPulse: true),
        new("SURVIVORS ONLINE", "27", SignalTone.Data),
        new("WORKSHOP SYNCED", "82 / 82", SignalTone.Routing),
        new("WORLD UPTIME", "06D 14H", SignalTone.Primary),
        new("NODE STATUS", "HEALTHY", SignalTone.Data, ShowPulse: true),
        new("ALLOCATION", "203.0.113.24:16261", SignalTone.Routing),
        new("BACKUP", "42 MIN AGO", SignalTone.Primary)
    ];

    /// <inheritdoc />
    public IReadOnlyList<WorldCell> WorldCells { get; } =
    [
        new(2, 1, WorldCellKind.Loaded),
        new(3, 1, WorldCellKind.Loaded),
        new(5, 1, WorldCellKind.Player, "SURVIVOR"),
        new(1, 2, WorldCellKind.Loaded),
        new(2, 2, WorldCellKind.Safehouse, "SAFEHOUSE"),
        new(3, 2, WorldCellKind.Loaded),
        new(4, 2, WorldCellKind.Loaded),
        new(6, 2, WorldCellKind.Loaded),
        new(2, 3, WorldCellKind.Loaded),
        new(4, 3, WorldCellKind.Server, "WORLD STATE"),
        new(5, 3, WorldCellKind.Loaded),
        new(7, 3, WorldCellKind.Player, "SURVIVOR"),
        new(1, 4, WorldCellKind.Loaded),
        new(3, 4, WorldCellKind.Loaded),
        new(4, 4, WorldCellKind.Loaded),
        new(6, 4, WorldCellKind.Loaded),
        new(3, 5, WorldCellKind.Player, "SURVIVOR"),
        new(5, 5, WorldCellKind.Loaded)
    ];

    /// <inheritdoc />
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
    public IReadOnlyList<ProvisioningStage> ProvisioningStages { get; } =
    [
        new("01", "ORDER RECEIVED",
            "Your configuration is captured the moment you confirm it - region, slot count, sandbox rules and mod list all travel together as one order.",
            "order"),
        new("02", "PAYMENT CONFIRMED",
            "The order is released to the provisioner as soon as billing clears. Nothing waits in a queue for a human to approve it.",
            "billing"),
        new("03", "NODE SELECTED",
            "The platform picks a healthy node with enough headroom for your allocation and reserves your CPU, memory and storage before anything is installed.",
            "node"),
        new("04", "PANEL PROVISIONING",
            "The game panel builds the instance: runtime, game files, your Workshop collection and your server configuration, applied in one pass.",
            "panel"),
        new("05", "WORLD ONLINE",
            "Health checks pass, the world generates, and the connection details come straight back to you. No ticket, no hand-off.",
            "world")
    ];

    /// <inheritdoc />
    public IReadOnlyList<InfrastructureNode> Nodes { get; } =
    [
        new("NODE / 01", "EU WEST",
            [
                new("CPU", "High-frequency dedicated cores"),
                new("MEMORY", "256 GB ECC"),
                new("STORAGE", "NVMe array"),
                new("NETWORK", "Redundant uplink")
            ],
            NodeHealth.Healthy, 62, 24),
        new("NODE / 02", "EU WEST",
            [
                new("CPU", "High-frequency dedicated cores"),
                new("MEMORY", "256 GB ECC"),
                new("STORAGE", "NVMe array"),
                new("NETWORK", "Redundant uplink")
            ],
            NodeHealth.Healthy, 41, 17),
        new("NODE / 03", "PENDING",
            [
                new("CPU", "High-frequency dedicated cores"),
                new("MEMORY", "256 GB ECC"),
                new("STORAGE", "NVMe array"),
                new("NETWORK", "Redundant uplink")
            ],
            NodeHealth.Reserved, 0, 0)
    ];

    /// <inheritdoc />
    public IReadOnlyList<CapabilityStatement> Capabilities { get; } =
    [
        new("01", "WORKSHOP",
            "82 mods? Good.",
            "A large Workshop collection should not turn server setup into a second job. Paste a collection, and the platform resolves the item list, writes the mod and Workshop identifiers in the right order, and keeps them in step every time you change something.",
            [new("RESOLVED", "AUTOMATIC"), new("LOAD ORDER", "EDITABLE")]),
        new("02", "PERSISTENCE",
            "Your world outlives the server.",
            "Map state, safehouses and loot live on storage that survives restarts, version changes and rebuilds of the instance underneath it. Rolling a mod back does not mean rolling your community back.",
            [new("WORLD DATA", "DURABLE"), new("RESTORE POINTS", "SCHEDULED")]),
        new("03", "CONTROL",
            "Every knob, none of the FTP.",
            "Sandbox rules, INI configuration, player permissions and the live console all sit in one panel. The things you would normally email support about are buttons instead.",
            [new("CONSOLE", "LIVE"), new("FILES", "DIRECT")]),
        new("04", "BUILD 42",
            "Versioned, not frozen.",
            "Server builds are handled as configurable environments rather than one fixed version, so an instance can follow the build your community actually plays. We will publish exactly which builds are selectable at launch.",
            [new("ENVIRONMENTS", "VERSIONED"), new("MIGRATION", "SUPPORTED")])
    ];

    /// <inheritdoc />
    public IReadOnlyList<FaqItem> Faqs { get; } =
    [
        new("mods", "Can I install Project Zomboid mods?",
            "Yes. Steam Workshop items and standalone mods can be added from the control panel, which keeps the Workshop and mod identifier lists in sync for you. Load order stays editable, so you can reorder or disable an item without rewriting server configuration by hand."),
        new("build-42", "Does the service support Build 42?",
            "Server builds are handled as configurable environments rather than one fixed version, so an instance can be pointed at the build your community plays. As Build 42 stabilises we will publish exactly which builds are selectable at launch."),
        new("panel", "Can I manage my server through a control panel?",
            "That is the core of the product. Power actions, resource usage, live console output, configuration and file access are all designed to live in a single web panel that works on desktop and on mobile."),
        new("restart", "Can the server be restarted remotely?",
            "Yes. Start, restart and stop actions are available from the panel at any time, and scheduled restarts can be configured so your world resets on a rhythm that suits your community."),
        new("automatic", "Will servers be deployed automatically?",
            "Deployment is an automated pipeline. Once a server is created the platform reserves capacity, installs the game files, applies your configuration and reports back when health checks pass, without manual intervention."),
        new("pricing", "Are the plans and prices on this page final?",
            "No. The catalogue on this page is placeholder test data used to review the interface, including the promotional-code field. Final plans, prices and discounts are still being decided and nothing here is a commercial offer."),
        new("migrate", "Can I migrate an existing server?",
            "Bringing an existing world across is a supported goal for launch. The intended flow is to upload your current save and configuration, then validate the world on the new instance before you point players at it. Talk to us about your setup and we will confirm the details.")
    ];
}
