using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The infrastructure page's structure - and, more importantly, the guarantee that it states
/// no specification nobody has agreed to.
/// </summary>
public class StaticInfrastructureContentServiceTests
{
    private readonly StaticInfrastructureContentService _sut = new();

    /// <summary>
    /// The whole point of the page in its current state: every hardware figure is pending, so
    /// the page renders a visible placeholder instead of a plausible-looking number. A value
    /// appearing here is how a placeholder quietly becomes a commitment.
    /// </summary>
    [Fact]
    public void NoHardwareSpecificationIsStatedYet()
    {
        Assert.All(_sut.SpecSheet, spec => Assert.True(spec.IsPending, $"{spec.Kind} has a value"));

        Assert.All(_sut.Nodes, node =>
            Assert.All(node.Specs, spec => Assert.True(spec.IsPending, $"{node.Ordinal}/{spec.Kind} has a value")));
    }

    /// <summary>
    /// An allocation bar drawn at some percentage is a claim about capacity. Until there is a
    /// real figure the nodes report none, and the page draws an indeterminate bar.
    /// </summary>
    [Fact]
    public void NoAllocationFigureIsStatedYet()
    {
        Assert.All(_sut.Nodes, node => Assert.False(node.HasKnownLoad, $"{node.Ordinal} has a load figure"));
    }

    [Fact]
    public void TheSpecSheetCoversEveryDimensionExactlyOnce()
    {
        var kinds = _sut.SpecSheet.Select(spec => spec.Kind).ToArray();

        Assert.Equal(Enum.GetValues<HardwareSpecKind>().Length, kinds.Length);
        Assert.Equal(kinds.Length, kinds.Distinct().Count());
    }

    [Fact]
    public void EveryNodeReportsTheSameDimensions_SoTheTopologyRowsLineUp()
    {
        var expected = _sut.SpecSheet.Select(spec => spec.Kind).ToArray();

        Assert.All(_sut.Nodes, node =>
            Assert.Equal(expected, node.Specs.Select(spec => spec.Kind).ToArray()));
    }

    [Fact]
    public void NodeOrdinalsAreUniqueAndTwoDigit()
    {
        var ordinals = _sut.Nodes.Select(node => node.Ordinal).ToArray();

        Assert.Equal(ordinals.Length, ordinals.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ordinals, ordinal => Assert.Matches("^[0-9]{2}$", ordinal));
    }

    [Fact]
    public void ChapterIdsAreUnique_SoInPageLinksAndHeadingIdsStayUnambiguous()
    {
        var ids = _sut.Chapters.Select(chapter => chapter.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// The page numbers its chapters continuously with the two sections above them, which are
    /// fixed at 01 and 02.
    /// </summary>
    [Fact]
    public void ChapterIndicesRunInOrderFromThree()
    {
        Assert.Equal(["03", "04", "05", "06", "07", "08"], _sut.Chapters.Select(chapter => chapter.Index));
    }

    [Fact]
    public void EveryTopicAppearsExactlyOnce()
    {
        var topics = _sut.Chapters.Select(chapter => chapter.Topic).ToArray();

        Assert.Equal(Enum.GetValues<InfrastructureTopic>().Length, topics.Length);
        Assert.Equal(topics.Length, topics.Distinct().Count());
    }

    /// <summary>
    /// The brief for this page was explicit that it must not be six identical cards. Adjacent
    /// chapters therefore never share a composition.
    /// </summary>
    [Fact]
    public void NoTwoConsecutiveChaptersShareALayout()
    {
        var layouts = _sut.Chapters.Select(chapter => chapter.Layout).ToArray();

        for (var index = 1; index < layouts.Length; index++)
        {
            Assert.True(
                layouts[index] != layouts[index - 1],
                $"chapters {index} and {index + 1} both use {layouts[index]}");
        }
    }

    /// <summary>The mirroring alternates, so the page zig-zags rather than running down one gutter.</summary>
    [Fact]
    public void MirroredChaptersAreSpreadThroughThePage()
    {
        Assert.Contains(_sut.Chapters, chapter => chapter.Mirrored);
        Assert.Contains(_sut.Chapters, chapter => !chapter.Mirrored);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
