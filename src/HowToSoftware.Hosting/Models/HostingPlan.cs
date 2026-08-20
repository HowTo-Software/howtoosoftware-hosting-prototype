namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Billing cadence a visitor can preview in the plan catalogue.
/// </summary>
public enum BillingPeriod
{
    Monthly,
    Quarterly,
    Annual
}

/// <summary>
/// A selectable billing cadence with its placeholder discount.
/// </summary>
/// <param name="Period">The cadence.</param>
/// <param name="Label">Short label shown on the switch.</param>
/// <param name="Months">Number of months billed at once.</param>
/// <param name="DiscountPercent">Placeholder discount applied to the headline rate.</param>
public sealed record BillingOption(BillingPeriod Period, string Label, int Months, int DiscountPercent);

/// <summary>
/// A single labelled specification line on a plan or node panel.
/// </summary>
/// <param name="Label">Specification name, e.g. <c>MEMORY</c>.</param>
/// <param name="Value">Specification value, e.g. <c>10 GB</c>.</param>
public sealed record PlanSpec(string Label, string Value);

/// <summary>
/// A Project Zomboid hosting plan in the catalogue.
/// </summary>
/// <remarks>
/// Every figure here is placeholder test data for the prototype. The commercial structure is
/// not defined yet; see <see cref="Services.StaticPlanCatalogService"/>.
/// </remarks>
public sealed record HostingPlan
{
    /// <summary>Stable slug, used for element ids and selection state.</summary>
    public required string Id { get; init; }

    /// <summary>Plan name.</summary>
    public required string Name { get; init; }

    /// <summary>One-line positioning for the plan.</summary>
    public required string Tagline { get; init; }

    /// <summary>Maximum configured player slots.</summary>
    public required int PlayerSlots { get; init; }

    /// <summary>Placeholder headline rate per month, before any period discount.</summary>
    public required decimal MonthlyPrice { get; init; }

    /// <summary>Hardware allocation lines rendered in the plan panel.</summary>
    public required IReadOnlyList<PlanSpec> Specs { get; init; }

    /// <summary>What the plan includes, rendered as a terse technical list.</summary>
    public required IReadOnlyList<string> Includes { get; init; }

    /// <summary>Marks the plan the catalogue highlights by default.</summary>
    public bool IsRecommended { get; init; }
}

/// <summary>
/// Outcome of checking a promotional code.
/// </summary>
/// <param name="Status">Whether the code was recognised.</param>
/// <param name="Code">The normalised code that was checked.</param>
/// <param name="PercentOff">Discount to preview, 0 when not applied.</param>
/// <param name="Message">Human-readable result shown under the field.</param>
public sealed record CouponResult(CouponStatus Status, string Code, int PercentOff, string Message)
{
    /// <summary>Nothing has been entered yet.</summary>
    public static readonly CouponResult None = new(CouponStatus.None, string.Empty, 0, string.Empty);

    /// <summary>Whether a discount should be applied to displayed totals.</summary>
    public bool IsApplied => Status is CouponStatus.Applied;
}

/// <summary>
/// State of the promotional code field.
/// </summary>
public enum CouponStatus
{
    None,
    Applied,
    Rejected
}
