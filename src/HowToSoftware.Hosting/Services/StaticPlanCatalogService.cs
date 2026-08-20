using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Prototype implementation of <see cref="IPlanCatalogService"/> backed by compiled test data.
/// </summary>
/// <remarks>
/// <para>
/// <b>None of these figures are commercial.</b> They exist so the catalogue, the billing-period
/// switch and the promotional-code mechanic can be reviewed as an interface. Replace this
/// registration in <c>Program.cs</c> once real pricing and a billing provider are decided.
/// </para>
/// <para>
/// Coupon checking is intentionally local: the codes below are demo values, the check never
/// leaves the process, and applying one changes displayed figures only.
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

    /// <inheritdoc />
    public string CurrencySymbol => "€";

    /// <inheritdoc />
    public IReadOnlyList<BillingOption> BillingOptions { get; } =
    [
        new(BillingPeriod.Monthly, "Monthly", 1, 0),
        new(BillingPeriod.Quarterly, "Quarterly", 3, 10),
        new(BillingPeriod.Annual, "Annual", 12, 20)
    ];

    /// <inheritdoc />
    public IReadOnlyList<HostingPlan> Plans { get; } =
    [
        new()
        {
            Id = "outpost",
            Name = "Outpost",
            Tagline = "A small group riding out the first month.",
            PlayerSlots = 8,
            MonthlyPrice = 6.99m,
            Specs =
            [
                new("MEMORY", "6 GB"),
                new("VCPU", "2 cores"),
                new("STORAGE", "30 GB NVMe")
            ],
            Includes = ["Workshop mod sync", "Daily backups", "Full console access"]
        },
        new()
        {
            Id = "settlement",
            Name = "Settlement",
            Tagline = "The size most modded communities actually land on.",
            PlayerSlots = 16,
            MonthlyPrice = 11.99m,
            IsRecommended = true,
            Specs =
            [
                new("MEMORY", "10 GB"),
                new("VCPU", "4 cores"),
                new("STORAGE", "60 GB NVMe")
            ],
            Includes = ["Workshop mod sync", "Twice-daily backups", "Full console access", "Scheduled restarts"]
        },
        new()
        {
            Id = "stronghold",
            Name = "Stronghold",
            Tagline = "Heavy mod lists and a population that keeps growing.",
            PlayerSlots = 32,
            MonthlyPrice = 19.99m,
            Specs =
            [
                new("MEMORY", "16 GB"),
                new("VCPU", "6 cores"),
                new("STORAGE", "120 GB NVMe")
            ],
            Includes = ["Workshop mod sync", "Hourly backups", "Full console access", "Scheduled restarts", "Priority placement"]
        },
        new()
        {
            Id = "knox-cell",
            Name = "Knox Cell",
            Tagline = "A whole community, on dedicated allocation.",
            PlayerSlots = 64,
            MonthlyPrice = 34.99m,
            Specs =
            [
                new("MEMORY", "24 GB"),
                new("VCPU", "8 cores"),
                new("STORAGE", "200 GB NVMe")
            ],
            Includes = ["Workshop mod sync", "Hourly backups", "Full console access", "Scheduled restarts", "Priority placement", "Dedicated allocation"]
        }
    ];

    /// <inheritdoc />
    public BillingOption GetOption(BillingPeriod period) =>
        BillingOptions.First(option => option.Period == period);

    /// <inheritdoc />
    public decimal GetMonthlyRate(HostingPlan plan, BillingPeriod period, CouponResult coupon)
    {
        var discount = GetOption(period).DiscountPercent + (coupon.IsApplied ? coupon.PercentOff : 0);
        discount = Math.Min(discount, 90);

        var rate = plan.MonthlyPrice * (100 - discount) / 100m;
        return Math.Round(rate, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    public decimal GetCycleTotal(HostingPlan plan, BillingPeriod period, CouponResult coupon) =>
        Math.Round(GetMonthlyRate(plan, period, coupon) * GetOption(period).Months, 2, MidpointRounding.AwayFromZero);

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
            ? new CouponResult(CouponStatus.Applied, normalised, percent, $"Demo code applied - {percent}% off the preview.")
            : new CouponResult(CouponStatus.Rejected, normalised, 0, "Not a recognised demo code.");
    }
}
