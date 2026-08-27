namespace HowToSoftware.Hosting.Models;

/// <summary>
/// The resources a visitor is asking for when no monthly plan is large enough.
/// </summary>
/// <param name="MemoryGb">Memory in GiB.</param>
/// <param name="CpuPercent">CPU allocation as a percentage of one logical thread.</param>
/// <param name="DiskGb">Storage in GiB.</param>
/// <param name="PlayerSlots">Players the community expects, or <see langword="null"/>.</param>
/// <param name="Notes">Anything the fields do not cover.</param>
public sealed record CustomBuildRequest(
    int MemoryGb,
    int CpuPercent,
    int DiskGb,
    int? PlayerSlots,
    string? Notes);

/// <summary>
/// What a custom build would cost, and how that figure was arrived at.
/// </summary>
/// <param name="Request">The normalised request the estimate was priced from.</param>
/// <param name="CpuCost">The CPU share of the monthly total.</param>
/// <param name="MemoryCost">The memory share of the monthly total.</param>
/// <param name="DiskCost">The storage share of the monthly total.</param>
/// <param name="Rounding">
/// What snapping the subtotal to the nearest <c>x.99</c> added or took off. Zero when charm
/// pricing is switched off.
/// </param>
/// <param name="MonthlyTotal">What the first month costs.</param>
/// <param name="RenewalTotal">
/// What every month after the first costs, or <see langword="null"/> when no renewal discount
/// is configured.
/// </param>
/// <param name="CurrencySymbol">Symbol to display the figures with.</param>
/// <param name="ComparedTo">The largest standard plan, for context.</param>
/// <remarks>
/// The breakdown is part of the result rather than something the component recomputes, so what
/// the visitor is shown adds up to what they are quoted by construction - the rounding step
/// included, which is why it is a line of the quote rather than something applied silently
/// after the lines were printed.
/// </remarks>
public sealed record CustomBuildEstimate(
    CustomBuildRequest Request,
    decimal CpuCost,
    decimal MemoryCost,
    decimal DiskCost,
    decimal Rounding,
    decimal MonthlyTotal,
    decimal? RenewalTotal,
    string CurrencySymbol,
    HostingPlan? ComparedTo)
{
    /// <summary>The three resource lines, before rounding.</summary>
    public decimal Subtotal => CpuCost + MemoryCost + DiskCost;
}

/// <summary>
/// The range the request form accepts.
/// </summary>
/// <param name="MinMemoryGb">Smallest memory a custom build starts at, in GiB.</param>
/// <param name="MaxMemoryGb">Largest memory the form will submit, in GiB.</param>
/// <param name="MemoryStepGb">Memory increment.</param>
/// <param name="MinCpuPercent">Smallest CPU allocation, as a percentage of one thread.</param>
/// <param name="MaxCpuPercent">Largest CPU allocation the form will submit.</param>
/// <param name="CpuStepPercent">CPU increment.</param>
/// <param name="MinDiskGb">Smallest storage, in GiB.</param>
/// <param name="MaxDiskGb">Largest storage the form will submit, in GiB.</param>
/// <param name="DiskStepGb">Storage increment.</param>
/// <remarks>
/// <para>
/// The minimums are read from the largest standard plan: a custom build is what you ask for when
/// the ladder runs out, so it starts where the ladder ends.
/// </para>
/// <para>
/// The maximums are <b>limits on the form</b>, not a statement about capacity. They exist so a
/// request cannot arrive asking for a petabyte, and the panel says plainly that anything beyond
/// them is a conversation rather than a slider.
/// </para>
/// </remarks>
public sealed record CustomBuildBounds(
    int MinMemoryGb,
    int MaxMemoryGb,
    int MemoryStepGb,
    int MinCpuPercent,
    int MaxCpuPercent,
    int CpuStepPercent,
    int MinDiskGb,
    int MaxDiskGb,
    int DiskStepGb)
{
    /// <summary>Clamps a request to the accepted range and snaps it to the increments.</summary>
    /// <param name="request">The request as it arrived.</param>
    /// <returns>A request that is inside the bounds.</returns>
    /// <remarks>
    /// Applied on the server, on every estimate. A number input is a suggestion to a browser,
    /// not a guarantee to a server.
    /// </remarks>
    public CustomBuildRequest Normalise(CustomBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notes = request.Notes?.Trim();

        return request with
        {
            MemoryGb = Snap(request.MemoryGb, MinMemoryGb, MaxMemoryGb, MemoryStepGb),
            CpuPercent = Snap(request.CpuPercent, MinCpuPercent, MaxCpuPercent, CpuStepPercent),
            DiskGb = Snap(request.DiskGb, MinDiskGb, MaxDiskGb, DiskStepGb),
            PlayerSlots = request.PlayerSlots is { } slots and > 0 ? Math.Min(slots, 512) : null,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Length > MaxNoteLength ? notes[..MaxNoteLength] : notes
        };
    }

    /// <summary>Longest free-text note the form carries.</summary>
    public const int MaxNoteLength = 600;

    private static int Snap(int value, int min, int max, int step)
    {
        var clamped = Math.Clamp(value, min, max);
        var offset = clamped - min;
        var snapped = min + ((offset + (step / 2)) / step * step);

        return Math.Clamp(snapped, min, max);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
