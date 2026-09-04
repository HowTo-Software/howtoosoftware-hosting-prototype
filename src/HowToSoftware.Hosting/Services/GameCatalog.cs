using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Localization;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The games a customer chooses between before they see a plan.
/// </summary>
public interface IGameCatalogService
{
    /// <summary>Every listed game, the primary product first.</summary>
    IReadOnlyList<HostedGame> Games { get; }

    /// <summary>The game the company leads with.</summary>
    HostedGame Primary { get; }

    /// <summary>Finds a game by slug.</summary>
    /// <returns>The game, or <see langword="null"/> when no game carries that slug.</returns>
    HostedGame? Find(string? slug);

    /// <summary>
    /// The cheapest monthly price among the game's priced plans, or <see langword="null"/> when
    /// the game has no plans or none of them is priced.
    /// </summary>
    decimal? StartingPrice(HostedGame game);

    /// <summary>The plans sold for a game, smallest first. Empty for a planned game.</summary>
    IReadOnlyList<HostingPlan> PlansFor(HostedGame game);
}

/// <summary>
/// The catalogue as it stands: one game on sale, one on the roadmap.
/// </summary>
/// <remarks>
/// <para>
/// Project Zomboid is the product. It is listed first, it is the only entry with plans, and it is
/// the only entry whose page exists. Minecraft is listed as planned because the platform's
/// Workshop tooling is being extended to CurseForge for it - that is a stated direction, not a
/// product, and the entry says so.
/// </para>
/// <para>
/// Adding a game later is one entry here plus a template and a set of plans naming it. Nothing
/// in the pages needs to change: they read this list.
/// </para>
/// </remarks>
public sealed class StaticGameCatalogService : IGameCatalogService
{
    /// <summary>Slug of the primary product.</summary>
    public const string ProjectZomboidSlug = "project-zomboid";

    /// <summary>Slug of the planned second game.</summary>
    public const string MinecraftSlug = "minecraft";

    private readonly IStringLocalizer<CheckoutText> _text;
    private readonly IPlanCatalogService _plans;

    /// <summary>Creates the catalogue.</summary>
    public StaticGameCatalogService(IStringLocalizer<CheckoutText> text, IPlanCatalogService plans)
    {
        _text = text;
        _plans = plans;
    }

    /// <inheritdoc />
    public IReadOnlyList<HostedGame> Games =>
    [
        Primary,
        new HostedGame
        {
            Slug = MinecraftSlug,
            Name = "Minecraft",
            Tagline = _text["Game.Minecraft.Tagline"],
            Summary = _text["Game.Minecraft.Summary"],
            Availability = GameAvailability.Planned
        }
    ];

    /// <inheritdoc />
    public HostedGame Primary => new()
    {
        Slug = ProjectZomboidSlug,
        Name = "Project Zomboid",
        Tagline = _text["Game.Zomboid.Tagline"],
        Summary = _text["Game.Zomboid.Summary"],
        Availability = GameAvailability.Available,
        TemplateId = GameTemplateCatalog.ProjectZomboidId,
        PageHref = SiteRoutes.ProjectZomboid,
        Highlights =
        [
            _text["Game.Zomboid.Highlight.Build42"],
            _text["Game.Zomboid.Highlight.Workshop"],
            _text["Game.Zomboid.Highlight.Panel"],
            _text["Game.Zomboid.Highlight.Deploy"]
        ]
    };

    /// <inheritdoc />
    public HostedGame? Find(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : Games.FirstOrDefault(game =>
                string.Equals(game.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public decimal? StartingPrice(HostedGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var priced = PlansFor(game).Where(plan => plan.IsPriced).ToArray();

        return priced.Length == 0 ? null : priced.Min(plan => plan.PriceMonthly);
    }

    /// <inheritdoc />
    public IReadOnlyList<HostingPlan> PlansFor(HostedGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.TemplateId is null)
        {
            return [];
        }

        return _plans.Plans
            .Where(plan => string.Equals(plan.GameTemplateId, game.TemplateId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
