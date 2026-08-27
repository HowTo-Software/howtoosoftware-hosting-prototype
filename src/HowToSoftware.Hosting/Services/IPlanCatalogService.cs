using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Supplies the Project Zomboid plan catalogue: the resources each plan is provisioned with, and
/// the pricing presentation around them.
/// </summary>
/// <remarks>
/// <para>
/// This is the single source of truth for plan resources. Components read from it and never
/// restate a memory or CPU figure of their own, and <c>ProvisioningService</c> reads the same
/// records when it creates a server - so what the catalogue advertises and what Pterodactyl is
/// asked for cannot drift apart.
/// </para>
/// <para>
/// Prices come from configuration and may be absent; discounts and promotional codes remain
/// display-only, with no payment or entitlement logic behind them.
/// </para>
/// </remarks>
public interface IPlanCatalogService
{
    /// <summary>Currency symbol used for display.</summary>
    string CurrencySymbol { get; }

    /// <summary>The plans on offer, smallest allocation first.</summary>
    IReadOnlyList<HostingPlan> Plans { get; }

    /// <summary>Billing cadences the visitor can switch between.</summary>
    IReadOnlyList<BillingOption> BillingOptions { get; }

    /// <summary>Whether any plan has a configured price.</summary>
    /// <remarks>
    /// The billing switch and the promotional-code field are meaningless against a catalogue
    /// with no prices in it, so the section hides them rather than offering controls that
    /// cannot change anything.
    /// </remarks>
    bool HasPricing { get; }

    /// <summary>Finds a plan by its slug.</summary>
    /// <param name="slug">Plan slug, e.g. <c>zomboid-4gb</c>.</param>
    /// <returns>The plan, or <see langword="null"/> when no plan carries that slug.</returns>
    HostingPlan? FindBySlug(string? slug);

    /// <summary>
    /// Effective per-month rate once the period discount and any applied coupon are taken into
    /// account, or <see langword="null"/> when the plan has no configured price.
    /// </summary>
    /// <param name="plan">Plan being priced.</param>
    /// <param name="period">Selected billing cadence.</param>
    /// <param name="coupon">Result of the current promotional-code check.</param>
    decimal? GetMonthlyRate(HostingPlan plan, BillingPeriod period, CouponResult coupon);

    /// <summary>
    /// Total charged up front for one billing cycle, or <see langword="null"/> when unpriced.
    /// </summary>
    /// <param name="plan">Plan being priced.</param>
    /// <param name="period">Selected billing cadence.</param>
    /// <param name="coupon">Result of the current promotional-code check.</param>
    decimal? GetCycleTotal(HostingPlan plan, BillingPeriod period, CouponResult coupon);

    /// <summary>
    /// What a month costs after the first one, for a rate already worked out.
    /// </summary>
    /// <param name="firstMonth">The rate charged for the first month.</param>
    /// <returns>The reduced rate, or <see langword="null"/> when there is no renewal discount.</returns>
    decimal? GetRenewalRate(decimal firstMonth);

    /// <summary>The percentage taken off every month after the first.</summary>
    decimal RenewalDiscountPercent { get; }

    /// <summary>Looks up a billing option by period.</summary>
    /// <param name="period">Cadence to resolve.</param>
    BillingOption GetOption(BillingPeriod period);

    /// <summary>
    /// Checks a promotional code against the demo list. This performs no network call and grants
    /// no entitlement; it exists so the catalogue mechanic can be reviewed.
    /// </summary>
    /// <param name="code">Raw text from the coupon field.</param>
    CouponResult CheckCoupon(string? code);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
