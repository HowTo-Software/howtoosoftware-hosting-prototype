using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Who is offered the HowToSoftware server bot, and what the site is allowed to say about it.
/// </summary>
public class ServerBotTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);

    public void Dispose() => _culture.Dispose();

    private static StaticPlanCatalogService Catalogue() =>
        new(TestLocalizer.For<HomeText>(),
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

    /// <summary>
    /// More than 5 GB, not 5 GB or more. The 5 GB tier is the one this is easy to get wrong on,
    /// so it is named explicitly.
    /// </summary>
    [Theory]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(8, true)]
    [InlineData(16, true)]
    public void EligibilityIsStrictlyAboveTheThreshold(int memoryGb, bool expected)
    {
        Assert.Equal(expected, ServerBot.IsEligible(memoryGb));
    }

    [Fact]
    public void TheThresholdIsFiveGigabytes()
    {
        Assert.Equal(5, ServerBot.MinimumMemoryGb);
    }

    /// <summary>
    /// The card reads its badge off the plan, and the plan reads it off its own memory. This
    /// pins the whole chain, because the failure it prevents - a card advertising software the
    /// server is not offered - is a promise the company cannot keep.
    /// </summary>
    [Fact]
    public void NoPlanAdvertisesTheBotBelowTheThreshold()
    {
        Assert.All(Catalogue().Plans, plan =>
        {
            if (plan.MemoryGb <= ServerBot.MinimumMemoryGb)
            {
                Assert.False(plan.HasServerBot, $"{plan.Slug} claims the bot at {plan.MemoryGb} GB");
            }
            else
            {
                Assert.True(plan.HasServerBot, $"{plan.Slug} omits the bot at {plan.MemoryGb} GB");
            }
        });
    }

    [Fact]
    public void TheTwoSmallestTiersDoNotCarryIt_AndTheRestDo()
    {
        var plans = Catalogue().Plans;

        Assert.Equal(2, plans.Count(plan => !plan.HasServerBot));
        Assert.Equal(plans.Count - 2, plans.Count(plan => plan.HasServerBot));
    }

    /// <summary>
    /// Eligibility never goes backwards up the ladder: once a tier carries the bot, every
    /// larger one does.
    /// </summary>
    [Fact]
    public void EligibilityNeverTurnsOffAgainFurtherUpTheLadder()
    {
        var flags = Catalogue().Plans.Select(plan => plan.HasServerBot).ToArray();

        Assert.Equal(flags.OrderBy(value => value), flags);
    }

    /// <summary>
    /// The bot's copy is bounded by what has actually been confirmed. Percentages, uptime
    /// figures and "guaranteed" anything are exactly what a marketing page invents when nobody
    /// is checking, and none of them have been measured.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    public void TheBotCopyPromisesNoNumbersItCannotStandBehind(string culture)
    {
        using var scope = new CultureScope(culture);

        var text = TestLocalizer.For<HomeText>();
        var copy = string.Join(
            " ",
            new[] { "Bot.Title", "Bot.Lede", "Bot.Caveat", "Bot.Threshold" }
                .Select(key => text[key].Value));

        Assert.DoesNotContain("%", copy, StringComparison.Ordinal);
        Assert.DoesNotContain("SLA", copy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guarante", copy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("garant", copy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("zero lag", copy, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The threshold is formatted into the copy rather than typed into it, so the sentence and
    /// the rule cannot drift apart.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    public void TheThresholdCopyTakesItsFigureFromTheRule(string culture)
    {
        using var scope = new CultureScope(culture);

        var rendered = TestLocalizer.For<HomeText>()["Bot.Threshold", ServerBot.MinimumMemoryGb].Value;

        Assert.Contains($"{ServerBot.MinimumMemoryGb} GB", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", rendered, StringComparison.Ordinal);
    }
}

// =============================================================
// (c) 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
