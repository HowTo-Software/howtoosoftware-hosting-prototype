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
/// <param name="LoadPercent">
/// Allocation load between 0 and 100, or <see langword="null"/> when there is no live figure.
/// </param>
/// <param name="Worlds">
/// Worlds currently placed on the node, or <see langword="null"/> when unknown.
/// </param>
/// <remarks>
/// Load and world count are live values that a statically-rendered page cannot keep current, so
/// both are optional. A stale number invented at build time is worse than no number: it is read
/// as a fact about the platform right now.
/// </remarks>
public sealed record InfrastructureNode(
    string Id,
    string Region,
    IReadOnlyList<PlanSpec> Specs,
    NodeHealth Health,
    int? LoadPercent = null,
    int? Worlds = null);

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
/// Something that happens to a server over its life, and what it does to the world on it.
/// </summary>
/// <param name="Tag">Short mono label, e.g. RESTART.</param>
/// <param name="Detail">One line saying what the world does while that happens.</param>
/// <param name="Kind">Whether the instance is interrupted, or the world is checkpointed.</param>
public sealed record WorldEvent(string Tag, string Detail, WorldEventKind Kind);

/// <summary>
/// What an event does to the two lanes of the continuity figure.
/// </summary>
public enum WorldEventKind
{
    /// <summary>The instance underneath stops and comes back. The world lane is untouched.</summary>
    InstanceInterrupted,

    /// <summary>A restore point. Marked on the world lane; the instance keeps running.</summary>
    WorldCheckpoint
}
