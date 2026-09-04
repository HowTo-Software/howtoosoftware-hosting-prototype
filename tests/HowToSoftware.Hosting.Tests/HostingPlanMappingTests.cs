using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The plan catalogue is the single source of truth for what a customer is sold and what
/// Pterodactyl is asked to create. These tests pin the numbers.
/// </summary>
public class HostingPlanMappingTests : IDisposable
{
    /// <summary>The rate card the site actually ships, mirrored from <c>appsettings.json</c>.</summary>
    private static PlanRateCard ShippedRates => new()
    {
        CpuPer100Percent = "0.90",
        MemoryPerGb = "1.20",
        DiskPerBlock = "0.30",
        DiskBlockGb = 20
    };

    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly StaticPlanCatalogService _sut = Build();

    public void Dispose() => _culture.Dispose();

    private static StaticPlanCatalogService Build(
        Dictionary<string, string>? prices = null,
        PlanRateCard? rates = null) =>
        new(TestLocalizer.For<HomeText>(),
            Options.Create(new HostingPlanPricingOptions
            {
                Rates = rates ?? new PlanRateCard(),
                Prices = prices ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            }));

    /// <summary>
    /// The agreed allocations, and the exact integers they become on the wire. Pterodactyl takes
    /// memory and disk in MiB and CPU as a percentage of one thread, so these are the values the
    /// creation request carries verbatim.
    /// </summary>
    public static TheoryData<string, int, int, int, int, int> Allocations => new()
    {
        // slug,             GB, memory MiB, cpu %, disk GB, disk MiB
        { "zomboid-4gb",      4,       4096,   300,      25,    25600 },
        { "zomboid-5gb",      5,       5120,   400,      25,    25600 },
        { "zomboid-6gb",      6,       6144,   400,      25,    25600 },
        { "zomboid-8gb",      8,       8192,   500,      40,    40960 },
        { "zomboid-10gb",    10,      10240,   500,      40,    40960 },
        { "zomboid-12gb",    12,      12288,   600,      40,    40960 },
        { "zomboid-14gb",    14,      14336,   600,      40,    40960 },
        { "zomboid-16gb",    16,      16384,   700,      40,    40960 }
    };

    [Theory]
    [MemberData(nameof(Allocations))]
    public void EveryPlanMapsToTheAgreedPterodactylLimits(
        string slug,
        int memoryGb,
        int memoryMib,
        int cpuPercent,
        int diskGb,
        int diskMib)
    {
        var plan = _sut.FindBySlug(slug);

        Assert.NotNull(plan);
        Assert.Equal(memoryGb, plan.MemoryGb);
        Assert.Equal(memoryMib, plan.MemoryMib);
        Assert.Equal(cpuPercent, plan.CpuPercent);
        Assert.Equal(diskGb, plan.DiskGb);
        Assert.Equal(diskMib, plan.DiskMib);
    }

    [Fact]
    public void TheLadderIsExactlyTheEightAgreedTiers()
    {
        Assert.Equal(
            Allocations.Select(row => (string)row[0]!),
            _sut.Plans.Select(plan => plan.Slug));
    }

    /// <summary>
    /// The limits struct is what the creation request is built from, so it has to carry the same
    /// numbers the card advertises - not a second conversion of them.
    /// </summary>
    [Theory]
    [MemberData(nameof(Allocations))]
    public void TheResourceLimitsStructCarriesTheSameFigures(
        string slug,
        int memoryGb,
        int memoryMib,
        int cpuPercent,
        int diskGb,
        int diskMib)
    {
        _ = memoryGb;
        _ = diskGb;

        var limits = _sut.FindBySlug(slug)!.ToResourceLimits();

        Assert.Equal(memoryMib, limits.MemoryMib);
        Assert.Equal(cpuPercent, limits.CpuPercent);
        Assert.Equal(diskMib, limits.DiskMib);
    }

    /// <summary>1024, not 1000. A MB conversion here would under-provision every plan by ~2.4%.</summary>
    [Fact]
    public void MemoryIsConvertedInMebibytes()
    {
        Assert.All(_sut.Plans, plan => Assert.Equal(0, plan.MemoryMib % 1024));
        Assert.Equal(1024, StaticPlanCatalogService.MibPerGib);
    }

    [Fact]
    public void EveryPlanHasFourGigabytesOrMoreAndACpuCeilingAboveOneThread()
    {
        Assert.All(_sut.Plans, plan =>
        {
            Assert.True(plan.MemoryGb >= 4, $"{plan.Slug} is under 4 GB");
            Assert.True(plan.CpuPercent >= 300, $"{plan.Slug} allows less than three threads");
        });
    }

    /// <summary>
    /// Storage steps once, at the 8 GiB tier, and never steps back down. The tiers above it are
    /// bought for long-lived worlds and large mod lists, which grow on disk.
    /// </summary>
    [Fact]
    public void StorageStepsUpOnceAtTheEightGigabyteTier()
    {
        Assert.All(
            _sut.Plans.Where(plan => plan.MemoryGb <= 6),
            plan => Assert.Equal(StaticPlanCatalogService.StandardDiskGb, plan.DiskGb));

        Assert.All(
            _sut.Plans.Where(plan => plan.MemoryGb >= 8),
            plan => Assert.Equal(StaticPlanCatalogService.ExpandedDiskGb, plan.DiskGb));
    }

    [Fact]
    public void NothingDecreasesAsPlansGrow()
    {
        var memory = _sut.Plans.Select(plan => plan.MemoryMib).ToArray();
        var cpu = _sut.Plans.Select(plan => plan.CpuPercent).ToArray();
        var disk = _sut.Plans.Select(plan => plan.DiskMib).ToArray();
        var backups = _sut.Plans.Select(plan => plan.BackupLimit).ToArray();

        Assert.Equal(memory.OrderBy(value => value), memory);
        Assert.Equal(cpu.OrderBy(value => value), cpu);
        Assert.Equal(disk.OrderBy(value => value), disk);
        Assert.Equal(backups.OrderBy(value => value), backups);
    }

    [Fact]
    public void SlugsAreUnique_BecauseTheyKeyBothPricingAndProvisioning()
    {
        var slugs = _sut.Plans.Select(plan => plan.Slug).ToArray();

        Assert.Equal(slugs.Length, slugs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>
    /// The slug is also the customer-visible key in configuration, so a slug saying 8 GB on a
    /// plan carrying 10 would misprice the tier the moment someone set an override.
    /// </summary>
    [Fact]
    public void EverySlugStatesThePlansOwnMemory()
    {
        Assert.All(_sut.Plans, plan =>
            Assert.Equal($"zomboid-{plan.MemoryGb}gb", plan.Slug));
    }

    [Fact]
    public void EveryPlanNamesARegisteredGameTemplate()
    {
        Assert.All(_sut.Plans, plan =>
            Assert.Equal(GameTemplateCatalog.ProjectZomboidId, plan.GameTemplateId));
    }

    [Fact]
    public void EveryPlanIsNamed_AndExactlyOneIsRecommended()
    {
        Assert.All(_sut.Plans, plan => Assert.False(string.IsNullOrWhiteSpace(plan.Name)));
        Assert.Single(_sut.Plans, plan => plan.IsRecommended);
    }

    /// <summary>
    /// Project Zomboid binds two ports and uses no relational database. These are technical
    /// requirements of the game, not tiers, so they must not vary between plans.
    /// </summary>
    [Fact]
    public void FeatureLimitsMatchWhatTheGameActuallyNeeds()
    {
        Assert.All(_sut.Plans, plan =>
        {
            Assert.Equal(0, plan.DatabaseLimit);
            Assert.Equal(2, plan.AllocationLimit);
            Assert.True(plan.BackupLimit >= 1, $"{plan.Slug} grants no backups");
        });
    }

    [Fact]
    public void FeatureLimitsStructMirrorsThePlan()
    {
        var plan = _sut.FindBySlug("zomboid-8gb")!;
        var features = plan.ToFeatureLimits();

        Assert.Equal(plan.DatabaseLimit, features.Databases);
        Assert.Equal(plan.AllocationLimit, features.Allocations);
        Assert.Equal(plan.BackupLimit, features.Backups);
    }

    // ── Pricing ───────────────────────────────────────────────────────────

    /// <summary>
    /// The agreed rate card applied to each tier, then snapped to the nearest x.99.
    /// </summary>
    /// <remarks>
    /// These are not eight chosen numbers. They are what CPU 0.90 / memory 1.20 / 20 GB 0.30
    /// produces for the resources each tier ships, put through one rounding rule. The table
    /// exists so that a change to the rates, the resources or the rule has to be a deliberate
    /// one rather than something noticed later on the live site.
    /// </remarks>
    public static TheoryData<string, decimal, decimal> RatedPrices => new()
    {
        // slug,             computed, charged
        // 4 GB: (300% x 0.90) + (4 x 1.20) + (25/20 x 0.30) = 2.70 + 4.80 + 0.375
        { "zomboid-4gb", 7.88m, 7.99m },
        { "zomboid-5gb", 9.98m, 9.99m },
        { "zomboid-6gb", 11.18m, 10.99m },
        { "zomboid-8gb", 14.70m, 14.99m },
        { "zomboid-10gb", 17.10m, 16.99m },
        { "zomboid-12gb", 20.40m, 19.99m },
        { "zomboid-14gb", 22.80m, 22.99m },
        { "zomboid-16gb", 26.10m, 25.99m }
    };

    [Theory]
    [MemberData(nameof(RatedPrices))]
    public void EveryPlanIsPricedFromTheRateCard(string slug, decimal computed, decimal charged)
    {
        _ = computed;

        var catalog = Build(rates: ShippedRates);

        Assert.Equal(charged, catalog.FindBySlug(slug)!.PriceMonthly);
    }

    /// <summary>
    /// Charm pricing is a rule laid over the arithmetic, not a change to it, so the rate card on
    /// its own still produces the honest figure.
    /// </summary>
    [Theory]
    [MemberData(nameof(RatedPrices))]
    public void TheRateCardItselfIsUnrounded(string slug, decimal computed, decimal charged)
    {
        _ = charged;

        var plan = Build(rates: ShippedRates).FindBySlug(slug)!;

        Assert.Equal(computed, ShippedRates.Compute(plan.CpuPercent, plan.MemoryGb, plan.DiskGb));
    }

    /// <summary>
    /// The rule: at or above the half unit the price keeps its unit and takes 99 cents; below it
    /// the price drops a whole unit first. Dropping the unit is the point - it is what makes
    /// 17.10 read as sixteen rather than seventeen.
    /// </summary>
    [Theory]
    [InlineData(14.70, 14.99)]
    [InlineData(14.50, 14.99)]
    [InlineData(14.49, 13.99)]
    [InlineData(17.10, 16.99)]
    [InlineData(20.00, 19.99)]
    [InlineData(1.20, 0.99)]
    [InlineData(0.30, 0.99)]
    public void CharmPricingSnapsToTheNearestNinetyNine(decimal computed, decimal expected)
    {
        Assert.Equal(expected, HostingPlanPricingOptions.ToCharmPrice(computed));
    }

    [Fact]
    public void CharmPricingNeverProducesANegativePrice()
    {
        Assert.All(
            new[] { 0m, 0.01m, 0.49m, 0.99m, 1m },
            value => Assert.True(HostingPlanPricingOptions.ToCharmPrice(value) >= 0.99m));
    }

    [Fact]
    public void CharmPricingCanBeSwitchedOff()
    {
        var catalog = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(),
            Options.Create(new HostingPlanPricingOptions
            {
                Rates = ShippedRates,
                CharmPricing = false
            }));

        Assert.Equal(7.88m, catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
    }

    /// <summary>
    /// Every charged price ends in 99. One that slipped through unrounded would sit in a row
    /// beside seven that did not and read as a mistake.
    /// </summary>
    [Fact]
    public void EveryChargedPriceEndsInNinetyNine()
    {
        Assert.All(
            Build(rates: ShippedRates).Plans,
            plan => Assert.Equal(0.99m, plan.PriceMonthly!.Value % 1m));
    }

    /// <summary>
    /// Storage is prorated, not billed in whole blocks. Rounding 25 GB up to two blocks would
    /// charge the 6 GB tier the same for storage as the 16 GB tier.
    /// </summary>
    [Fact]
    public void StorageIsProratedWithinABlock()
    {
        var rates = ShippedRates;

        var quarterBlock = rates.Compute(cpuPercent: 0, memoryGb: 0, diskGb: 5);
        var wholeBlock = rates.Compute(cpuPercent: 0, memoryGb: 0, diskGb: 20);

        Assert.Equal(0.08m, quarterBlock);   // 0.30 x 5/20 = 0.075, rounded away from zero
        Assert.Equal(0.30m, wholeBlock);
    }

    /// <summary>
    /// With no rate card and no override, nothing is priced. There is deliberately no fallback:
    /// a default price is a price someone reads as real.
    /// </summary>
    [Fact]
    public void WithNoRateCardAndNoOverrides_NothingIsPriced()
    {
        Assert.False(_sut.HasPricing);
        Assert.All(_sut.Plans, plan =>
        {
            Assert.False(plan.IsPriced);
            Assert.Null(plan.PriceMonthly);
            Assert.Null(_sut.Quote(plan, BillingPeriod.Monthly));
            Assert.Null(_sut.Quote(plan, BillingPeriod.Annual));
        });
    }

    /// <summary>
    /// A rate card missing one rate prices nothing. Charging for memory and CPU but not storage
    /// because one line was left blank is worse than showing the tier as unpriced.
    /// </summary>
    [Theory]
    [InlineData("", "1.20", "0.30")]
    [InlineData("0.90", "", "0.30")]
    [InlineData("0.90", "1.20", "")]
    [InlineData("0.90", "1.20", "nine")]
    public void AnIncompleteRateCardPricesNothing(string cpu, string memory, string disk)
    {
        var rates = new PlanRateCard
        {
            CpuPer100Percent = cpu,
            MemoryPerGb = memory,
            DiskPerBlock = disk,
            DiskBlockGb = 20
        };

        Assert.False(rates.IsConfigured);
        Assert.False(Build(rates: rates).HasPricing);
    }

    [Fact]
    public void ABlockSizeOfZeroPricesNothing_RatherThanDividingByIt()
    {
        var rates = ShippedRates;
        rates.DiskBlockGb = 0;

        Assert.False(rates.IsConfigured);
        Assert.Null(rates.Compute(300, 4, 25));
    }

    [Fact]
    public void AConfiguredPriceOverridesTheRateCard_ForThatSlugOnly()
    {
        var catalog = Build(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zomboid-5gb"] = "11.99"
            },
            ShippedRates);

        Assert.Equal(11.99m, catalog.FindBySlug("zomboid-5gb")!.PriceMonthly);
        Assert.Equal(7.99m, catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
    }

    [Fact]
    public void AConfiguredPriceIsPickedUpEvenWithNoRateCard()
    {
        var catalog = Build(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zomboid-5gb"] = "11.99"
        });

        Assert.True(catalog.HasPricing);
        Assert.Equal(11.99m, catalog.FindBySlug("zomboid-5gb")!.PriceMonthly);
        Assert.Null(catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
    }

    /// <summary>
    /// Prices and rates are held as strings and parsed invariantly. Binding them as decimals
    /// would read <c>0.90</c> as 90 on a machine whose decimal separator is a comma - which is
    /// this one.
    /// </summary>
    [Fact]
    public void PricesAndRatesParseInvariantly_RegardlessOfTheHostCulture()
    {
        using var brazilian = new CultureScope(SupportedCultures.Portuguese);

        var catalog = Build(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zomboid-16gb"] = "9.99"
            },
            ShippedRates);

        Assert.Equal(9.99m, catalog.FindBySlug("zomboid-16gb")!.PriceMonthly);
        Assert.Equal(7.99m, catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("free")]
    [InlineData("0")]
    [InlineData("-5")]
    public void AnUnusableOverrideFallsBackToTheRateCard(string configured)
    {
        var catalog = Build(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zomboid-4gb"] = configured
            },
            ShippedRates);

        Assert.Equal(7.99m, catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
    }

    /// <summary>
    /// A figure typed into configuration is a decision somebody already made, so it is charged
    /// exactly as written - charm rounding would silently overrule them.
    /// </summary>
    [Fact]
    public void AConfiguredOverrideIsNotCharmRounded()
    {
        var catalog = Build(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zomboid-5gb"] = "11.50"
            },
            ShippedRates);

        Assert.Equal(11.50m, catalog.FindBySlug("zomboid-5gb")!.PriceMonthly);
    }

    [Theory]
    [InlineData("")]
    [InlineData("free")]
    [InlineData("-5")]
    public void AnUnusableOverrideWithNoRateCardLeavesThePlanUnpriced(string configured)
    {
        var catalog = Build(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zomboid-4gb"] = configured
        });

        Assert.Null(catalog.FindBySlug("zomboid-4gb")!.PriceMonthly);
        Assert.False(catalog.HasPricing);
    }

    /// <summary>A bigger plan must never cost less than a smaller one.</summary>
    [Fact]
    public void PricesRiseWithTheLadder()
    {
        var prices = Build(rates: ShippedRates).Plans.Select(plan => plan.PriceMonthly!.Value).ToArray();

        Assert.Equal(prices.OrderBy(value => value), prices);
        Assert.Equal(prices.Length, prices.Distinct().Count());
    }

    [Fact]
    public void DiscountsApplyToAConfiguredPrice()
    {
        var catalog = Build(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zomboid-4gb"] = "10.00"
        });

        var plan = catalog.FindBySlug("zomboid-4gb")!;

        // Annual takes 10% off twelve months: 10.00 x 12 x 0.90.
        var annual = catalog.Quote(plan, BillingPeriod.Annual);

        Assert.NotNull(annual);
        Assert.Equal(120.00m, annual.BaseAmount);
        Assert.Equal(108.00m, annual.FinalAmount);
        Assert.Equal(9.00m, annual.EffectiveMonthly);
    }

    [Fact]
    public void FindBySlugIsForgivingAboutCaseAndWhitespace_AndRefusesNonsense()
    {
        Assert.NotNull(_sut.FindBySlug("ZOMBOID-4GB"));
        Assert.NotNull(_sut.FindBySlug("  zomboid-4gb  "));
        Assert.Null(_sut.FindBySlug("zomboid-99gb"));
        Assert.Null(_sut.FindBySlug(""));
        Assert.Null(_sut.FindBySlug(null));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
