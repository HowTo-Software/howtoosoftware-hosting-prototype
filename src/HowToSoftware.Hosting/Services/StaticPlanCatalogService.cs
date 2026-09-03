using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The Project Zomboid plan catalogue.
/// </summary>
/// <remarks>
/// <para>
/// <b>The resource figures in this file are the product.</b> They are what the site advertises
/// and, byte for byte, what <c>ProvisioningService</c> sends to Pterodactyl - there is no second
/// copy of them anywhere. Changing a number here changes both at once, which is the point.
/// </para>
/// <para>
/// CPU is expressed as Pterodactyl's percentage limit: 100 is the equivalent of one logical
/// thread. It is a ceiling on a shared pool, not a set of pinned cores, and the catalogue says
/// "300% CPU allocation" rather than "3 dedicated cores" because the latter would not be true.
/// </para>
/// <para>
/// Prices are not here either, and they are not a list of agreed numbers anywhere. Each one
/// is the configured rate card in <see cref="HostingPlanPricingOptions"/> applied to the
/// figures above, so a tier cannot be repriced without repricing what it is made of. A plan
/// with no rate card and no override renders as visibly unpriced.
/// </para>
/// </remarks>
public sealed class StaticPlanCatalogService : IPlanCatalogService
{
    private static readonly Dictionary<string, int> DemoCoupons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SURVIVOR10"] = 10,
        ["KNOX25"] = 25,
        ["HTS2026"] = 15
    };

    /// <summary>MiB in a GiB. Pterodactyl's memory and disk limits are both in MiB.</summary>
    internal const int MibPerGib = 1024;

    /// <summary>Storage on the tiers up to and including 6 GiB of memory, in GiB.</summary>
    internal const int StandardDiskGb = 25;

    /// <summary>Storage from the 8 GiB tier upwards, in GiB.</summary>
    /// <remarks>
    /// The step exists because the tiers above it are bought for large mod lists and long-lived
    /// worlds, which grow on disk rather than in memory. It is one step, not a per-tier ladder:
    /// tiering storage finely would only complicate moving between plans later.
    /// </remarks>
    internal const int ExpandedDiskGb = 40;

    /// <summary>
    /// Ports a Project Zomboid server needs to hold. The game binds its UDP game port and a
    /// second direct-connect port, so this is a technical floor rather than a commercial tier.
    /// </summary>
    private const int ZomboidAllocations = 2;

    /// <summary>
    /// Project Zomboid keeps its world on disk and uses no relational database, so no plan grants
    /// one. Raising this would hand out a MySQL instance nothing ever connects to.
    /// </summary>
    private const int ZomboidDatabases = 0;

    private readonly IStringLocalizer<HomeText> _text;
    private readonly HostingPlanPricingOptions _pricing;

    /// <summary>Creates the catalogue.</summary>
    /// <param name="text">Plan copy for the culture chosen for this request.</param>
    /// <param name="pricing">Configured prices and currency.</param>
    public StaticPlanCatalogService(
        IStringLocalizer<HomeText> text,
        IOptions<HostingPlanPricingOptions> pricing)
    {
        _text = text;
        _pricing = pricing.Value;
    }

    /// <inheritdoc />
    public string CurrencySymbol => _pricing.CurrencySymbol;

    /// <inheritdoc />
    public IReadOnlyList<BillingOption> BillingOptions =>
    [
        new(BillingPeriod.Monthly, _text["Billing.Monthly"], 1, 0),
        new(BillingPeriod.Quarterly, _text["Billing.Quarterly"], 3, 10),
        new(BillingPeriod.Annual, _text["Billing.Annual"], 12, 20)
    ];

    /// <inheritdoc />
    public IReadOnlyList<HostingPlan> Plans =>
    [
        Build(
            slug: "zomboid-4gb",
            name: "Outpost",
            tagline: _text["Plan.Outpost.Tagline"],
            memoryGb: 4,
            cpuPercent: 300,
            diskGb: StandardDiskGb,
            backups: 1,
            includes:
            [
                _text["Include.WorkshopSync"],
                _text["Include.DailyBackups"],
                _text["Include.FullConsole"]
            ]),

        Build(
            slug: "zomboid-5gb",
            name: "Settlement",
            tagline: _text["Plan.Settlement.Tagline"],
            memoryGb: 5,
            cpuPercent: 400,
            diskGb: StandardDiskGb,
            backups: 2,
            isRecommended: true,
            includes:
            [
                _text["Include.WorkshopSync"],
                _text["Include.TwiceDailyBackups"],
                _text["Include.FullConsole"],
                _text["Include.ScheduledRestarts"]
            ]),

        Build(
            slug: "zomboid-6gb",
            name: "Stronghold",
            tagline: _text["Plan.Stronghold.Tagline"],
            memoryGb: 6,
            cpuPercent: 400,
            diskGb: StandardDiskGb,
            backups: 3,
            includes:
            [
                _text["Include.WorkshopSync"],
                _text["Include.HourlyBackups"],
                _text["Include.FullConsole"],
                _text["Include.ScheduledRestarts"],
                _text["Include.PriorityPlacement"]
            ]),

        Build(
            slug: "zomboid-8gb",
            name: "Knox Cell",
            tagline: _text["Plan.KnoxCell.Tagline"],
            memoryGb: 8,
            cpuPercent: 500,
            diskGb: ExpandedDiskGb,
            backups: 5,
            includes: LargeTierIncludes),

        Build(
            slug: "zomboid-10gb",
            name: "Rosewood",
            tagline: _text["Plan.Rosewood.Tagline"],
            memoryGb: 10,
            cpuPercent: 500,
            diskGb: ExpandedDiskGb,
            backups: 6,
            includes: LargeTierIncludes),

        Build(
            slug: "zomboid-12gb",
            name: "West Point",
            tagline: _text["Plan.WestPoint.Tagline"],
            memoryGb: 12,
            cpuPercent: 600,
            diskGb: ExpandedDiskGb,
            backups: 7,
            includes: LargeTierIncludes),

        Build(
            slug: "zomboid-14gb",
            name: "Louisville",
            tagline: _text["Plan.Louisville.Tagline"],
            memoryGb: 14,
            cpuPercent: 600,
            diskGb: ExpandedDiskGb,
            backups: 8,
            includes: LargeTierIncludes),

        Build(
            slug: "zomboid-16gb",
            name: "Knox County",
            tagline: _text["Plan.KnoxCounty.Tagline"],
            memoryGb: 16,
            cpuPercent: 700,
            diskGb: ExpandedDiskGb,
            backups: 10,
            includes: LargeTierIncludes)
    ];

    /// <summary>
    /// The feature list every tier from 8 GiB up carries. Shared rather than repeated, so a
    /// change to what the large tiers include cannot land on four cards and miss the fifth.
    /// </summary>
    private IReadOnlyList<string> LargeTierIncludes =>
    [
        _text["Include.WorkshopSync"],
        _text["Include.HourlyBackups"],
        _text["Include.FullConsole"],
        _text["Include.ScheduledRestarts"],
        _text["Include.PriorityPlacement"],
        _text["Include.DedicatedAllocation"]
    ];

    /// <inheritdoc />
    public bool HasPricing => Plans.Any(plan => plan.IsPriced);

    /// <inheritdoc />
    public decimal RenewalDiscountPercent => _pricing.RenewalDiscountPercent;

    /// <inheritdoc />
    public decimal? GetRenewalRate(decimal firstMonth) => _pricing.GetRenewalRate(firstMonth);

    /// <inheritdoc />
    public HostingPlan? FindBySlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : Plans.FirstOrDefault(plan =>
                string.Equals(plan.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public BillingOption GetOption(BillingPeriod period) =>
        BillingOptions.First(option => option.Period == period);

    /// <inheritdoc />
    public decimal? GetMonthlyRate(HostingPlan plan, BillingPeriod period, CouponResult coupon)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.PriceMonthly is not { } price)
        {
            return null;
        }

        var discount = GetOption(period).DiscountPercent + (coupon.IsApplied ? coupon.PercentOff : 0);
        discount = Math.Min(discount, 90);

        return Math.Round(price * (100 - discount) / 100m, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    public decimal? GetCycleTotal(HostingPlan plan, BillingPeriod period, CouponResult coupon) =>
        GetMonthlyRate(plan, period, coupon) is { } rate
            ? Math.Round(rate * GetOption(period).Months, 2, MidpointRounding.AwayFromZero)
            : null;

    /// <inheritdoc />
    public CouponResult CheckCoupon(string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return CouponResult.None;
        }

        var normalised = trimmed.ToUpperInvariant();

        return DemoCoupons.TryGetValue(normalised, out var percent)
            ? new CouponResult(CouponStatus.Applied, normalised, percent, _text["Coupon.Applied", percent])
            : new CouponResult(CouponStatus.Rejected, normalised, 0, _text["Coupon.Rejected"]);
    }

    /// <summary>
    /// Builds a plan from the figures that differ between tiers, converting the human-facing GiB
    /// once - here - so no caller has to remember which unit Pterodactyl wanted.
    /// </summary>
    /// <param name="slug">Stable slug, also the configuration key for the plan's price.</param>
    /// <param name="name">Plan name.</param>
    /// <param name="tagline">One-line positioning.</param>
    /// <param name="memoryGb">Memory in GiB.</param>
    /// <param name="cpuPercent">Pterodactyl CPU limit as a percentage.</param>
    /// <param name="diskGb">Storage in GiB.</param>
    /// <param name="backups">Backups the customer may keep.</param>
    /// <param name="includes">Feature list shown on the card.</param>
    /// <param name="isRecommended">Marks the highlighted plan.</param>
    private HostingPlan Build(
        string slug,
        string name,
        string tagline,
        int memoryGb,
        int cpuPercent,
        int diskGb,
        int backups,
        IReadOnlyList<string> includes,
        bool isRecommended = false) => new()
        {
            Slug = slug,
            Name = name,
            Tagline = tagline,
            MemoryMib = memoryGb * MibPerGib,
            CpuPercent = cpuPercent,
            DiskMib = diskGb * MibPerGib,
            BackupLimit = backups,
            DatabaseLimit = ZomboidDatabases,
            AllocationLimit = ZomboidAllocations,
            GameTemplateId = GameTemplateCatalog.ProjectZomboidId,
            // Derived from the configured rate card, unless this slug carries an override.
            // Passing the tier's own resources rather than a stored number is what keeps the
            // price and the product from drifting apart.
            PriceMonthly = _pricing.Resolve(slug, cpuPercent, memoryGb, diskGb),
            // Project Zomboid's player cap is a server-configuration value, not a container
            // limit, so it is not inferred from memory. Unset until the figures are agreed.
            PlayerSlots = null,
            Includes = includes,
            IsRecommended = isRecommended
        };
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
