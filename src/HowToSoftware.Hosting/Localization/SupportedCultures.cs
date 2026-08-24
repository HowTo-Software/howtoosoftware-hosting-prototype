using System.Globalization;

namespace HowToSoftware.Hosting.Localization;

/// <summary>
/// The cultures the site ships translations for, and the single place that decides how a
/// requested language tag maps onto one of them.
/// </summary>
/// <remarks>
/// <para>
/// Keeping the mapping here means components never test for a language. They ask for a
/// localised string and the active culture - chosen once per request by the localisation
/// middleware - decides which resource set answers.
/// </para>
/// <para>
/// English is the default and the fallback: anything that is not English or Portuguese
/// resolves to <see langword="null"/>, which leaves the request on <see cref="Default"/>.
/// </para>
/// </remarks>
public static class SupportedCultures
{
    /// <summary>English - the default culture and the fallback for anything unsupported.</summary>
    public const string Default = "en";

    /// <summary>Brazilian Portuguese.</summary>
    public const string Portuguese = "pt-BR";

    /// <summary>Every supported culture name, default first.</summary>
    public static IReadOnlyList<string> Names { get; } = [Default, Portuguese];

    /// <summary>Every supported culture, default first.</summary>
    public static IReadOnlyList<CultureInfo> All { get; } =
        [.. Names.Select(CultureInfo.GetCultureInfo)];

    /// <summary>The default culture.</summary>
    public static CultureInfo DefaultCulture { get; } = CultureInfo.GetCultureInfo(Default);

    /// <summary>
    /// Maps a requested language tag onto a supported culture name.
    /// </summary>
    /// <param name="requested">
    /// A BCP 47 tag such as <c>pt-BR</c>, <c>pt</c>, <c>pt-PT</c> or <c>en-GB</c>. Case and
    /// surrounding whitespace are ignored.
    /// </param>
    /// <returns>
    /// The supported culture name, or <see langword="null"/> when the site has no translation
    /// for it. Every Portuguese variant resolves to <see cref="Portuguese"/>: a
    /// Portuguese-speaking visitor is far better served by Brazilian Portuguese than by the
    /// English fallback.
    /// </returns>
    public static string? Resolve(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return null;
        }

        var tag = requested.Trim();

        // The primary subtag is what decides the language; the region only refines it, and we
        // ship exactly one variant per language.
        var separator = tag.AsSpan().IndexOfAny('-', '_');
        var language = separator < 0 ? tag : tag[..separator];

        return language.ToLowerInvariant() switch
        {
            "pt" => Portuguese,
            "en" => Default,
            _ => null
        };
    }

    /// <summary>
    /// Same as <see cref="Resolve"/> but never returns <see langword="null"/>; unsupported
    /// tags fall back to <see cref="Default"/>.
    /// </summary>
    /// <param name="requested">The requested language tag.</param>
    public static string ResolveOrDefault(string? requested) => Resolve(requested) ?? Default;

    /// <summary>Whether <paramref name="name"/> is one of the shipped cultures, exactly.</summary>
    /// <param name="name">Culture name to test.</param>
    public static bool IsSupported(string? name) =>
        name is not null && Names.Contains(name, StringComparer.OrdinalIgnoreCase);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
