namespace HowToSoftware.Hosting.Models.Commerce;

/// <summary>Permanent order-to-trial association; an expired reservation never becomes a fresh-server order.</summary>
public sealed class TrialUpgradeOrderRecord
{
    public Guid OrderId { get; set; }
    public Guid TrialId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
