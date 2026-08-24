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
        Assert.NotEmpty(_sut.WorldCells);
        Assert.NotEmpty(_sut.WorkshopItems);
        Assert.NotEmpty(_sut.ProvisioningStages);
        Assert.NotEmpty(_sut.Nodes);
        Assert.NotEmpty(_sut.Capabilities);
        Assert.NotEmpty(_sut.Faqs);
    }

    [Theory]
    [InlineData("/#world")]
    [InlineData("/#workshop")]
    [InlineData("/#provisioning")]
    [InlineData("/#control-panel")]
    [InlineData("/#plans")]
    [InlineData("/#faq")]
    [InlineData("/infrastructure")]
    public void PrimaryNavigation_LinksToTheSectionsTheHomepageRenders(string anchor)
    {
        Assert.Contains(_sut.PrimaryNavigation, link => link.Href == anchor);
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
    public void WorldCells_StayInsideTheRenderedGrid()
    {
        Assert.All(_sut.WorldCells, cell =>
        {
            Assert.InRange(cell.X, 1, 7);
            Assert.InRange(cell.Y, 1, 5);
        });
    }

    [Fact]
    public void WorldCells_DoNotOverlap()
    {
        var coordinates = _sut.WorldCells.Select(cell => (cell.X, cell.Y)).ToArray();

        Assert.Equal(coordinates.Length, coordinates.Distinct().Count());
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

    [Fact]
    public void NodeLoad_StaysWithinRange()
    {
        Assert.All(_sut.Nodes, node => Assert.InRange(node.LoadPercent, 0, 100));
    }

    /// <summary>
    /// The plan catalogue is placeholder test data, so the page has to say so somewhere a
    /// visitor will actually read. This guards that disclosure against being edited away.
    /// </summary>
    [Fact]
    public void Faqs_DiscloseThatPricingIsPlaceholder()
    {
        var disclosure = new Regex(
            @"placeholder|test data|not a commercial offer",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.Contains(_sut.Faqs, faq => disclosure.IsMatch(faq.Answer));
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
