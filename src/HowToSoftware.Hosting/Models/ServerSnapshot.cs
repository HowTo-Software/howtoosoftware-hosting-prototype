namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Power state of a game server instance.
/// </summary>
public enum ServerState
{
    Offline,
    Starting,
    Online,
    Restarting,
    Stopping
}

/// <summary>
/// A power action a customer can trigger from the control panel.
/// </summary>
public enum ServerCommand
{
    Start,
    Restart,
    Stop
}

/// <summary>
/// Severity of a console line, used purely for colour coding in the preview.
/// </summary>
public enum ConsoleSeverity
{
    Info,
    Success,
    Warning
}

/// <summary>
/// A single resource gauge (CPU, memory, storage, …) shown in the control panel.
/// </summary>
/// <param name="Label">Gauge name.</param>
/// <param name="Icon">Glyph shown beside the label.</param>
/// <param name="Percent">Utilisation between 0 and 100.</param>
/// <param name="Display">Pre-formatted primary value, e.g. <c>"6.2 GB"</c>.</param>
/// <param name="Detail">Secondary caption, e.g. <c>"of 12 GB allocated"</c>.</param>
public sealed record ResourceMetric(
    string Label,
    IconName Icon,
    int Percent,
    string Display,
    string Detail);

/// <summary>
/// A single line of server console output.
/// </summary>
/// <param name="Timestamp">Formatted clock value, e.g. <c>"21:04:12"</c>.</param>
/// <param name="Source">Subsystem tag rendered in brackets, e.g. <c>WORKSHOP</c>.</param>
/// <param name="Severity">Controls the colour of the line.</param>
/// <param name="Message">The log message.</param>
public sealed record ConsoleEntry(string Timestamp, string Source, ConsoleSeverity Severity, string Message);

/// <summary>
/// An immutable point-in-time view of a Project Zomboid server, as surfaced by
/// <see cref="Services.IServerPreviewService"/>.
/// </summary>
/// <remarks>
/// The shape intentionally mirrors the data a panel API (Pterodactyl) would return, so the
/// prototype's in-memory implementation can be replaced without changing any component.
/// </remarks>
public sealed record ServerSnapshot
{
    /// <summary>Display name of the server instance.</summary>
    public required string Name { get; init; }

    /// <summary>Connection endpoint shown to the customer.</summary>
    public required string Address { get; init; }

    /// <summary>Current power state.</summary>
    public required ServerState State { get; init; }

    /// <summary>Game build the instance is running.</summary>
    public required string Build { get; init; }

    /// <summary>Deployment region label.</summary>
    public required string Region { get; init; }

    /// <summary>Number of players currently connected.</summary>
    public required int PlayersOnline { get; init; }

    /// <summary>Maximum configured player slots.</summary>
    public required int PlayerCapacity { get; init; }

    /// <summary>Human-readable uptime, e.g. <c>"14d 06h"</c>.</summary>
    public required string Uptime { get; init; }

    /// <summary>Resource gauges rendered in the control panel.</summary>
    public required IReadOnlyList<ResourceMetric> Metrics { get; init; }

    /// <summary>Most recent console output, oldest first.</summary>
    public required IReadOnlyList<ConsoleEntry> Console { get; init; }

    /// <summary>Whether the instance is in a transitional (busy) state.</summary>
    public bool IsTransitioning =>
        State is ServerState.Starting or ServerState.Restarting or ServerState.Stopping;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
