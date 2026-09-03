using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The hero panel draws a meter beside a reading, and it derives the fill from the reading
/// itself rather than being handed a second number. These cover the case that matters: a bar
/// must never be able to disagree with the figure printed next to it, and a value that is not
/// a ratio must get no bar at all rather than a guessed one.
/// </summary>
public sealed class TelemetrySignalTests
{
    private static TelemetrySignal Reading(string value) =>
        new("SURVIVORS", value, SignalTone.Data);

    [Theory]
    [InlineData("27 / 64", 27d / 64d)]
    [InlineData("82 / 82", 1d)]
    [InlineData("0 / 64", 0d)]
    [InlineData("27/64", 27d / 64d)]
    [InlineData("  27  /  64  ", 27d / 64d)]
    public void ARatioBecomesAFraction(string value, double expected)
    {
        Assert.Equal(expected, Reading(value).Ratio!.Value, 6);
    }

    [Theory]
    [InlineData("06D 14H")]      // a duration has no denominator
    [InlineData("82 MODS")]      // neither has a count
    [InlineData("42")]           // nor a build number
    [InlineData("OPERATIONAL")]  // nor a state
    [InlineData("")]
    public void AnythingThatIsNotARatioGetsNoMeter(string value)
    {
        Assert.Null(Reading(value).Ratio);
    }

    [Theory]
    [InlineData("27 / 0")]       // dividing by it would be undefined
    [InlineData("27 / -4")]
    [InlineData("-1 / 64")]
    [InlineData("a / b")]
    [InlineData("1.5 / 2")]      // whole numbers only; a decimal here means something else
    public void AnUnusableRatioGetsNoMeterEither(string value)
    {
        Assert.Null(Reading(value).Ratio);
    }

    /// <remarks>
    /// A world that has been over-filled - more players than slots, which a badly configured
    /// server can report - must not draw a bar past the end of its own track.
    /// </remarks>
    [Fact]
    public void AReadingOverItsTotalIsClampedToFull()
    {
        Assert.Equal(1d, Reading("70 / 64").Ratio!.Value, 6);
    }

    /// <remarks>
    /// The fraction is formatted into a CSS percentage by the component. Under pt-BR the
    /// default formatter writes a comma, which silently kills the declaration, so the value
    /// has to survive the round trip in either culture.
    /// </remarks>
    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    public void TheFractionIsCultureIndependent(string culture)
    {
        using var _ = new CultureScope(culture);
        Assert.Equal(27d / 64d, Reading("27 / 64").Ratio!.Value, 6);
    }
}
