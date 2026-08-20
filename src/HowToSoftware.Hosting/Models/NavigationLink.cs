namespace HowToSoftware.Hosting.Models;

/// <summary>
/// A single entry in the primary or footer navigation.
/// </summary>
/// <param name="Label">Human-readable link text.</param>
/// <param name="Href">Target URL or in-page anchor.</param>
public sealed record NavigationLink(string Label, string Href);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
