namespace HowToSoftware.Hosting.Models;

/// <summary>
/// How often a hosting subscription is billed.
/// </summary>
/// <remarks>
/// The numeric values are stored in the order database and sent to Stripe as metadata, so they
/// are fixed here on purpose: reordering the members would silently re-label every existing
/// order.
/// </remarks>
public enum BillingPeriod
{
    /// <summary>One month at a time.</summary>
    Monthly = 0,

    /// <summary>Three months paid up front.</summary>
    Quarterly = 1,

    /// <summary>Twelve months paid up front.</summary>
    Annual = 2
}

/// <summary>
/// The one pricing policy the site has: three billing periods, each with a fixed discount.
/// </summary>
/// <remarks>
/// <para>
/// <b>These are the only discounts that exist.</b> Monthly is the list price. Quarterly takes
/// five per cent off three months paid together; annual takes ten per cent off twelve. There
/// is no first-month promotion, no renewal discount and no coupon, and this class is the single
/// place a percentage lives - the catalogue, the plan picker, the review step, the Stripe line
/// item and the webhook's price check all call into it. A percentage typed anywhere else would
/// be a second policy waiting to disagree with this one.
/// </para>
/// <para>
/// Deliberately code rather than configuration. A discount is a commercial rule that has been
/// decided, and a rule that can be changed by editing a JSON file on one host is a rule that
/// will one day differ between the page a customer read and the server that charged them.
/// </para>
/// <para>
/// Arithmetic: for a monthly list price <c>P</c>, the amount charged for a period is
/// <c>P × months × (100 − discount) / 100</c>, rounded to the cent away from zero at the end -
/// never per month and then multiplied, which would compound the rounding.
/// </para>
/// </remarks>
public static class BillingPolicy
{
    /// <summary>Every period the site sells, in the order they are offered.</summary>
    public static IReadOnlyList<BillingPeriod> Periods { get; } =
        [BillingPeriod.Monthly, BillingPeriod.Quarterly, BillingPeriod.Annual];

    /// <summary>How many months one payment covers.</summary>
    public static int Months(BillingPeriod period) => period switch
    {
        BillingPeriod.Monthly => 1,
        BillingPeriod.Quarterly => 3,
        BillingPeriod.Annual => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown billing period.")
    };

    /// <summary>The whole-number percentage taken off the period's list total.</summary>
    public static int DiscountPercent(BillingPeriod period) => period switch
    {
        BillingPeriod.Monthly => 0,
        BillingPeriod.Quarterly => 5,
        BillingPeriod.Annual => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown billing period.")
    };

    /// <summary>
    /// The identifier a period travels under in URLs, form fields and Stripe metadata.
    /// </summary>
    public static string Slug(BillingPeriod period) => period switch
    {
        BillingPeriod.Monthly => "monthly",
        BillingPeriod.Quarterly => "quarterly",
        BillingPeriod.Annual => "annual",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown billing period.")
    };

    /// <summary>
    /// Reads a period back from its slug. Case-insensitive, whitespace-tolerant, and strict
    /// about everything else: a query string is not a place to be generous.
    /// </summary>
    public static bool TryParse(string? value, out BillingPeriod period)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "monthly":
                period = BillingPeriod.Monthly;
                return true;
            case "quarterly":
                period = BillingPeriod.Quarterly;
                return true;
            case "annual":
                period = BillingPeriod.Annual;
                return true;
            default:
                period = BillingPeriod.Monthly;
                return false;
        }
    }

    /// <summary>
    /// Prices one billing period of a plan.
    /// </summary>
    /// <param name="monthlyPrice">The plan's list price per month.</param>
    /// <param name="period">The period being bought.</param>
    /// <returns>Every figure the customer is shown and the server later verifies.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The price is not positive.</exception>
    public static BillingQuote Quote(decimal monthlyPrice, BillingPeriod period)
    {
        if (monthlyPrice <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice), monthlyPrice, "A quote needs a positive monthly price.");
        }

        var months = Months(period);
        var percent = DiscountPercent(period);

        var baseAmount = RoundToCent(monthlyPrice * months);
        var finalAmount = RoundToCent(monthlyPrice * months * (100 - percent) / 100m);

        return new BillingQuote(
            Period: period,
            Months: months,
            DiscountPercent: percent,
            MonthlyPrice: monthlyPrice,
            BaseAmount: baseAmount,
            DiscountAmount: baseAmount - finalAmount,
            FinalAmount: finalAmount);
    }

    /// <summary>Rounds a currency amount to the cent, away from zero on the half.</summary>
    public static decimal RoundToCent(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// What one billing period of a plan costs, and how the figure was arrived at.
/// </summary>
/// <param name="Period">The period quoted.</param>
/// <param name="Months">Months the payment covers.</param>
/// <param name="DiscountPercent">Percentage taken off the list total; zero for monthly.</param>
/// <param name="MonthlyPrice">The plan's list price per month.</param>
/// <param name="BaseAmount">The list price multiplied by the months, before discount.</param>
/// <param name="DiscountAmount">What the discount takes off, in currency.</param>
/// <param name="FinalAmount">What is actually charged for the period.</param>
public sealed record BillingQuote(
    BillingPeriod Period,
    int Months,
    int DiscountPercent,
    decimal MonthlyPrice,
    decimal BaseAmount,
    decimal DiscountAmount,
    decimal FinalAmount)
{
    /// <summary>Whether a discount applies at all - monthly does not, and must not say it does.</summary>
    public bool IsDiscounted => DiscountPercent > 0;

    /// <summary>The final amount spread over the months it covers, for the "per month" line.</summary>
    public decimal EffectiveMonthly => BillingPolicy.RoundToCent(FinalAmount / Months);

    /// <summary>
    /// The final amount in the currency's minor unit, which is what Stripe takes.
    /// </summary>
    /// <remarks>
    /// Assumes a two-decimal currency (USD, BRL, EUR). A zero-decimal currency such as JPY would
    /// need a different multiplier, and the site does not sell in one.
    /// </remarks>
    public long FinalAmountMinor => (long)Math.Round(FinalAmount * 100m, MidpointRounding.AwayFromZero);
}

/// <summary>
/// A billing period as the plan picker presents it: the policy figures plus a localised label.
/// </summary>
/// <param name="Period">The period.</param>
/// <param name="Label">Short label shown on the switch.</param>
/// <param name="Months">Months one payment covers, from <see cref="BillingPolicy"/>.</param>
/// <param name="DiscountPercent">Discount, from <see cref="BillingPolicy"/>.</param>
public sealed record BillingOption(BillingPeriod Period, string Label, int Months, int DiscountPercent)
{
    /// <summary>URL and form identifier for the period.</summary>
    public string Slug => BillingPolicy.Slug(Period);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
