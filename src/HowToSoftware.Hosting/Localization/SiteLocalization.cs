using Microsoft.AspNetCore.Localization;

namespace HowToSoftware.Hosting.Localization;

/// <summary>
/// Builds the site's request-localisation policy in one place, so the ordering that decides a
/// visitor's language is a single readable statement rather than something spread across
/// <c>Program.cs</c>.
/// </summary>
public static class SiteLocalization
{
    /// <summary>Cookie the explicit language choice is persisted in.</summary>
    public static string CultureCookieName => CookieRequestCultureProvider.DefaultCookieName;

    /// <summary>How long an explicit language choice is remembered for.</summary>
    public static readonly TimeSpan CultureCookieLifetime = TimeSpan.FromDays(365);

    /// <summary>
    /// Applies the site's language policy to <paramref name="options"/>: English default,
    /// Brazilian Portuguese alongside it, and a two-step decision - the visitor's own choice
    /// first, the browser's preference second, English if neither says anything usable.
    /// </summary>
    /// <param name="options">The options instance the middleware will read.</param>
    public static void Configure(RequestLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SetDefaultCulture(SupportedCultures.Default)
            .AddSupportedCultures([.. SupportedCultures.Names])
            .AddSupportedUICultures([.. SupportedCultures.Names]);

        options.FallBackToParentCultures = true;
        options.FallBackToParentUICultures = true;
        options.ApplyCurrentCultureToResponseHeaders = true;

        // Order is the whole policy:
        //   1. the cookie written by the header's language switcher - an explicit choice, and
        //      it must survive every later navigation;
        //   2. the browser's Accept-Language - only ever the first-visit default.
        // The query-string provider is deliberately absent: language is not a URL concern here.
        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new CookieRequestCultureProvider { Options = options });
        options.RequestCultureProviders.Add(new BrowserLanguageCultureProvider { Options = options });
    }

    /// <summary>
    /// Builds a fully configured options instance. Used by tests; the application configures
    /// the container's instance through <see cref="Configure"/> instead.
    /// </summary>
    public static RequestLocalizationOptions Create()
    {
        var options = new RequestLocalizationOptions();
        Configure(options);
        return options;
    }

    /// <summary>Cookie settings used when persisting an explicit language choice.</summary>
    /// <param name="isHttps">Whether the current request arrived over HTTPS.</param>
    public static CookieOptions CreateCultureCookieOptions(bool isHttps) => new()
    {
        Path = "/",
        Expires = DateTimeOffset.UtcNow.Add(CultureCookieLifetime),
        // A language preference is needed for the site to work correctly, so it is exempt from
        // consent gating; it holds no personal data.
        IsEssential = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = isHttps
    };
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
