using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Supplies the Project Zomboid plan catalogue and the promotional-code check behind it.
/// </summary>
/// <remarks>
/// Everything this returns is placeholder test data. Pricing, discounts and promotional codes
/// are not commercially agreed, and no payment or entitlement logic exists behind them - the
/// catalogue is a frontend prototype only. A billing-backed implementation can replace this
/// registration without changing <c>PlansSection</c>.
/// </remarks>
public interface IPlanCatalogService
{
    /// <summary>Currency symbol used for display.</summary>
    string CurrencySymbol { get; }

    /// <summary>The plans on offer, cheapest first.</summary>
    IReadOnlyList<HostingPlan> Plans { get; }

    /// <summary>Billing cadences the visitor can switch between.</summary>
    IReadOnlyList<BillingOption> BillingOptions { get; }

    /// <summary>
    /// Effective per-month rate for a plan once the period discount and any applied coupon are
    /// taken into account.
    /// </summary>
    /// <param name="plan">Plan being priced.</param>
    /// <param name="period">Selected billing cadence.</param>
    /// <param name="coupon">Result of the current promotional-code check.</param>
    decimal GetMonthlyRate(HostingPlan plan, BillingPeriod period, CouponResult coupon);

    /// <summary>Total charged up front for one billing cycle.</summary>
    /// <param name="plan">Plan being priced.</param>
    /// <param name="period">Selected billing cadence.</param>
    /// <param name="coupon">Result of the current promotional-code check.</param>
    decimal GetCycleTotal(HostingPlan plan, BillingPeriod period, CouponResult coupon);

    /// <summary>Looks up a billing option by period.</summary>
    /// <param name="period">Cadence to resolve.</param>
    BillingOption GetOption(BillingPeriod period);

    /// <summary>
    /// Checks a promotional code against the demo list. This performs no network call and
    /// grants no entitlement; it exists so the catalogue mechanic can be reviewed.
    /// </summary>
    /// <param name="code">Raw text from the coupon field.</param>
    CouponResult CheckCoupon(string? code);
}
