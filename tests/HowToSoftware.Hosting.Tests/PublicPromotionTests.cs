using System.Data.Common;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Commerce;
using HowToSoftware.Hosting.Services;
using HowToSoftware.Hosting.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

public sealed class PublicPromotionTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly OrderPricingService _pricing;

    public PublicPromotionTests()
    {
        var plans = new StaticPlanCatalogService(TestLocalizer.For<HomeText>(), Options.Create(
            new HostingPlanPricingOptions
            {
                Rates = new PlanRateCard
                {
                    CpuPer100Percent = "0.90", MemoryPerGb = "1.20", DiskPerBlock = "0.30", DiskBlockGb = 20
                }
            }));
        _pricing = new OrderPricingService(new StaticGameCatalogService(TestLocalizer.For<CheckoutText>(), plans), plans);
    }

    public void Dispose() => _culture.Dispose();

    private static PublicPromotionCampaign Campaign(string code = "TEST_PUBLIC", string plan = "zomboid-4gb", string period = "monthly") =>
        new() { Code = code, GameSlug = "project-zomboid", PlanSlug = plan, Period = period };

    private PublicPromotionService Service(IPromotionService promotions, params PublicPromotionCampaign[] campaigns) =>
        new(Options.Create(new PublicPromotionOptions { Campaigns = campaigns.ToList() }), _pricing, promotions,
            NullLogger<PublicPromotionService>.Instance);

    [Fact]
    public async Task EmptyPublicAllowlistNeverTouchesThePromotionStore()
    {
        var promotions = new RecordingPromotions();
        Assert.Empty(await Service(promotions).GetOffersAsync());
        Assert.Empty(promotions.Lookups);
    }

    [Fact]
    public async Task OnlyExplicitlyOptedInCodesAreEverLookedUp()
    {
        var promotions = new RecordingPromotions();
        var offers = await Service(promotions, Campaign()).GetOffersAsync();
        Assert.Single(offers);
        Assert.Equal("TEST_PUBLIC", Assert.Single(promotions.Lookups).Code);
        Assert.False(promotions.RedeemCalled);
    }

    [Fact]
    public async Task MissingCommerceDatabaseCannotAdvertiseAnUnvalidatedCoupon()
    {
        Assert.Empty(await Service(new NullPromotionService(), Campaign()).GetOffersAsync());
    }

    [Fact]
    public async Task IneligibleCodesAreOmittedRatherThanAdvertisedWithGuessedDiscounts()
    {
        var promotions = new RecordingPromotions { Eligible = false };
        Assert.Empty(await Service(promotions, Campaign()).GetOffersAsync());
        Assert.Single(promotions.Lookups);
    }

    [Theory]
    [InlineData("zomboid-99gb", "monthly")]
    [InlineData("zomboid-4gb", "forever")]
    public async Task CampaignsWithoutARealPricedPlanAndPeriodAreNotValidated(string plan, string period)
    {
        var promotions = new RecordingPromotions();
        Assert.Empty(await Service(promotions, Campaign(plan: plan, period: period)).GetOffersAsync());
        Assert.Empty(promotions.Lookups);
    }

    [Fact]
    public async Task EligibilityUsesTheCurrentServerQuoteIncludingPeriodDiscounts()
    {
        var promotions = new RecordingPromotions();
        var offer = Assert.Single(await Service(promotions, Campaign(period: "quarterly")).GetOffersAsync());
        var lookup = Assert.Single(promotions.Lookups);
        Assert.Equal("project-zomboid", lookup.Game);
        Assert.Equal("zomboid-4gb", lookup.Plan);
        Assert.Equal(22.77m, lookup.Quote.FinalAmount);
        Assert.Equal(5, lookup.Quote.DiscountPercent);
        Assert.Equal(1m, offer.DiscountAmount);
        Assert.Equal(21.77m, offer.FinalAmount);
        Assert.Contains("period=quarterly", offer.ReviewHref);
        Assert.EndsWith("&promo=TEST_PUBLIC", offer.ReviewHref);
    }

    [Fact]
    public async Task IdenticalCampaignsAreDeduplicatedAfterCodeNormalization()
    {
        var promotions = new RecordingPromotions();
        Assert.Single(await Service(promotions, Campaign(" test_public "), Campaign("TEST_PUBLIC")).GetOffersAsync());
        Assert.Single(promotions.Lookups);
    }

    [Fact]
    public async Task TheAnnouncementQueryBudgetIsBoundedEvenWithoutAnOptionsValidator()
    {
        var promotions = new RecordingPromotions();
        await Service(promotions, Campaign("TEST_1"), Campaign("TEST_2"), Campaign("TEST_3"), Campaign("TEST_4")).GetOffersAsync();
        Assert.Equal(PublicPromotionOptions.MaximumCampaigns, promotions.Lookups.Count);
    }

    [Fact]
    public async Task OptionalAnnouncementsDoNotBreakTheStorefrontWhenTheDatabaseIsUnavailable()
    {
        var promotions = new RecordingPromotions { Failure = new UnavailableDatabaseException() };
        Assert.Empty(await Service(promotions, Campaign()).GetOffersAsync());
    }

    [Fact]
    public async Task CallerCancellationIsPreserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service(new RecordingPromotions(), Campaign()).GetOffersAsync(cancellation.Token));
    }

    [Fact]
    public void EmptyAllowlistIsValidButMoreThanThreePublicCampaignsAreRejected()
    {
        var validator = new PublicPromotionOptionsValidator();
        Assert.True(validator.Validate(null, new PublicPromotionOptions()).Succeeded);
        Assert.True(validator.Validate(null, new PublicPromotionOptions
        {
            Campaigns = [Campaign("TEST_1"), Campaign("TEST_2"), Campaign("TEST_3"), Campaign("TEST_4")]
        }).Failed);
    }

    [Theory]
    [InlineData("", "monthly")]
    [InlineData("CODE<script>", "monthly")]
    [InlineData("CODE", "forever")]
    public void UnsafeOrAmbiguousCampaignConfigurationIsRejected(string code, string period)
    {
        Assert.True(new PublicPromotionOptionsValidator().Validate(null, new PublicPromotionOptions
        {
            Campaigns = [Campaign(code, period: period)]
        }).Failed);
    }

    private sealed class RecordingPromotions : IPromotionService
    {
        public List<(string Code, string Game, string Plan, BillingQuote Quote)> Lookups { get; } = [];
        public bool Eligible { get; init; } = true;
        public bool RedeemCalled { get; private set; }
        public Exception? Failure { get; init; }

        public Task<PromotionResult?> ValidateAsync(string? code, string gameSlug, string planSlug, BillingQuote quote,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is { } failure) throw failure;
            Lookups.Add((code!, gameSlug, planSlug, quote));
            return Task.FromResult(Eligible
                ? new PromotionResult(new PromotionCode
                {
                    Id = Guid.NewGuid(), Code = code!, DiscountType = "fixed", FixedAmountCents = 100
                }, 1m, quote.FinalAmount - 1m)
                : null);
        }

        public Task RedeemAsync(Guid promotionId, Guid orderId, long discountAmountCents, DateTimeOffset redeemedAt,
            CancellationToken cancellationToken = default)
        {
            RedeemCalled = true;
            throw new InvalidOperationException("An announcement must never redeem a promotion.");
        }
    }

    private sealed class UnavailableDatabaseException : DbException { }
}
