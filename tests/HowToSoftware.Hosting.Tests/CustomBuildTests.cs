using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The build-to-order quote: what it accepts, what it charges and what it refuses to guess.
/// </summary>
public class CustomBuildTests : IDisposable
{
    private static PlanRateCard ShippedRates => new()
    {
        CpuPer100Percent = "0.90",
        MemoryPerGb = "1.20",
        DiskPerBlock = "0.30",
        DiskBlockGb = 20
    };

    private readonly CultureScope _culture = new(SupportedCultures.Default);

    public void Dispose() => _culture.Dispose();

    private static RateCardBuildService Build(
        PlanRateCard? rates = null,
        CustomBuildLimits? limits = null)
    {
        var pricing = new HostingPlanPricingOptions
        {
            Rates = rates ?? new PlanRateCard(),
            CustomBuild = limits ?? new CustomBuildLimits()
        };

        var catalog = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(), Options.Create(pricing));

        return new RateCardBuildService(catalog, Options.Create(pricing));
    }

    /// <summary>
    /// A custom build is what you ask for when the ladder runs out, so it starts where the
    /// ladder ends - read from the catalogue rather than restated here.
    /// </summary>
    [Fact]
    public void TheFormStartsWhereTheLargestPlanEnds()
    {
        var service = Build(ShippedRates);
        var largest = service.Bounds;

        Assert.Equal(16, largest.MinMemoryGb);
        Assert.Equal(700, largest.MinCpuPercent);
        Assert.Equal(40, largest.MinDiskGb);
    }

    [Fact]
    public void TheDefaultRequestIsOneStepAboveTheLargestPlan()
    {
        var service = Build(ShippedRates);
        var bounds = service.Bounds;

        var request = service.CreateDefault();

        Assert.Equal(bounds.MinMemoryGb + bounds.MemoryStepGb, request.MemoryGb);
        Assert.Equal(bounds.MinCpuPercent + bounds.CpuStepPercent, request.CpuPercent);
    }

    /// <summary>
    /// A number input is a suggestion to a browser, not a promise to a server. Everything is
    /// clamped again on this side before it is priced.
    /// </summary>
    [Theory]
    [InlineData(-5000, 700, 40)]
    [InlineData(999999, 700, 40)]
    [InlineData(24, -1, 40)]
    [InlineData(24, 99999, 40)]
    [InlineData(24, 700, 0)]
    [InlineData(24, 700, 999999)]
    public void RequestsAreClampedToTheAcceptedRange(int memory, int cpu, int disk)
    {
        var service = Build(ShippedRates);
        var bounds = service.Bounds;

        var estimate = service.Estimate(new CustomBuildRequest(memory, cpu, disk, null, null));

        Assert.NotNull(estimate);
        Assert.InRange(estimate.Request.MemoryGb, bounds.MinMemoryGb, bounds.MaxMemoryGb);
        Assert.InRange(estimate.Request.CpuPercent, bounds.MinCpuPercent, bounds.MaxCpuPercent);
        Assert.InRange(estimate.Request.DiskGb, bounds.MinDiskGb, bounds.MaxDiskGb);
    }

    [Fact]
    public void RequestsAreSnappedToTheIncrements()
    {
        var service = Build(ShippedRates);
        var bounds = service.Bounds;

        var estimate = service.Estimate(new CustomBuildRequest(23, 733, 47, null, null));

        Assert.NotNull(estimate);
        Assert.Equal(0, (estimate.Request.MemoryGb - bounds.MinMemoryGb) % bounds.MemoryStepGb);
        Assert.Equal(0, (estimate.Request.CpuPercent - bounds.MinCpuPercent) % bounds.CpuStepPercent);
        Assert.Equal(0, (estimate.Request.DiskGb - bounds.MinDiskGb) % bounds.DiskStepGb);
    }

    /// <summary>
    /// The visible arithmetic has to be the arithmetic. A breakdown that does not sum to the
    /// total above it is the detail a reader checks once and then distrusts the rest.
    /// </summary>
    [Theory]
    [InlineData(24, 800, 60)]
    [InlineData(32, 1000, 100)]
    [InlineData(64, 3600, 500)]
    [InlineData(16, 700, 40)]
    public void TheBreakdownSumsToTheTotal(int memory, int cpu, int disk)
    {
        var estimate = Build(ShippedRates)
            .Estimate(new CustomBuildRequest(memory, cpu, disk, null, null));

        Assert.NotNull(estimate);
        Assert.Equal(estimate.Subtotal, estimate.CpuCost + estimate.MemoryCost + estimate.DiskCost);
        Assert.Equal(estimate.MonthlyTotal, estimate.Subtotal + estimate.Rounding);
    }

    /// <summary>
    /// Charm rounding is a line of the quote, not something applied after the lines were
    /// printed. A total that silently disagreed with its own breakdown would undo the reason the
    /// breakdown is shown at all.
    /// </summary>
    [Theory]
    [InlineData(24, 800, 60)]
    [InlineData(32, 1000, 100)]
    [InlineData(64, 3600, 500)]
    public void TheQuotedTotalEndsInNinetyNine(int memory, int cpu, int disk)
    {
        var estimate = Build(ShippedRates)
            .Estimate(new CustomBuildRequest(memory, cpu, disk, null, null));

        Assert.NotNull(estimate);
        Assert.Equal(0.99m, estimate.MonthlyTotal % 1m);
        Assert.NotEqual(0m, estimate.Rounding);
    }

    /// <summary>
    /// The CPU ceiling is the whole processor the infrastructure page already names: 36 threads,
    /// and Pterodactyl counts 100% as one thread.
    /// </summary>
    /// <remarks>
    /// The unit is the trap here. 36 threads is 3600, not 36000 - a figure with one zero too
    /// many would offer ten times the hardware that exists.
    /// </remarks>
    [Fact]
    public void TheCpuCeilingIsTheProcessorsThreadCount()
    {
        Assert.Equal(3600, Build(ShippedRates).Bounds.MaxCpuPercent);
    }

    [Fact]
    public void TheMemoryAndStorageCeilingsAreTheAgreedLimits()
    {
        var bounds = Build(ShippedRates).Bounds;

        Assert.Equal(64, bounds.MaxMemoryGb);
        Assert.Equal(500, bounds.MaxDiskGb);
    }

    /// <summary>
    /// A quote for exactly a plan's resources has to agree with that plan's price, or the page
    /// is quoting two different prices for the same server.
    /// </summary>
    [Fact]
    public void AQuoteMatchingTheLargestPlan_AgreesWithItsPrice()
    {
        var pricing = new HostingPlanPricingOptions { Rates = ShippedRates };
        var catalog = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(), Options.Create(pricing));
        var service = new RateCardBuildService(catalog, Options.Create(pricing));

        var largest = catalog.Plans[^1];
        var estimate = service.Estimate(
            new CustomBuildRequest(largest.MemoryGb, largest.CpuPercent, largest.DiskGb, null, null));

        Assert.NotNull(estimate);

        // Both sides go through the same rate card and the same rounding rule, so a quote for a
        // plan's exact resources cannot quote a different price from that plan's card.
        Assert.Equal(largest.PriceMonthly, estimate.MonthlyTotal);
    }

    [Fact]
    public void AskingForMoreCostsMore()
    {
        var service = Build(ShippedRates);

        var small = service.Estimate(new CustomBuildRequest(16, 700, 40, null, null))!;
        var large = service.Estimate(new CustomBuildRequest(32, 1000, 100, null, null))!;

        Assert.True(large.MonthlyTotal > small.MonthlyTotal);
    }

    /// <summary>
    /// With no rate card there is no figure, and the panel says so rather than inventing one.
    /// </summary>
    [Fact]
    public void WithNoRateCard_NothingIsEstimated()
    {
        var service = Build();

        Assert.False(service.CanEstimate);
        Assert.Null(service.Estimate(new CustomBuildRequest(32, 1000, 100, null, null)));
    }

    /// <summary>
    /// The free-text note reaches a mail body, so its length is bounded here rather than being
    /// trusted to a maxlength attribute.
    /// </summary>
    [Fact]
    public void NotesAreTrimmedAndBounded()
    {
        var service = Build(ShippedRates);

        var estimate = service.Estimate(new CustomBuildRequest(
            24, 800, 60, null, "   " + new string('x', 5000) + "   "));

        Assert.NotNull(estimate);
        Assert.NotNull(estimate.Request.Notes);
        Assert.Equal(CustomBuildBounds.MaxNoteLength, estimate.Request.Notes.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnEmptyNoteBecomesNoNote(string? note)
    {
        var estimate = Build(ShippedRates)
            .Estimate(new CustomBuildRequest(24, 800, 60, null, note));

        Assert.NotNull(estimate);
        Assert.Null(estimate.Request.Notes);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-4, null)]
    [InlineData(40, 40)]
    [InlineData(9000, 512)]
    public void PlayerCountsAreBoundedOrDropped(int given, int? expected)
    {
        var estimate = Build(ShippedRates)
            .Estimate(new CustomBuildRequest(24, 800, 60, given, null));

        Assert.NotNull(estimate);
        Assert.Equal(expected, estimate.Request.PlayerSlots);
    }

    /// <summary>
    /// A ceiling configured below the floor would leave a slider with no range at all, so the
    /// bounds keep at least one step above the largest plan whatever configuration says.
    /// </summary>
    [Fact]
    public void ACeilingBelowTheFloorStillLeavesARange()
    {
        var service = Build(ShippedRates, new CustomBuildLimits
        {
            MaxMemoryGb = 1,
            MaxCpuPercent = 1,
            MaxDiskGb = 1
        });

        var bounds = service.Bounds;

        Assert.True(bounds.MaxMemoryGb > bounds.MinMemoryGb);
        Assert.True(bounds.MaxCpuPercent > bounds.MinCpuPercent);
        Assert.True(bounds.MaxDiskGb > bounds.MinDiskGb);
    }

    /// <summary>
    /// Rates parse invariantly here too - the quote reads the same rate card as the plan cards,
    /// and it must read it the same way on a machine whose decimal separator is a comma.
    /// </summary>
    [Fact]
    public void QuotesParseTheRateCardInvariantly()
    {
        using var brazilian = new CultureScope(SupportedCultures.Portuguese);

        var estimate = Build(ShippedRates)
            .Estimate(new CustomBuildRequest(20, 800, 40, null, null));

        Assert.NotNull(estimate);

        // 800% x 0.90 = 7.20, 20 GB x 1.20 = 24.00, 40 GB / 20 x 0.30 = 0.60
        Assert.Equal(7.20m, estimate.CpuCost);
        Assert.Equal(24.00m, estimate.MemoryCost);
        Assert.Equal(0.60m, estimate.DiskCost);
        Assert.Equal(31.80m, estimate.Subtotal);

        // 31.80 is above the half unit, so it keeps its unit and takes 99 cents.
        Assert.Equal(31.99m, estimate.MonthlyTotal);
        Assert.Equal(0.19m, estimate.Rounding);
    }
}

// =============================================================
// (c) 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
