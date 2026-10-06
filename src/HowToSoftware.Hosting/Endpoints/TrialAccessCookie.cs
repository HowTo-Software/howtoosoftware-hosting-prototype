namespace HowToSoftware.Hosting.Endpoints;

/// <summary>
/// Holds an opaque, verified-owner capability issued by the trial service. This is not a
/// general application login, and neither an email address nor a browser-supplied customer id
/// is accepted as proof of ownership.
/// </summary>
public static class TrialAccessCookie
{
    public const string Name = "hts.trial-access";

    public static string? Read(HttpContext? context)
    {
        var token = context?.Request.Cookies[Name];
        return token is { Length: > 0 and <= 4096 } ? token : null;
    }

    public static void Issue(HttpContext context, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        context.Response.Cookies.Append(Name, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            // Installation precedes the 24-hour timer. Cookie lifetime is not an entitlement:
            // the durable trial's expiry, retention and state remain authoritative.
            MaxAge = TimeSpan.FromDays(7)
        });
    }
}
