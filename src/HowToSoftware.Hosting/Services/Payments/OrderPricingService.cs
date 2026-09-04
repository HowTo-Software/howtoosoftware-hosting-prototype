using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services.Payments;

/// <summary>
/// A plan priced for a billing period, with the game it belongs to.
/// </summary>
/// <param name="Game">The game the plan is sold for.</param>
/// <param name="Plan">The plan.</param>
/// <param name="Quote">What the period costs.</param>
/// <param name="CurrencyCode">ISO 4217 code, lower case.</param>
public sealed record PricedPlan(HostedGame Game, HostingPlan Plan, BillingQuote Quote, string CurrencyCode);

/// <summary>
/// The one place a purchase is priced.
/// </summary>
/// <remarks>
/// <para>
/// The browser sends a game, a plan and a period. It never sends a price, and if it did the
/// figure would be ignored: the review page, the Checkout session and the webhook's check all
/// price the order through this service, from the catalogue and <see cref="BillingPolicy"/>,
/// so the number the customer saw, the number Stripe charged and the number the server
/// verifies are the same computation run three times.
/// </para>
/// </remarks>
public interface IOrderPricingService
{
    /// <summary>
    /// Prices a plan on a game for a period.
    /// </summary>
    /// <returns>
    /// The priced plan, or <see langword="null"/> when the game is unknown or not on sale, the
    /// plan is unknown or does not belong to the game, or the plan has no configured price.
    /// </returns>
    PricedPlan? Price(string? gameSlug, string? planSlug, BillingPeriod period);
}

/// <summary>
/// <see cref="IOrderPricingService"/> over the game and plan catalogues.
/// </summary>
public sealed class OrderPricingService : IOrderPricingService
{
    private readonly IGameCatalogService _games;
    private readonly IPlanCatalogService _plans;

    /// <summary>Creates the service.</summary>
    public OrderPricingService(IGameCatalogService games, IPlanCatalogService plans)
    {
        _games = games;
        _plans = plans;
    }

    /// <inheritdoc />
    public PricedPlan? Price(string? gameSlug, string? planSlug, BillingPeriod period)
    {
        var game = _games.Find(gameSlug);

        if (game is null || !game.IsAvailable || game.TemplateId is null)
        {
            return null;
        }

        var plan = _plans.FindBySlug(planSlug);

        // A plan is only for sale on the game it was built for. Passing a Zomboid plan slug
        // with another game's slug is either a bug or somebody poking at the form.
        if (plan is null
            || !string.Equals(plan.GameTemplateId, game.TemplateId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (_plans.Quote(plan, period) is not { } quote)
        {
            return null;
        }

        return new PricedPlan(game, plan, quote, _plans.CurrencyCode);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
