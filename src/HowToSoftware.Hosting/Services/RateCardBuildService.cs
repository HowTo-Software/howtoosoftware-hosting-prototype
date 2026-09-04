using HowToSoftware.Hosting.Models;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Prices a custom build from the same rate card the standard plans are priced from.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole point of holding prices as a rate card rather than as eight numbers. A
/// custom build is not a special case that needs its own pricing policy; it is the same three
/// rates applied to different figures, so a quote and a plan can never tell different stories
/// about what a gigabyte costs.
/// </para>
/// <para>
/// Nothing here submits anything. The estimate is arithmetic the visitor can check, and the
/// panel hands it to them to send. Building a request pipeline that silently dropped enquiries
/// would be worse than not having one.
/// </para>
/// </remarks>
public sealed class RateCardBuildService : ICustomBuildService
{
    private readonly IPlanCatalogService _catalog;
    private readonly HostingPlanPricingOptions _pricing;

    /// <summary>Creates the service.</summary>
    /// <param name="catalog">Used to find where the standard ladder ends.</param>
    /// <param name="pricing">The configured rate card and request bounds.</param>
    public RateCardBuildService(
        IPlanCatalogService catalog,
        IOptions<HostingPlanPricingOptions> pricing)
    {
        _catalog = catalog;
        _pricing = pricing.Value;
    }

    /// <inheritdoc />
    public bool CanEstimate => _pricing.Rates.IsConfigured;

    /// <inheritdoc />
    public CustomBuildBounds Bounds
    {
        get
        {
            var limits = _pricing.CustomBuild;
            var largest = Largest;

            // A custom build starts where the ladder ends. Reading that from the catalogue
            // rather than restating it means adding a tier moves this floor with it.
            var minMemory = largest?.MemoryGb ?? 8;
            var minCpu = largest?.CpuPercent ?? 500;
            var minDisk = largest?.DiskGb ?? 40;

            return new CustomBuildBounds(
                MinMemoryGb: minMemory,
                MaxMemoryGb: Math.Max(limits.MaxMemoryGb, minMemory + limits.MemoryStepGb),
                MemoryStepGb: limits.MemoryStepGb,
                MinCpuPercent: minCpu,
                MaxCpuPercent: Math.Max(limits.MaxCpuPercent, minCpu + limits.CpuStepPercent),
                CpuStepPercent: limits.CpuStepPercent,
                MinDiskGb: minDisk,
                MaxDiskGb: Math.Max(limits.MaxDiskGb, minDisk + limits.DiskStepGb),
                DiskStepGb: limits.DiskStepGb);
        }
    }

    /// <inheritdoc />
    public CustomBuildRequest CreateDefault()
    {
        var bounds = Bounds;

        // One step above the largest plan: the form opens on the smallest thing that is not
        // already for sale, which is the question the panel exists to answer.
        return new CustomBuildRequest(
            MemoryGb: Math.Min(bounds.MinMemoryGb + bounds.MemoryStepGb, bounds.MaxMemoryGb),
            CpuPercent: Math.Min(bounds.MinCpuPercent + bounds.CpuStepPercent, bounds.MaxCpuPercent),
            DiskGb: bounds.MinDiskGb,
            PlayerSlots: null,
            Notes: null);
    }

    /// <inheritdoc />
    public CustomBuildEstimate? Estimate(CustomBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanEstimate)
        {
            return null;
        }

        var normalised = Bounds.Normalise(request);
        var rates = _pricing.Rates;

        // Each line is priced on its own so the breakdown the visitor reads is the arithmetic
        // that produced the total, not a second calculation that could disagree with it.
        var cpu = rates.Compute(normalised.CpuPercent, 0, 0) ?? 0m;
        var memory = rates.Compute(0, normalised.MemoryGb, 0) ?? 0m;
        var disk = rates.Compute(0, 0, normalised.DiskGb) ?? 0m;

        // The total is the three lines added up, not a fourth calculation. A breakdown that
        // does not sum to the figure above it is the kind of detail a reader checks once and
        // then stops trusting the rest of the page.
        var subtotal = cpu + memory + disk;

        if (subtotal <= 0m)
        {
            return null;
        }

        // Charm pricing moves the figure, so it is a line of the quote too - printed, signed,
        // and summing with the rest. A total that quietly disagreed with the lines above it
        // would undo the reason the lines are there.
        var monthly = _pricing.CharmPricing
            ? HostingPlanPricingOptions.ToCharmPrice(subtotal)
            : subtotal;

        return new CustomBuildEstimate(
            normalised,
            cpu,
            memory,
            disk,
            monthly - subtotal,
            monthly,
            _pricing.CurrencySymbol,
            Largest);
    }

    /// <summary>The top of the standard ladder, or null on an empty catalogue.</summary>
    private HostingPlan? Largest =>
        _catalog.Plans.Count == 0 ? null : _catalog.Plans[^1];
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
