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

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
