using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Prices a build that no standard plan covers.
/// </summary>
public interface ICustomBuildService
{
    /// <summary>The range the request form accepts.</summary>
    CustomBuildBounds Bounds { get; }

    /// <summary>Whether a price can be produced at all.</summary>
    /// <remarks>
    /// False when no rate card is configured. The panel then asks for the specification and
    /// leaves the figure out, rather than showing a total it cannot stand behind.
    /// </remarks>
    bool CanEstimate { get; }

    /// <summary>The percentage taken off every month after the first.</summary>
    decimal RenewalDiscountPercent { get; }

    /// <summary>A request with the bounds' starting values already filled in.</summary>
    CustomBuildRequest CreateDefault();

    /// <summary>
    /// Normalises a request and prices it.
    /// </summary>
    /// <param name="request">What the visitor asked for.</param>
    /// <returns>
    /// The estimate, or <see langword="null"/> when <see cref="CanEstimate"/> is false.
    /// </returns>
    CustomBuildEstimate? Estimate(CustomBuildRequest request);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
