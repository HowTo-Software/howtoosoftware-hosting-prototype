using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// Builds real <see cref="IStringLocalizer{T}"/> instances over the shipped <c>.resx</c> files.
/// </summary>
/// <remarks>
/// Deliberately not a stub. A fake localiser would happily return a key that has no
/// translation, which is exactly the failure these tests exist to catch.
/// </remarks>
internal static class TestLocalizer
{
    private static readonly IStringLocalizerFactory Factory = new ResourceManagerStringLocalizerFactory(
        Options.Create(new LocalizationOptions()),
        NullLoggerFactory.Instance);

    /// <summary>Creates a localiser for the given resource set.</summary>
    public static IStringLocalizer<T> For<T>() => new StringLocalizer<T>(Factory);
}

/// <summary>
/// Pins the thread's culture for the lifetime of a test.
/// </summary>
/// <remarks>
/// Without this, a test asserting on English copy passes or fails depending on the locale of
/// the machine running it: resource lookup follows <see cref="CultureInfo.CurrentUICulture"/>,
/// so a developer in Brazil gets Portuguese back. A test that says "the English copy discloses
/// X" has to say which culture it means.
/// </remarks>
/// <param name="culture">Culture to apply, e.g. <c>en</c> or <c>pt-BR</c>.</param>
internal sealed class CultureScope(string culture) : IDisposable
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _applied = ApplyTo(culture);

    /// <summary>The culture this scope applied.</summary>
    public CultureInfo Culture => _applied;

    private static CultureInfo ApplyTo(string culture)
    {
        var target = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;
        return target;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
