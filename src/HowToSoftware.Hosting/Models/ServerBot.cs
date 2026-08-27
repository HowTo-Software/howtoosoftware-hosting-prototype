namespace HowToSoftware.Hosting.Models;

/// <summary>
/// The HowToSoftware server bot: what it is offered on, and what it is said to do.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything this file asserts is everything that has been confirmed.</b> The bot is our own
/// server-side management software; it is offered on servers configured with more than 5 GB of
/// memory; and it exists to help monitor and maintain server performance and to reduce
/// performance degradation as a world grows.
/// </para>
/// <para>
/// What it deliberately does not carry: optimisation percentages, uptime guarantees, "zero lag",
/// automatic fixes, or any figure describing how much it improves anything. Those numbers have
/// not been measured, and a marketing page is the worst place to find that out.
/// </para>
/// </remarks>
public static class ServerBot
{
    /// <summary>
    /// Memory, in GiB, a server must exceed for the bot to be offered on it.
    /// </summary>
    /// <remarks>
    /// Strictly greater than. The 5 GB tier does not qualify; 6 GB is the first that does.
    /// </remarks>
    public const int MinimumMemoryGb = 5;

    /// <summary>Whether a server of this size is offered the bot.</summary>
    /// <param name="memoryGb">Server memory in GiB.</param>
    /// <returns><see langword="true"/> when the server exceeds the threshold.</returns>
    public static bool IsEligible(int memoryGb) => memoryGb > MinimumMemoryGb;
}

/// <summary>
/// One line of the bot's system diagram.
/// </summary>
/// <param name="Label">What the line watches, e.g. MEMORY.</param>
/// <param name="State">The word shown against it, e.g. MONITORED.</param>
/// <param name="Kind">How the line is drawn.</param>
/// <remarks>
/// The states are descriptions of what the software attends to, not readings. Nothing here is
/// wired to a running server, and the section says so on the page rather than only in a comment.
/// </remarks>
public sealed record BotChannel(string Label, string State, BotChannelKind Kind);

/// <summary>How a bot channel is drawn.</summary>
public enum BotChannelKind
{
    /// <summary>Something the bot observes.</summary>
    Watched,

    /// <summary>Something the bot acts on.</summary>
    Managed,

    /// <summary>The bot's own link to the server.</summary>
    Link
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
