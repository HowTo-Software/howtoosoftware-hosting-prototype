using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The pricing presentation: discounts, coupons and rounding.
/// </summary>
/// <remarks>
/// Resource mapping is covered by <see cref="HostingPlanMappingTests"/>. These tests give the
/// catalogue configured prices, because arithmetic on an unpriced plan has nothing to assert.
/// </remarks>
public class StaticPlanCatalogServiceTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly StaticPlanCatalogService _sut;

    public StaticPlanCatalogServiceTests() =>
        _sut = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(),
            Options.Create(new HostingPlanPricingOptions
            {
                // The shipped rate card, so every tier is priced without this file holding a
                // second copy of the ladder that would go stale the moment a tier is added.
                Rates = new PlanRateCard
                {
                    CpuPer100Percent = "0.90",
                    MemoryPerGb = "1.20",
                    DiskPerBlock = "0.30",
                    DiskBlockGb = 20
                }
            }));

    public void Dispose() => _culture.Dispose();

    private HostingPlan FirstPlan => _sut.Plans[0];

    [Fact]
    public void Catalogue_IsPopulatedAndUniquelyIdentified()
    {
        Assert.NotEmpty(_sut.Plans);

        var slugs = _sut.Plans.Select(plan => plan.Slug).ToArray();
        Assert.Equal(slugs.Length, slugs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ExactlyOnePlan_IsRecommended()
    {
        Assert.Single(_sut.Plans, plan => plan.IsRecommended);
    }

    [Fact]
    public void Plans_AreOrderedByAscendingPriceAndAllocation()
    {
        var prices = _sut.Plans.Select(plan => plan.PriceMonthly).ToArray();
        var memory = _sut.Plans.Select(plan => plan.MemoryMib).ToArray();

        Assert.Equal(prices.OrderBy(price => price), prices);
        Assert.Equal(memory.OrderBy(value => value), memory);
    }

    [Fact]
    public void EveryBillingPeriod_HasAnOption()
    {
        Assert.All(Enum.GetValues<BillingPeriod>(), period =>
            Assert.NotNull(_sut.GetOption(period)));
    }

    [Fact]
    public void LongerCommitment_LowersTheEffectiveMonthlyRate()
    {
        var monthly = _sut.Quote(FirstPlan, BillingPeriod.Monthly)!;
        var quarterly = _sut.Quote(FirstPlan, BillingPeriod.Quarterly)!;
        var annual = _sut.Quote(FirstPlan, BillingPeriod.Annual)!;

        Assert.Equal(FirstPlan.PriceMonthly, monthly.FinalAmount);
        Assert.True(quarterly.EffectiveMonthly < monthly.EffectiveMonthly);
        Assert.True(annual.EffectiveMonthly < quarterly.EffectiveMonthly);
    }

    [Fact]
    public void BillingOptions_CarryThePolicyFiguresAndNothingElse()
    {
        Assert.Collection(
            _sut.BillingOptions,
            option => { Assert.Equal(BillingPeriod.Monthly, option.Period); Assert.Equal(1, option.Months); Assert.Equal(0, option.DiscountPercent); },
            option => { Assert.Equal(BillingPeriod.Quarterly, option.Period); Assert.Equal(3, option.Months); Assert.Equal(5, option.DiscountPercent); },
            option => { Assert.Equal(BillingPeriod.Annual, option.Period); Assert.Equal(12, option.Months); Assert.Equal(10, option.DiscountPercent); });
    }

    [Fact]
    public void EveryPlan_CarriesAnAudienceLineForTheReviewStep()
    {
        Assert.All(_sut.Plans, plan =>
        {
            Assert.False(string.IsNullOrWhiteSpace(plan.Audience), $"{plan.Slug} has no audience line");
            Assert.DoesNotContain("Plan.", plan.Audience, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Quotes_NeverGoNegative_AndNeverExceedTheListTotal()
    {
        Assert.All(_sut.Plans, plan =>
            Assert.All(_sut.BillingOptions, option =>
            {
                var quote = _sut.Quote(plan, option.Period)!;

                Assert.True(quote.FinalAmount > 0);
                Assert.True(quote.FinalAmount <= quote.BaseAmount);
            }));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
