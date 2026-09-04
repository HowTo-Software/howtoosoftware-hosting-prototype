using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Site-wide values bound from the <c>Site</c> configuration section. Keeping these in
/// configuration avoids hard-coding brand strings and URLs inside components.
/// </summary>
public sealed class SiteOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Site";

    /// <summary>Company / product name used in titles and structured data.</summary>
    public string Name { get; set; } = "HowToSoftware";

    /// <summary>Absolute base URL used to build canonical and Open Graph URLs.</summary>
    public string BaseUrl { get; set; } = "https://howtoosoftware.com";

    /// <summary>Contact mailbox surfaced in the footer and contact call-to-action.</summary>
    public string ContactEmail { get; set; } = "hello@howtoosoftware.com";
}

/// <summary>Rejects unsafe public origins before they can be used in metadata or redirects.</summary>
public sealed class SiteOptionsValidator : IValidateOptions<SiteOptions>
{
    private readonly IWebHostEnvironment _environment;

    public SiteOptionsValidator(IWebHostEnvironment environment) => _environment = environment;

    public ValidateOptionsResult Validate(string? name, SiteOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name) || options.Name.Length > 80)
        {
            failures.Add("Site:Name must contain between 1 and 80 characters.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0)
        {
            failures.Add("Site:BaseUrl must be an absolute HTTP(S) origin without credentials, query or fragment.");
        }
        else if (!_environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("Site:BaseUrl must use HTTPS outside Development.");
        }

        if (!MailAddress.TryCreate(options.ContactEmail, out _))
        {
            failures.Add("Site:ContactEmail must be a valid email address.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
