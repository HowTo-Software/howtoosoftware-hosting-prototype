namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Which brand hue a telemetry value is rendered in.
/// </summary>
public enum SignalTone
{
    /// <summary>Light blue - measurements and technical data.</summary>
    Data,

    /// <summary>Purple - platform and routing state.</summary>
    Routing,

    /// <summary>White - headline information.</summary>
    Primary
}

/// <summary>
/// One reading in the telemetry strip that runs under the hero.
/// </summary>
/// <param name="Label">Small caps label, e.g. <c>WORLD UPTIME</c>.</param>
/// <param name="Value">The reading itself.</param>
/// <param name="Tone">Colour role for the value.</param>
/// <param name="ShowPulse">Renders a pulsing indicator before the label.</param>
public sealed record TelemetrySignal(string Label, string Value, SignalTone Tone, bool ShowPulse = false);

/// <summary>
/// Synchronisation state of a Workshop item.
/// </summary>
public enum ModSyncState
{
    Synced,
    Syncing,
    Queued
}

/// <summary>
/// A Steam Workshop item shown in the mod-synchronisation panel.
/// </summary>
/// <param name="Name">Workshop item name.</param>
/// <param name="WorkshopId">Workshop identifier.</param>
/// <param name="SizeLabel">Pre-formatted download size.</param>
/// <param name="State">Current sync state.</param>
/// <param name="Progress">Completion between 0 and 100.</param>
public sealed record WorkshopItem(
    string Name,
    string WorkshopId,
    string SizeLabel,
    ModSyncState State,
    int Progress);

/// <summary>
/// Health of a physical node in the topology.
/// </summary>
public enum NodeHealth
{
    Healthy,
    Provisioning,
    Reserved
}

/// <summary>
/// An abstract representation of a hosting node.
/// </summary>
/// <param name="Id">Node label, e.g. <c>NODE / 01</c>.</param>
/// <param name="Region">Deployment region.</param>
/// <param name="Specs">Hardware allocation lines.</param>
/// <param name="Health">Current health.</param>
/// <param name="LoadPercent">Allocation load between 0 and 100.</param>
/// <param name="Worlds">Number of Project Zomboid worlds currently placed on the node.</param>
public sealed record InfrastructureNode(
    string Id,
    string Region,
    IReadOnlyList<PlanSpec> Specs,
    NodeHealth Health,
    int LoadPercent,
    int Worlds);

/// <summary>
/// One stage of the provisioning story told by the sticky scroll section.
/// </summary>
/// <param name="Code">Two-digit ordinal, e.g. <c>03</c>.</param>
/// <param name="Title">Stage name in caps.</param>
/// <param name="Detail">What happens during the stage.</param>
/// <param name="DiagramKey">Identifies which diagram nodes light up for this stage.</param>
public sealed record ProvisioningStage(string Code, string Title, string Detail, string DiagramKey);

/// <summary>
/// A numbered editorial block in the capabilities section.
/// </summary>
/// <param name="Index">Two-digit ordinal, e.g. <c>02</c>.</param>
/// <param name="Kicker">Small caps category, e.g. <c>WORKSHOP</c>.</param>
/// <param name="Headline">Large display headline, kept deliberately short.</param>
/// <param name="Body">Supporting paragraph.</param>
/// <param name="Readout">Short technical readout rendered beside the block.</param>
public sealed record CapabilityStatement(
    string Index,
    string Kicker,
    string Headline,
    string Body,
    IReadOnlyList<PlanSpec> Readout);

/// <summary>
/// A coordinate cell in the stylised Project Zomboid world grid.
/// </summary>
/// <param name="X">Column, 1-based.</param>
/// <param name="Y">Row, 1-based.</param>
/// <param name="Kind">What the cell represents.</param>
/// <param name="Label">Optional label drawn inside the cell.</param>
public sealed record WorldCell(int X, int Y, WorldCellKind Kind, string? Label = null);

/// <summary>
/// What a world-grid cell represents.
/// </summary>
public enum WorldCellKind
{
    Empty,
    Loaded,
    Player,
    Safehouse,
    Server
}
