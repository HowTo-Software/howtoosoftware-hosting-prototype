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
    public string CurrencyCode => _pricing.CurrencyCode;

    /// <inheritdoc />
    /// <remarks>
    /// The figures come from <see cref="BillingPolicy"/>; only the labels are this class's.
    /// A percentage written here would be a second copy of the policy.
    /// </remarks>
    public IReadOnlyList<BillingOption> BillingOptions =>
        BillingPolicy.Periods
            .Select(period => new BillingOption(
                period,
                _text[$"Billing.{period}"],
                BillingPolicy.Months(period),
                BillingPolicy.DiscountPercent(period)))
            .ToArray();

    /// <inheritdoc />
    public IReadOnlyList<HostingPlan> Plans =>
    [
        Build("zomboid-4gb", "Outpost", "Outpost", memoryGb: 4, cpuPercent: 300, diskGb: StandardDiskGb, backups: 1),
        Build("zomboid-5gb", "Settlement", "Settlement", memoryGb: 5, cpuPercent: 400, diskGb: StandardDiskGb, backups: 2, isRecommended: true),
        Build("zomboid-6gb", "Stronghold", "Stronghold", memoryGb: 6, cpuPercent: 400, diskGb: StandardDiskGb, backups: 3),
        Build("zomboid-8gb", "Knox Cell", "KnoxCell", memoryGb: 8, cpuPercent: 500, diskGb: ExpandedDiskGb, backups: 5),
        Build("zomboid-10gb", "Rosewood", "Rosewood", memoryGb: 10, cpuPercent: 500, diskGb: ExpandedDiskGb, backups: 6),
        Build("zomboid-12gb", "West Point", "WestPoint", memoryGb: 12, cpuPercent: 600, diskGb: ExpandedDiskGb, backups: 7),
        Build("zomboid-14gb", "Louisville", "Louisville", memoryGb: 14, cpuPercent: 600, diskGb: ExpandedDiskGb, backups: 8),
        Build("zomboid-16gb", "Knox County", "KnoxCounty", memoryGb: 16, cpuPercent: 700, diskGb: ExpandedDiskGb, backups: 10)
    ];

    /// <inheritdoc />
    public bool HasPricing => Plans.Any(plan => plan.IsPriced);

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
    public BillingQuote? Quote(HostingPlan plan, BillingPeriod period)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return plan.PriceMonthly is { } price and > 0m
            ? BillingPolicy.Quote(price, period)
            : null;
    }

    /// <summary>
    /// Builds a plan from the figures that differ between tiers, converting the human-facing GiB
    /// once - here - so no caller has to remember which unit Pterodactyl wanted.
    /// </summary>
    /// <param name="slug">Stable slug, also the configuration key for the plan's price.</param>
    /// <param name="name">Plan name.</param>
    /// <param name="copyKey">Resource key stem for the plan's tagline and audience line.</param>
    /// <param name="memoryGb">Memory in GiB.</param>
    /// <param name="cpuPercent">Pterodactyl CPU limit as a percentage.</param>
    /// <param name="diskGb">Storage in GiB.</param>
    /// <param name="backups">Backups the customer may keep.</param>
    /// <param name="isRecommended">Marks the highlighted plan.</param>
    private HostingPlan Build(
        string slug,
        string name,
        string copyKey,
        int memoryGb,
        int cpuPercent,
        int diskGb,
        int backups,
        bool isRecommended = false) => new()
        {
            Slug = slug,
            Name = name,
            Tagline = _text[$"Plan.{copyKey}.Tagline"],
            Audience = _text[$"Plan.{copyKey}.Audience"],
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
            IsRecommended = isRecommended
        };
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
