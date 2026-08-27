using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Binds the <c>appsettings.json</c> the site actually ships and checks what comes out.
/// </summary>
/// <remarks>
/// The other pricing tests construct options in code, which proves the arithmetic but not that
/// the file feeding it is shaped the way the binder expects. A renamed section or a rate nested
/// one level too deep compiles, passes every other test, and ships a page reading PRICE PENDING.
/// </remarks>
public class ShippedConfigurationTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);

    public void Dispose() => _culture.Dispose();

    private static HostingPlanPricingOptions ShippedPricing()
    {
        var options = new HostingPlanPricingOptions();

        new ConfigurationBuilder()
            .AddJsonFile(LocateAppSettings(), optional: false)
            .Build()
            .GetSection(HostingPlanPricingOptions.SectionName)
            .Bind(options);

        return options;
    }

    [Fact]
    public void TheShippedRateCardBinds()
    {
        var pricing = ShippedPricing();

        Assert.True(
            pricing.Rates.IsConfigured,
            "HostingPlans:Rates in appsettings.json did not bind to a usable rate card.");
        Assert.False(string.IsNullOrWhiteSpace(pricing.CurrencySymbol));
    }

    [Fact]
    public void EveryShippedPlanIsPriced()
    {
        var catalog = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(),
            Options.Create(ShippedPricing()));

        Assert.True(catalog.HasPricing);
        Assert.All(catalog.Plans, plan =>
            Assert.True(plan.IsPriced, $"{plan.Slug} renders as unpriced with the shipped configuration."));
    }

    /// <summary>
    /// Every tier needs an entry - blank, but present - so the file itself lists where a
    /// hand-set price would go. A slug missing here is a slug nobody can override.
    /// </summary>
    [Fact]
    public void EveryPlanSlugHasAnOverrideSlot()
    {
        var pricing = ShippedPricing();
        var catalog = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(), Options.Create(pricing));

        Assert.All(catalog.Plans, plan =>
            Assert.True(
                pricing.Prices.ContainsKey(plan.Slug),
                $"appsettings.json has no HostingPlans:Prices entry for {plan.Slug}."));
    }

    /// <summary>The API key must never appear in a file that is committed.</summary>
    [Fact]
    public void TheShippedConfigurationCarriesNoApiKey()
    {
        var raw = File.ReadAllText(LocateAppSettings());

        Assert.DoesNotContain("ptla_", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", raw, StringComparison.OrdinalIgnoreCase);
    }

    private static string LocateAppSettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "HowToSoftware.Hosting", "appsettings.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find appsettings.json above {AppContext.BaseDirectory}.");
    }
}

// =============================================================
// (c) 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
