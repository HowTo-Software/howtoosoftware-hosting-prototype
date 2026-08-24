using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Localization;

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

    private readonly IStringLocalizer<HomeText> _text;

    /// <summary>Creates the catalogue.</summary>
    /// <param name="text">
    /// Plan copy - taglines, what each plan includes, the billing cadence labels - for the
    /// culture chosen for this request. Prices, slot counts and coupon codes are not copy and
    /// stay in this file.
    /// </param>
    public StaticPlanCatalogService(IStringLocalizer<HomeText> text) => _text = text;

    /// <inheritdoc />
    public string CurrencySymbol => "€";

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
        new()
        {
            Id = "outpost",
            Name = "Outpost",
            Tagline = _text["Plan.Outpost.Tagline"],
            PlayerSlots = 8,
            MonthlyPrice = 6.99m,
            Specs =
            [
                new(_text["Spec.Memory"], "6 GB"),
                new(_text["Spec.Vcpu"], _text["Plan.Cores", 2]),
                new(_text["Spec.Storage"], "30 GB NVMe")
            ],
            Includes =
            [
                _text["Include.WorkshopSync"],
                _text["Include.DailyBackups"],
                _text["Include.FullConsole"]
            ]
        },
        new()
        {
            Id = "settlement",
            Name = "Settlement",
            Tagline = _text["Plan.Settlement.Tagline"],
            PlayerSlots = 16,
            MonthlyPrice = 11.99m,
            IsRecommended = true,
            Specs =
            [
                new(_text["Spec.Memory"], "10 GB"),
                new(_text["Spec.Vcpu"], _text["Plan.Cores", 4]),
                new(_text["Spec.Storage"], "60 GB NVMe")
            ],
            Includes =
            [
                _text["Include.WorkshopSync"],
                _text["Include.TwiceDailyBackups"],
                _text["Include.FullConsole"],
                _text["Include.ScheduledRestarts"]
            ]
        },
        new()
        {
            Id = "stronghold",
            Name = "Stronghold",
            Tagline = _text["Plan.Stronghold.Tagline"],
            PlayerSlots = 32,
            MonthlyPrice = 19.99m,
            Specs =
            [
                new(_text["Spec.Memory"], "16 GB"),
                new(_text["Spec.Vcpu"], _text["Plan.Cores", 6]),
                new(_text["Spec.Storage"], "120 GB NVMe")
            ],
            Includes =
            [
                _text["Include.WorkshopSync"],
                _text["Include.HourlyBackups"],
                _text["Include.FullConsole"],
                _text["Include.ScheduledRestarts"],
                _text["Include.PriorityPlacement"]
            ]
        },
        new()
        {
            Id = "knox-cell",
            Name = "Knox Cell",
            Tagline = _text["Plan.KnoxCell.Tagline"],
            PlayerSlots = 64,
            MonthlyPrice = 34.99m,
            Specs =
            [
                new(_text["Spec.Memory"], "24 GB"),
                new(_text["Spec.Vcpu"], _text["Plan.Cores", 8]),
                new(_text["Spec.Storage"], "200 GB NVMe")
            ],
            Includes =
            [
                _text["Include.WorkshopSync"],
                _text["Include.HourlyBackups"],
                _text["Include.FullConsole"],
                _text["Include.ScheduledRestarts"],
                _text["Include.PriorityPlacement"],
                _text["Include.DedicatedAllocation"]
            ]
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
            ? new CouponResult(CouponStatus.Applied, normalised, percent, _text["Coupon.Applied", percent])
            : new CouponResult(CouponStatus.Rejected, normalised, 0, _text["Coupon.Rejected"]);
    }
}
