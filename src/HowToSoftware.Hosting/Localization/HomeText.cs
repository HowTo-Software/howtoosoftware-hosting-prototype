namespace HowToSoftware.Hosting.Localization;

/// <summary>
/// Marker type for the homepage resource set (<c>Localization/HomeText.resx</c>).
/// </summary>
/// <remarks>
/// The homepage carries most of the site's prose, so it gets its own resource set rather than
/// swelling <see cref="CommonText"/>. It also holds the marketing content behind
/// <c>IMarketingContentService</c> and <c>IPlanCatalogService</c> - plan taglines, FAQ answers,
/// provisioning stages - because those are copy, not structure.
/// </remarks>
public sealed class HomeText;

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
