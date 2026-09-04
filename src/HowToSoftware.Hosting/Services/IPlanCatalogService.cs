using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Supplies the Project Zomboid plan catalogue: the resources each plan is provisioned with, and
/// the prices around them.
/// </summary>
/// <remarks>
/// <para>
/// This is the single source of truth for plan resources. Components read from it and never
/// restate a memory or CPU figure of their own, and <c>ProvisioningService</c> reads the same
/// records when it creates a server - so what the catalogue advertises and what Pterodactyl is
/// asked for cannot drift apart.
/// </para>
/// <para>
/// Prices come from configuration and may be absent. Billing-period discounts come from
/// <see cref="BillingPolicy"/> and nowhere else; there is no coupon and no renewal discount.
/// </para>
/// </remarks>
public interface IPlanCatalogService
{
    /// <summary>Currency symbol used for display.</summary>
    string CurrencySymbol { get; }

    /// <summary>ISO 4217 currency code, lower case, as the payment provider wants it.</summary>
    string CurrencyCode { get; }

    /// <summary>The plans on offer, smallest allocation first.</summary>
    IReadOnlyList<HostingPlan> Plans { get; }

    /// <summary>The billing periods a customer can choose, with localised labels.</summary>
    IReadOnlyList<BillingOption> BillingOptions { get; }

    /// <summary>Whether any plan has a configured price.</summary>
    /// <remarks>
    /// The billing switch is meaningless against a catalogue with no prices in it, so the plan
    /// picker hides it rather than offering a control that cannot change anything.
    /// </remarks>
    bool HasPricing { get; }

    /// <summary>Finds a plan by its slug.</summary>
    /// <param name="slug">Plan slug, e.g. <c>zomboid-4gb</c>.</param>
    /// <returns>The plan, or <see langword="null"/> when no plan carries that slug.</returns>
    HostingPlan? FindBySlug(string? slug);

    /// <summary>Looks up a billing option by period.</summary>
    BillingOption GetOption(BillingPeriod period);

    /// <summary>
    /// What one billing period of a plan costs, or <see langword="null"/> when the plan has no
    /// configured price.
    /// </summary>
    BillingQuote? Quote(HostingPlan plan, BillingPeriod period);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
