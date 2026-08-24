using System.Text.RegularExpressions;
using HowToSoftware.Hosting.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// How a visitor's language is chosen, and whether the translations behind that choice are
/// actually there.
/// </summary>
public class SupportedCulturesTests
{
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("pt")]
    [InlineData("pt-PT")]
    [InlineData("PT-br")]
    [InlineData("  pt-BR  ")]
    [InlineData("pt_BR")]
    public void EveryPortugueseVariant_ResolvesToBrazilianPortuguese(string requested)
    {
        Assert.Equal(SupportedCultures.Portuguese, SupportedCultures.Resolve(requested));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("EN")]
    public void EveryEnglishVariant_ResolvesToEnglish(string requested)
    {
        Assert.Equal(SupportedCultures.Default, SupportedCultures.Resolve(requested));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("es-AR")]
    [InlineData("de-DE")]
    [InlineData("zh-Hans")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnUnsupportedLanguage_ResolvesToNothing(string? requested)
    {
        Assert.Null(SupportedCultures.Resolve(requested));
    }

    /// <summary>
    /// The fallback is what a visitor actually sees, so it is asserted separately from the
    /// "no match" result above.
    /// </summary>
    [Theory]
    [InlineData("fr")]
    [InlineData("ja")]
    [InlineData(null)]
    public void AnUnsupportedLanguage_FallsBackToEnglish(string? requested)
    {
        Assert.Equal(SupportedCultures.Default, SupportedCultures.ResolveOrDefault(requested));
    }

    [Fact]
    public void EnglishIsListedFirst_BecauseItIsTheDefault()
    {
        Assert.Equal(SupportedCultures.Default, SupportedCultures.Names[0]);
        Assert.Equal(2, SupportedCultures.Names.Count);
    }
}

/// <summary>
/// First-visit detection: what the browser asks for, and what it gets.
/// </summary>
public class BrowserLanguageCultureProviderTests
{
    [Theory]
    // The header Chrome sends for a Brazilian user.
    [InlineData("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7", SupportedCultures.Portuguese)]
    // Plain "pt" is the case the framework provider gets wrong.
    [InlineData("pt", SupportedCultures.Portuguese)]
    // European Portuguese is still better served in Portuguese than in English.
    [InlineData("pt-PT,pt;q=0.9", SupportedCultures.Portuguese)]
    [InlineData("en-GB,en;q=0.9", SupportedCultures.Default)]
    // Quality decides, not order: Portuguese is preferred here despite being listed second.
    [InlineData("en;q=0.4,pt-BR;q=0.9", SupportedCultures.Portuguese)]
    // Anything at all: give them the default.
    [InlineData("*", SupportedCultures.Default)]
    // Unsupported languages ahead of a supported one must not win.
    [InlineData("fr-FR,fr;q=0.9,pt-BR;q=0.5", SupportedCultures.Portuguese)]
    public void TheHeaderIsResolvedToASupportedCulture(string header, string expected)
    {
        Assert.Equal(expected, BrowserLanguageCultureProvider.Match([header]));
    }

    [Theory]
    [InlineData("fr-FR,de;q=0.8")]
    [InlineData("")]
    [InlineData("   ")]
    public void AHeaderWithNothingWeSpeak_ResolvesToNothing(string header)
    {
        Assert.Null(BrowserLanguageCultureProvider.Match([header]));
    }

    [Fact]
    public void NoHeaderAtAll_ResolvesToNothing()
    {
        Assert.Null(BrowserLanguageCultureProvider.Match(null));
        Assert.Null(BrowserLanguageCultureProvider.Match([]));
    }

    /// <summary>
    /// <c>q=0</c> is an explicit refusal, not a weak preference.
    /// </summary>
    [Fact]
    public void ALanguageRefusedWithQualityZero_IsSkipped()
    {
        Assert.Equal(SupportedCultures.Default, BrowserLanguageCultureProvider.Match(["pt-BR;q=0,en;q=0.5"]));
    }
}

/// <summary>
/// The policy that decides which of the two inputs - the visitor's choice or the browser's
/// preference - wins.
/// </summary>
public class SiteLocalizationTests
{
    private readonly RequestLocalizationOptions _options = SiteLocalization.Create();

    [Fact]
    public void EnglishIsTheDefaultCulture()
    {
        Assert.Equal(SupportedCultures.Default, _options.DefaultRequestCulture.Culture.Name);
        Assert.Equal(SupportedCultures.Default, _options.DefaultRequestCulture.UICulture.Name);
    }

    [Fact]
    public void BothCulturesAreSupported_ForFormattingAndForText()
    {
        Assert.NotNull(_options.SupportedCultures);
        Assert.NotNull(_options.SupportedUICultures);

        foreach (var name in SupportedCultures.Names)
        {
            Assert.Contains(_options.SupportedCultures!, culture => culture.Name == name);
            Assert.Contains(_options.SupportedUICultures!, culture => culture.Name == name);
        }
    }

    /// <summary>
    /// The whole override guarantee is this ordering: an explicit choice is read before the
    /// browser is asked, so it survives every later navigation.
    /// </summary>
    [Fact]
    public void TheCookieIsConsultedBeforeTheBrowser()
    {
        Assert.Collection(
            _options.RequestCultureProviders,
            provider => Assert.IsType<CookieRequestCultureProvider>(provider),
            provider => Assert.IsType<BrowserLanguageCultureProvider>(provider));
    }

    /// <summary>
    /// A culture in the query string would let any link change a visitor's language, and would
    /// be indexed as a duplicate of every page. It is deliberately not a provider.
    /// </summary>
    [Fact]
    public void TheQueryStringCannotChangeTheLanguage()
    {
        Assert.DoesNotContain(_options.RequestCultureProviders, provider =>
            provider is QueryStringRequestCultureProvider);
    }

    [Fact]
    public void TheCultureCookieOutlivesTheSession()
    {
        var cookie = SiteLocalization.CreateCultureCookieOptions(isHttps: true);

        Assert.NotNull(cookie.Expires);
        Assert.True(cookie.Expires > DateTimeOffset.UtcNow.AddDays(300));
        Assert.True(cookie.IsEssential);
        Assert.True(cookie.Secure);
        Assert.Equal("/", cookie.Path);
    }

    [Fact]
    public void TheCookieIsNotMarkedSecureOverPlainHttp_OrItWouldNeverBeSentBack()
    {
        Assert.False(SiteLocalization.CreateCultureCookieOptions(isHttps: false).Secure);
    }
}

/// <summary>
/// The language switcher is a GET that sets a cookie and redirects, which is exactly the shape
/// an open redirect takes if the return path is not constrained.
/// </summary>
public class CultureEndpointsTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/login", "/login")]
    [InlineData("/infrastructure", "/infrastructure")]
    [InlineData("/plans?period=annual", "/plans?period=annual")]
    public void ASiteRelativePathIsKept(string requested, string expected)
    {
        Assert.Equal(expected, CultureEndpoints.SafeRedirect(requested));
    }

    [Theory]
    // Protocol-relative: the browser resolves this to another origin.
    [InlineData("//evil.example")]
    [InlineData("//evil.example/login")]
    // Backslash forms are normalised to forward slashes by browsers.
    [InlineData("/\\evil.example")]
    // Absolute URLs, in every scheme worth worrying about.
    [InlineData("https://evil.example")]
    [InlineData("http://evil.example")]
    [InlineData("javascript:alert(1)")]
    // Relative paths that are not rooted could be resolved against the wrong base.
    [InlineData("login")]
    [InlineData("../admin")]
    // Nothing at all.
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnythingThatCouldLeaveTheSite_FallsBackToTheHomepage(string? requested)
    {
        Assert.Equal("/", CultureEndpoints.SafeRedirect(requested));
    }

    /// <summary>A control character can hide the real target or split a header.</summary>
    [Fact]
    public void APathCarryingControlCharacters_IsRejected()
    {
        Assert.Equal("/", CultureEndpoints.SafeRedirect("/login\r\nSet-Cookie: a=b"));
        Assert.Equal("/", CultureEndpoints.SafeRedirect("/login "));
    }
}

/// <summary>
/// Whether the translations exist at all.
/// </summary>
/// <remarks>
/// These read through a real <see cref="IStringLocalizer{T}"/>, so they also prove the
/// <c>pt-BR</c> satellite assembly is being produced and loaded - a missing satellite fails
/// silently at runtime by returning the English text, or the key.
/// </remarks>
public class LocalizationResourceTests
{
    /// <summary>Placeholders for values a format string will fill in, e.g. <c>{0}</c>.</summary>
    private static readonly Regex FormatSlot = new(@"\{\d+\}", RegexOptions.CultureInvariant);

    public static TheoryData<string> ResourceSets => new() { "Common", "Home", "Login", "Hardware" };

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void EveryEnglishKeyHasAPortugueseTranslation(string set)
    {
        var english = Strings(set, SupportedCultures.Default);
        var portuguese = Strings(set, SupportedCultures.Portuguese);

        var missing = english.Keys.Except(portuguese.Keys, StringComparer.Ordinal).ToArray();

        Assert.True(missing.Length == 0, $"{set}: no pt-BR translation for {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void NoPortugueseKeyIsOrphaned(string set)
    {
        var english = Strings(set, SupportedCultures.Default);
        var portuguese = Strings(set, SupportedCultures.Portuguese);

        var orphaned = portuguese.Keys.Except(english.Keys, StringComparer.Ordinal).ToArray();

        Assert.True(orphaned.Length == 0, $"{set}: pt-BR has {string.Join(", ", orphaned)} but English does not");
    }

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void NoTranslationIsBlank(string set)
    {
        foreach (var culture in SupportedCultures.Names)
        {
            foreach (var (key, value) in Strings(set, culture))
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"{set}/{culture}: {key} is empty");
            }
        }
    }

    /// <summary>
    /// A translation that drops a <c>{0}</c> silently loses the value it was meant to carry;
    /// one that invents an extra slot throws at render time.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void FormatSlotsSurviveTranslation(string set)
    {
        var english = Strings(set, SupportedCultures.Default);
        var portuguese = Strings(set, SupportedCultures.Portuguese);

        foreach (var (key, value) in english)
        {
            if (!portuguese.TryGetValue(key, out var translated))
            {
                continue;
            }

            var expected = FormatSlot.Matches(value).Select(match => match.Value).Order().ToArray();
            var actual = FormatSlot.Matches(translated).Select(match => match.Value).Order().ToArray();

            Assert.True(
                expected.SequenceEqual(actual, StringComparer.Ordinal),
                $"{set}: {key} expects [{string.Join(", ", expected)}] but pt-BR has [{string.Join(", ", actual)}]");
        }
    }

    /// <summary>
    /// The infrastructure page marks unwritten copy with a deliberate, unmissable string. The
    /// exact wording was specified, so it is pinned here: a "helpful" rewrite into something
    /// that reads like real copy is precisely the mistake to catch.
    /// </summary>
    [Theory]
    [InlineData(SupportedCultures.Default, "TEXT ABOUT HERE")]
    [InlineData(SupportedCultures.Portuguese, "TEXTO SOBRE AQUI")]
    public void TheCopyPlaceholderIsExact(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, TestLocalizer.For<HardwareText>()["Placeholder"].Value);
    }

    /// <summary>Reads every string in one resource set for one culture.</summary>
    /// <remarks>
    /// English is the <em>neutral</em> resource - it has no culture suffix, so it lives under
    /// the invariant culture rather than in an <c>en</c> satellite. Enumerating it as "en"
    /// would look for a satellite that rightly does not exist.
    /// </remarks>
    private static Dictionary<string, string> Strings(string set, string culture)
    {
        using var scope = new CultureScope(culture == SupportedCultures.Default ? string.Empty : culture);

        IStringLocalizer localizer = set switch
        {
            "Common" => TestLocalizer.For<CommonText>(),
            "Home" => TestLocalizer.For<HomeText>(),
            "Login" => TestLocalizer.For<LoginText>(),
            _ => TestLocalizer.For<HardwareText>()
        };

        // includeParentCultures: false, so pt-BR does not silently answer with English.
        return localizer.GetAllStrings(includeParentCultures: false)
            .ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
