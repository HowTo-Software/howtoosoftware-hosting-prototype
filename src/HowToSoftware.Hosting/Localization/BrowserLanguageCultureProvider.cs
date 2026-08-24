using Microsoft.AspNetCore.Localization;
using Microsoft.Net.Http.Headers;

namespace HowToSoftware.Hosting.Localization;

/// <summary>
/// Picks the site language from the browser's <c>Accept-Language</c> header on a visitor's
/// first visit.
/// </summary>
/// <remarks>
/// <para>
/// The framework's own <see cref="AcceptLanguageHeaderRequestCultureProvider"/> only matches
/// tags it is configured with, so a browser asking for plain <c>pt</c> - or for <c>pt-PT</c> -
/// would miss <c>pt-BR</c> and land on English. This provider resolves the header through
/// <see cref="SupportedCultures"/> instead, which treats any Portuguese variant as Brazilian
/// Portuguese.
/// </para>
/// <para>
/// It deliberately runs <em>after</em> the cookie provider: browser preference is only the
/// first-visit default, and an explicit choice by the visitor always wins.
/// </para>
/// </remarks>
public sealed class BrowserLanguageCultureProvider : RequestCultureProvider
{
    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var match = Match(httpContext.Request.Headers.AcceptLanguage);

        return Task.FromResult(match is null ? null : new ProviderCultureResult(match));
    }

    /// <summary>
    /// Resolves the best supported culture from raw <c>Accept-Language</c> header values.
    /// </summary>
    /// <param name="headerValues">
    /// The header as sent, e.g. <c>pt-BR,pt;q=0.9,en-US;q=0.8</c>. Multiple header lines are
    /// treated as one list.
    /// </param>
    /// <returns>The supported culture name, or <see langword="null"/> when none matches.</returns>
    internal static string? Match(IEnumerable<string?>? headerValues)
    {
        if (headerValues is null)
        {
            return null;
        }

        var inputs = headerValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        if (inputs.Length == 0 ||
            !StringWithQualityHeaderValue.TryParseList(inputs, out var languages) ||
            languages.Count == 0)
        {
            return null;
        }

        // A missing q is q=1. OrderByDescending is stable, so equally-weighted tags keep the
        // order the browser listed them in - which is the order the visitor prefers them.
        foreach (var language in languages.OrderByDescending(language => language.Quality ?? 1d))
        {
            if (language.Quality is 0)
            {
                continue;
            }

            var tag = language.Value.Value;

            if (string.IsNullOrEmpty(tag))
            {
                continue;
            }

            // "*" means "anything will do", which is the default culture.
            if (tag == "*")
            {
                return SupportedCultures.Default;
            }

            if (SupportedCultures.Resolve(tag) is { } resolved)
            {
                return resolved;
            }
        }

        return null;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
