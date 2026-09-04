namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Whether a game can be bought today.
/// </summary>
public enum GameAvailability
{
    /// <summary>Plans exist, provisioning exists, a customer can pay for it now.</summary>
    Available,

    /// <summary>
    /// On the roadmap. Listed so the catalogue shows where the platform is going, but it has no
    /// plans, no price and no button that starts a purchase.
    /// </summary>
    Planned
}

/// <summary>
/// A game the platform hosts, or intends to.
/// </summary>
/// <remarks>
/// <para>
/// This is the catalogue entry - the thing a visitor chooses before they see a single plan. It
/// carries no resource figures and no prices: those belong to the plans that name it, so a game
/// cannot advertise something its plans do not deliver.
/// </para>
/// <para>
/// A <see cref="GameAvailability.Planned"/> game is listed honestly as planned. It has no
/// <see cref="TemplateId"/> because nothing can deploy it yet, and no <see cref="PageHref"/>
/// because there is nothing to sell on the page it would have.
/// </para>
/// </remarks>
public sealed record HostedGame
{
    /// <summary>Stable slug: the route segment, the order's game id, and the Stripe metadata value.</summary>
    public required string Slug { get; init; }

    /// <summary>The game's name, as its studio spells it.</summary>
    public required string Name { get; init; }

    /// <summary>One line under the name.</summary>
    public required string Tagline { get; init; }

    /// <summary>Two or three sentences about hosting this game here.</summary>
    public required string Summary { get; init; }

    /// <summary>Whether it can be bought.</summary>
    public required GameAvailability Availability { get; init; }

    /// <summary>Short, factual points about what the hosting includes. Empty for a planned game.</summary>
    public IReadOnlyList<string> Highlights { get; init; } = [];

    /// <summary>
    /// The <see cref="GameTemplate"/> that deploys it, or <see langword="null"/> while planned.
    /// </summary>
    public string? TemplateId { get; init; }

    /// <summary>Route of the game's own page, or <see langword="null"/> while planned.</summary>
    public string? PageHref { get; init; }

    /// <summary>Whether a customer can start a purchase.</summary>
    public bool IsAvailable => Availability is GameAvailability.Available;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
