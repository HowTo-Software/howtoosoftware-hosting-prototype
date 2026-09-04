using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The one pricing policy: monthly at list, quarterly five per cent off, annual fifteen off,
/// and nothing else.
/// </summary>
public class BillingPolicyTests
{
    [Theory]
    [InlineData(BillingPeriod.Monthly, 1, 0)]
    [InlineData(BillingPeriod.Quarterly, 3, 5)]
    [InlineData(BillingPeriod.Annual, 12, 15)]
    public void ThePeriodsAreExactlyTheAgreedThree(BillingPeriod period, int months, int percent)
    {
        Assert.Equal(months, BillingPolicy.Months(period));
        Assert.Equal(percent, BillingPolicy.DiscountPercent(period));
    }

    [Fact]
    public void ThereAreThreePeriods_InTheOrderTheyAreOffered()
    {
        Assert.Equal(
            [BillingPeriod.Monthly, BillingPeriod.Quarterly, BillingPeriod.Annual],
            BillingPolicy.Periods);
        Assert.Equal(3, Enum.GetValues<BillingPeriod>().Length);
    }

    [Fact]
    public void MonthlyIsTheListPrice_AndSaysItHasNoDiscount()
    {
        var quote = BillingPolicy.Quote(7.99m, BillingPeriod.Monthly);

        Assert.Equal(7.99m, quote.BaseAmount);
        Assert.Equal(7.99m, quote.FinalAmount);
        Assert.Equal(0m, quote.DiscountAmount);
        Assert.False(quote.IsDiscounted);
        Assert.Equal(7.99m, quote.EffectiveMonthly);
        Assert.Equal(799, quote.FinalAmountMinor);
    }

    /// <summary>The brief's own formula: (P x 3) x 0.95.</summary>
    [Fact]
    public void QuarterlyIsThreeMonthsLessFivePercent()
    {
        var quote = BillingPolicy.Quote(7.99m, BillingPeriod.Quarterly);

        Assert.Equal(23.97m, quote.BaseAmount);
        Assert.Equal(22.77m, quote.FinalAmount);   // 22.7715 rounded to the cent
        Assert.Equal(1.20m, quote.DiscountAmount);
        Assert.Equal(7.59m, quote.EffectiveMonthly);
        Assert.Equal(2277, quote.FinalAmountMinor);
        Assert.True(quote.IsDiscounted);
    }

    /// <summary>The brief's own formula: (P x 12) x 0.85.</summary>
    [Fact]
    public void AnnualIsTwelveMonthsLessFifteenPercent()
    {
        var quote = BillingPolicy.Quote(7.99m, BillingPeriod.Annual);

        Assert.Equal(95.88m, quote.BaseAmount);
        Assert.Equal(81.50m, quote.FinalAmount);   // 81.498 rounded to the cent
        Assert.Equal(14.38m, quote.DiscountAmount);
        Assert.Equal(6.79m, quote.EffectiveMonthly);
        Assert.Equal(8150, quote.FinalAmountMinor);
    }

    [Theory]
    [InlineData(7.99, 22.77, 81.50)]
    [InlineData(9.99, 28.47, 101.90)]
    [InlineData(10.99, 31.32, 112.10)]
    [InlineData(14.99, 42.72, 152.90)]
    [InlineData(16.99, 48.42, 173.30)]
    [InlineData(19.99, 56.97, 203.90)]
    [InlineData(22.99, 65.52, 234.50)]
    [InlineData(25.99, 74.07, 265.10)]
    public void EveryShippedPriceFollowsTheFormulas(decimal monthly, decimal quarterly, decimal annual)
    {
        Assert.Equal(quarterly, BillingPolicy.Quote(monthly, BillingPeriod.Quarterly).FinalAmount);
        Assert.Equal(annual, BillingPolicy.Quote(monthly, BillingPeriod.Annual).FinalAmount);

        // Rounded once, at the end, never per month and then multiplied.
        Assert.Equal(BillingPolicy.RoundToCent(monthly * 3 * 0.95m), quarterly);
        Assert.Equal(BillingPolicy.RoundToCent(monthly * 12 * 0.85m), annual);
    }

    [Fact]
    public void TheDiscountNeverExceedsTheBase_AndTheFinalIsNeverNegative()
    {
        foreach (var period in BillingPolicy.Periods)
        {
            foreach (var price in new[] { 0.99m, 7.99m, 25.99m, 1234.56m })
            {
                var quote = BillingPolicy.Quote(price, period);

                Assert.True(quote.FinalAmount > 0m);
                Assert.True(quote.FinalAmount <= quote.BaseAmount);
                Assert.Equal(quote.BaseAmount - quote.FinalAmount, quote.DiscountAmount);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AQuoteNeedsAPositivePrice(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingPolicy.Quote(price, BillingPeriod.Monthly));
    }

    [Theory]
    [InlineData("monthly", BillingPeriod.Monthly)]
    [InlineData("Quarterly", BillingPeriod.Quarterly)]
    [InlineData("  ANNUAL  ", BillingPeriod.Annual)]
    public void SlugsRoundTrip(string raw, BillingPeriod expected)
    {
        Assert.True(BillingPolicy.TryParse(raw, out var period));
        Assert.Equal(expected, period);
        Assert.Equal(raw.Trim().ToLowerInvariant(), BillingPolicy.Slug(period));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("yearly")]
    [InlineData("1")]
    [InlineData("monthly; drop table")]
    public void AnythingElseIsNotAPeriod(string? raw)
    {
        Assert.False(BillingPolicy.TryParse(raw, out _));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
