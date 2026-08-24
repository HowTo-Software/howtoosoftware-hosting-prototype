using Microsoft.AspNetCore.Localization;

namespace HowToSoftware.Hosting.Localization;

/// <summary>
/// The endpoint behind the header's language switcher.
/// </summary>
/// <remarks>
/// Switching language is a plain link to this endpoint rather than a scripted control: it
/// writes the culture cookie and redirects back to the page the visitor was on, so the whole
/// response - including anything rendered statically on the server - comes back in the new
/// language. It works without JavaScript and keeps the choice for the next visit.
/// </remarks>
public static class CultureEndpoints
{
    /// <summary>Route the switcher points at.</summary>
    public const string Pattern = "/culture/select";

    /// <summary>Query-string key carrying the requested culture.</summary>
    public const string CultureKey = "culture";

    /// <summary>Query-string key carrying the page to return to.</summary>
    public const string RedirectKey = "redirect";

    /// <summary>Maps the language-selection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static IEndpointRouteBuilder MapCultureSelection(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(Pattern, (HttpContext http, string? culture, string? redirect) =>
        {
            var selected = SupportedCultures.ResolveOrDefault(culture);

            http.Response.Cookies.Append(
                SiteLocalization.CultureCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
                SiteLocalization.CreateCultureCookieOptions(http.Request.IsHttps));

            return Results.Redirect(SafeRedirect(redirect));
        })
        .ExcludeFromDescription();

        return endpoints;
    }

    /// <summary>
    /// Reduces a caller-supplied return path to something that can only point back into this
    /// site.
    /// </summary>
    /// <param name="redirect">The requested return path.</param>
    /// <returns>
    /// The path when it is a site-relative URL, otherwise <c>/</c>. Protocol-relative
    /// (<c>//host</c>) and backslash forms are rejected because browsers resolve them to
    /// another origin, which would turn the switcher into an open redirect.
    /// </returns>
    internal static string SafeRedirect(string? redirect)
    {
        if (string.IsNullOrWhiteSpace(redirect))
        {
            return "/";
        }

        var candidate = redirect.Trim();

        if (candidate[0] is not '/')
        {
            return "/";
        }

        // "//host" and "/\host" both resolve to another origin in a browser, so
        // both have to go. 92 is the backslash.
        if (candidate.Length > 1 && candidate[1] is '/' or (char)92)
        {
            return "/";
        }

        // A control character can be used to smuggle a second header or hide the real target.
        return candidate.Any(char.IsControl) ? "/" : candidate;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
