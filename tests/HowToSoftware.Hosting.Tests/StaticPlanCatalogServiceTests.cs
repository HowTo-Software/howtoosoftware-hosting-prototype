using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

namespace HowToSoftware.Hosting.Tests;

public class StaticPlanCatalogServiceTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly StaticPlanCatalogService _sut = new(TestLocalizer.For<HomeText>());

    public void Dispose() => _culture.Dispose();

    private HostingPlan FirstPlan => _sut.Plans[0];

    [Fact]
    public void Catalogue_IsPopulatedAndUniquelyIdentified()
    {
        Assert.NotEmpty(_sut.Plans);

        var ids = _sut.Plans.Select(plan => plan.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ExactlyOnePlan_IsRecommended()
    {
        Assert.Single(_sut.Plans, plan => plan.IsRecommended);
    }

    [Fact]
    public void Plans_AreOrderedByAscendingPriceAndSlots()
    {
        var prices = _sut.Plans.Select(plan => plan.MonthlyPrice).ToArray();
        var slots = _sut.Plans.Select(plan => plan.PlayerSlots).ToArray();

        Assert.Equal(prices.OrderBy(price => price), prices);
        Assert.Equal(slots.OrderBy(slot => slot), slots);
    }

    [Fact]
    public void EveryBillingPeriod_HasAnOption()
    {
        Assert.All(Enum.GetValues<BillingPeriod>(), period =>
            Assert.NotNull(_sut.GetOption(period)));
    }

    [Fact]
    public void LongerCommitment_LowersTheMonthlyRate()
    {
        var monthly = _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Monthly, CouponResult.None);
        var quarterly = _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Quarterly, CouponResult.None);
        var annual = _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Annual, CouponResult.None);

        Assert.Equal(FirstPlan.MonthlyPrice, monthly);
        Assert.True(quarterly < monthly, $"Quarterly {quarterly} should be below monthly {monthly}.");
        Assert.True(annual < quarterly, $"Annual {annual} should be below quarterly {quarterly}.");
    }

    [Fact]
    public void CycleTotal_IsTheMonthlyRateTimesTheNumberOfMonths()
    {
        foreach (var option in _sut.BillingOptions)
        {
            var monthly = _sut.GetMonthlyRate(FirstPlan, option.Period, CouponResult.None);
            var total = _sut.GetCycleTotal(FirstPlan, option.Period, CouponResult.None);

            Assert.Equal(monthly * option.Months, total);
        }
    }

    [Theory]
    [InlineData("SURVIVOR10", 10)]
    [InlineData("survivor10", 10)]
    [InlineData("  KNOX25  ", 25)]
    [InlineData("HTS2026", 15)]
    public void KnownDemoCodes_AreAccepted(string input, int expectedPercent)
    {
        var result = _sut.CheckCoupon(input);

        Assert.Equal(CouponStatus.Applied, result.Status);
        Assert.True(result.IsApplied);
        Assert.Equal(expectedPercent, result.PercentOff);
        Assert.Equal(input.Trim().ToUpperInvariant(), result.Code);
    }

    [Theory]
    [InlineData("NOPE")]
    [InlineData("SURVIVOR")]
    [InlineData("12345")]
    public void UnknownCodes_AreRejectedWithoutDiscount(string input)
    {
        var result = _sut.CheckCoupon(input);

        Assert.Equal(CouponStatus.Rejected, result.Status);
        Assert.False(result.IsApplied);
        Assert.Equal(0, result.PercentOff);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInput_LeavesTheFieldUndecided(string? input)
    {
        var result = _sut.CheckCoupon(input);

        Assert.Equal(CouponStatus.None, result.Status);
        Assert.False(result.IsApplied);
        Assert.Empty(result.Message);
    }

    [Fact]
    public void AppliedCoupon_StacksOnTopOfThePeriodDiscount()
    {
        var coupon = _sut.CheckCoupon("KNOX25");

        var withoutCoupon = _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Annual, CouponResult.None);
        var withCoupon = _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Annual, coupon);

        // Annual is 20% off, the code adds 25%, so the rate lands at 55% of the headline.
        Assert.Equal(Math.Round(FirstPlan.MonthlyPrice * 0.55m, 2, MidpointRounding.AwayFromZero), withCoupon);
        Assert.True(withCoupon < withoutCoupon);
    }

    [Fact]
    public void RejectedCoupon_DoesNotChangeThePrice()
    {
        var rejected = _sut.CheckCoupon("NOPE");

        Assert.Equal(
            _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Monthly, CouponResult.None),
            _sut.GetMonthlyRate(FirstPlan, BillingPeriod.Monthly, rejected));
    }

    [Fact]
    public void Pricing_NeverGoesNegative()
    {
        var extreme = new CouponResult(CouponStatus.Applied, "EXTREME", 500, "test");

        Assert.All(_sut.Plans, plan =>
            Assert.True(_sut.GetMonthlyRate(plan, BillingPeriod.Annual, extreme) > 0));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
