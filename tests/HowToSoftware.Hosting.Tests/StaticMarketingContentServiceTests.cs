using System.Text.RegularExpressions;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Content assertions run against the English resource set, so the culture is pinned for the
/// life of each test rather than inherited from whatever locale the machine is set to.
/// </summary>
public class StaticMarketingContentServiceTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly StaticMarketingContentService _sut = new(TestLocalizer.For<CommonText>(), TestLocalizer.For<HomeText>());

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void EveryContentCollection_IsPopulated()
    {
        Assert.NotEmpty(_sut.PrimaryNavigation);
        Assert.NotEmpty(_sut.ProductLinks);
        Assert.NotEmpty(_sut.CompanyLinks);
        Assert.NotEmpty(_sut.LegalPlaceholders);
        Assert.NotEmpty(_sut.HeroSignals);
        Assert.NotEmpty(_sut.TelemetryStrip);
        Assert.NotEmpty(_sut.WorldEvents);
        Assert.NotEmpty(_sut.WorkshopItems);
        Assert.NotEmpty(_sut.ProvisioningStages);
        Assert.NotEmpty(_sut.Nodes);
        Assert.NotEmpty(_sut.Capabilities);
        Assert.NotEmpty(_sut.Faqs);
    }

    /// <summary>
    /// Three entries, and these three.
    /// </summary>
    /// <remarks>
    /// The menu was seven, five of which were anchors into one long page. It is deliberately
    /// short now, so this asserts the whole list rather than the presence of members - the
    /// failure worth catching is somebody adding a fourth, not somebody removing one.
    /// </remarks>
    [Fact]
    public void PrimaryNavigation_IsTheThreeQuestionsAVisitorArrivesWith()
    {
        Assert.Equal(
            ["/project-zomboid", "/infrastructure", "/project-zomboid#plans"],
            _sut.PrimaryNavigation.Select(link => link.Href));
    }

    /// <summary>
    /// Every entry has to reach a page that exists. An anchor into a page that was deleted, or
    /// a route that was renamed, fails silently in a menu.
    /// </summary>
    [Theory]
    [InlineData("/project-zomboid")]
    [InlineData("/infrastructure")]
    public void PrimaryNavigation_PointsAtRealRoutes(string route)
    {
        Assert.Contains(
            _sut.PrimaryNavigation,
            link => link.Href.Split('#')[0] == route);
    }

    [Fact]
    public void NavigationLinks_AllHaveLabelAndTarget()
    {
        var allLinks = _sut.PrimaryNavigation
            .Concat(_sut.ProductLinks)
            .Concat(_sut.CompanyLinks);

        Assert.All(allLinks, link =>
        {
            Assert.False(string.IsNullOrWhiteSpace(link.Label));
            Assert.False(string.IsNullOrWhiteSpace(link.Href));
        });
    }

    [Fact]
    public void FaqIds_AreUnique_SoAriaControlsStaysUnambiguous()
    {
        var ids = _sut.Faqs.Select(faq => faq.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void WorldEvents_AreAllLabelled()
    {
        Assert.All(_sut.WorldEvents, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Tag), "an event has no tag");
            Assert.False(string.IsNullOrWhiteSpace(item.Detail), $"{item.Tag} has no detail");
        });
    }

    /// <summary>
    /// The figure draws two lanes and its entire argument is the difference between them, so it
    /// needs at least one event of each kind. All interruptions and it never shows the world
    /// being checkpointed; all checkpoints and it never shows the instance being cut.
    /// </summary>
    [Fact]
    public void WorldEvents_ShowBothLanesDoingSomething()
    {
        Assert.Contains(_sut.WorldEvents, item => item.Kind is WorldEventKind.InstanceInterrupted);
        Assert.Contains(_sut.WorldEvents, item => item.Kind is WorldEventKind.WorldCheckpoint);
    }

    [Fact]
    public void WorldEvents_DoNotRepeatATag()
    {
        var tags = _sut.WorldEvents.Select(item => item.Tag).ToArray();

        Assert.Equal(tags.Length, tags.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ProvisioningStages_AreNumberedInOrder()
    {
        var codes = _sut.ProvisioningStages.Select(stage => stage.Code).ToArray();

        Assert.Equal(["01", "02", "03", "04", "05"], codes);
    }

    [Fact]
    public void WorkshopProgress_StaysWithinRange()
    {
        Assert.All(_sut.WorkshopItems, item => Assert.InRange(item.Progress, 0, 100));
    }

    /// <summary>
    /// A load percentage is live data. A statically rendered page cannot keep one current, so
    /// either there is a real figure in range or there is none at all - never a number frozen at
    /// build time and read as a fact about the platform right now.
    /// </summary>
    [Fact]
    public void NodeLoad_IsEitherAbsentOrInRange()
    {
        Assert.All(_sut.Nodes, node =>
        {
            if (node.LoadPercent is { } load)
            {
                Assert.InRange(load, 0, 100);
            }
        });
    }

    /// <summary>
    /// The prototype advertised "256 GB ECC" on hardware that cannot take ECC. The homepage node
    /// panels now carry the specifications read from the running hosts.
    /// </summary>
    [Fact]
    public void NodeSpecifications_DoNotClaimEccMemory()
    {
        Assert.All(_sut.Nodes, node =>
            Assert.All(node.Specs, spec =>
                Assert.DoesNotContain("ECC", spec.Value, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// The pricing answer has to describe the ladder that is actually on sale.
    /// </summary>
    /// <remarks>
    /// It has been wrong once already: it went on describing four tiers and 25 GB of storage
    /// after the ladder grew to eight and storage stepped at the 8 GB tier. Matched on the
    /// figures rather than on a phrase, so the answer can be rewritten freely - it just cannot
    /// describe a different product from the one the cards sell.
    /// </remarks>
    [Fact]
    public void Faqs_DescribeTheLadderThatIsActuallyOnSale()
    {
        var pricing = _sut.Faqs.SingleOrDefault(faq => faq.Id == "pricing");

        Assert.NotNull(pricing);
        Assert.Contains("4 GB", pricing.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("16 GB", pricing.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("25 GB", pricing.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("40 GB", pricing.Answer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prices are configured now, so no answer may still be telling a visitor they are not.
    /// </summary>
    /// <remarks>
    /// The inverse of the guard this replaced. While the cards had no figures the page had to
    /// disclose that; now that they do, the same sentence would be the false statement.
    /// </remarks>
    [Fact]
    public void Faqs_DoNotStillClaimPricingIsUndecided()
    {
        var undecided = new Regex(
            @"not (yet )?(decided|set|final)|pricing is pending|placeholder price",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.All(_sut.Faqs, faq => Assert.DoesNotMatch(undecided, faq.Answer));
    }

    /// <summary>
    /// The first prototype advertises Project Zomboid only; no other title may appear.
    /// </summary>
    [Fact]
    public void NoCopy_AdvertisesAnotherGameOrProduct()
    {
        var forbidden = new Regex(
            @"\b(?:Minecraft|Palworld|FiveM|Valheim|VPS)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.All(AllCopy(), text => Assert.DoesNotMatch(forbidden, text));
    }

    private IEnumerable<string> AllCopy()
    {
        foreach (var link in _sut.PrimaryNavigation.Concat(_sut.ProductLinks).Concat(_sut.CompanyLinks))
        {
            yield return link.Label;
        }

        foreach (var text in _sut.LegalPlaceholders)
        {
            yield return text;
        }

        foreach (var signal in _sut.HeroSignals.Concat(_sut.TelemetryStrip))
        {
            yield return signal.Label;
            yield return signal.Value;
        }

        foreach (var stage in _sut.ProvisioningStages)
        {
            yield return stage.Title;
            yield return stage.Detail;
        }

        foreach (var capability in _sut.Capabilities)
        {
            yield return capability.Kicker;
            yield return capability.Headline;
            yield return capability.Body;
        }

        foreach (var node in _sut.Nodes)
        {
            yield return node.Id;
            yield return node.Region;

            foreach (var spec in node.Specs)
            {
                yield return spec.Label;
                yield return spec.Value;
            }
        }

        foreach (var faq in _sut.Faqs)
        {
            yield return faq.Question;
            yield return faq.Answer;
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
