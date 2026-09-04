using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The game catalogue: one product on sale, one planned, and the hierarchy between them.
/// </summary>
public class GameCatalogTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly StaticGameCatalogService _sut;
    private readonly StaticPlanCatalogService _plans;

    public GameCatalogTests()
    {
        _plans = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(),
            Options.Create(new HostingPlanPricingOptions
            {
                Rates = new PlanRateCard
                {
                    CpuPer100Percent = "0.90",
                    MemoryPerGb = "1.20",
                    DiskPerBlock = "0.30",
                    DiskBlockGb = 20
                }
            }));

        _sut = new StaticGameCatalogService(TestLocalizer.For<CheckoutText>(), _plans);
    }

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void ProjectZomboidIsThePrimaryProduct_AndListedFirst()
    {
        Assert.Equal(StaticGameCatalogService.ProjectZomboidSlug, _sut.Primary.Slug);
        Assert.Equal(_sut.Primary.Slug, _sut.Games[0].Slug);
        Assert.True(_sut.Primary.IsAvailable);
        Assert.Equal(SiteRoutes.ProjectZomboid, _sut.Primary.PageHref);
        Assert.Equal(GameTemplateCatalog.ProjectZomboidId, _sut.Primary.TemplateId);
    }

    [Fact]
    public void ThePrimaryProductSellsEveryPlanInTheLadder()
    {
        var plans = _sut.PlansFor(_sut.Primary);

        Assert.Equal(_plans.Plans.Select(plan => plan.Slug), plans.Select(plan => plan.Slug));
    }

    [Fact]
    public void TheStartingPriceIsTheCheapestPricedPlan()
    {
        Assert.Equal(_plans.Plans.Min(plan => plan.PriceMonthly), _sut.StartingPrice(_sut.Primary));
        Assert.Equal(7.99m, _sut.StartingPrice(_sut.Primary));
    }

    [Fact]
    public void APlannedGameHasNoPlans_NoPrice_AndNoPage()
    {
        var minecraft = _sut.Find(StaticGameCatalogService.MinecraftSlug);

        Assert.NotNull(minecraft);
        Assert.Equal(GameAvailability.Planned, minecraft.Availability);
        Assert.False(minecraft.IsAvailable);
        Assert.Null(minecraft.TemplateId);
        Assert.Null(minecraft.PageHref);
        Assert.Empty(_sut.PlansFor(minecraft));
        Assert.Null(_sut.StartingPrice(minecraft));
    }

    [Fact]
    public void EveryGameIsDescribed_AndSlugsAreUnique()
    {
        Assert.All(_sut.Games, game =>
        {
            Assert.False(string.IsNullOrWhiteSpace(game.Name));
            Assert.False(string.IsNullOrWhiteSpace(game.Tagline));
            Assert.False(string.IsNullOrWhiteSpace(game.Summary));
            Assert.DoesNotContain("Game.", game.Tagline, StringComparison.Ordinal);
            Assert.DoesNotContain("Game.", game.Summary, StringComparison.Ordinal);
        });

        var slugs = _sut.Games.Select(game => game.Slug).ToArray();
        Assert.Equal(slugs.Length, slugs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void OnlyOneGameIsOnSale()
    {
        Assert.Single(_sut.Games, game => game.IsAvailable);
    }

    [Fact]
    public void FindIsForgivingAboutCase_AndRefusesNonsense()
    {
        Assert.NotNull(_sut.Find("PROJECT-ZOMBOID"));
        Assert.NotNull(_sut.Find("  minecraft "));
        Assert.Null(_sut.Find("palworld"));
        Assert.Null(_sut.Find(""));
        Assert.Null(_sut.Find(null));
    }

    [Fact]
    public void RoutesAreBuiltFromTheGameSlug()
    {
        Assert.Equal("/game-hosting/project-zomboid", SiteRoutes.Game("project-zomboid"));
        Assert.Equal(
            "/game-hosting/project-zomboid/review?plan=zomboid-8gb&period=quarterly",
            SiteRoutes.Review("project-zomboid", "zomboid-8gb", BillingPeriod.Quarterly));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
