using HowToSoftware.Hosting.Services.Trials;

namespace HowToSoftware.Hosting.Tests;

internal sealed class CheckoutTrialStub : ITrialService
{
    public TrialAccountView? View { get; set; } = new(Guid.NewGuid(), "owner@example.com", "Owner", "project-zomboid",
        null, null, TrialState.Active, 77, "trial77", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(24),
        DateTimeOffset.UtcNow.AddDays(4), true, null);
    public bool ReserveAllowed { get; set; } = true;
    public bool ThrowOnReservation { get; set; }
    public List<Guid> Released { get; } = [];
    public List<Guid> Reserved { get; } = [];
    public TrialConversionResult Conversion { get; set; } = new(TrialConversionOutcome.Converted,
        new TrialPanelServer(77, "trial77", "uuid77", true, false));
    public TrialResourceLimits? PaidLimits { get; private set; }
    public bool IsAvailable => true;
    public IReadOnlyList<TrialGameProfile> AvailableProfiles => [];
    public Task<TrialRequestResult> RequestAsync(TrialSelectionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<TrialConfirmationResult> ConfirmAsync(string token, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<TrialAccountView?> GetByAccessTokenAsync(string token, CancellationToken cancellationToken = default) => Task.FromResult(View);
    public Task<TrialUpgradeReservation> ReserveUpgradeAsync(Guid trialId, string token, Guid orderId, CancellationToken cancellationToken = default)
    {
        Reserved.Add(orderId);
        if (ThrowOnReservation) throw new InvalidOperationException("Reservation store unavailable");
        return Task.FromResult(new TrialUpgradeReservation(ReserveAllowed, trialId));
    }
    public Task ReleaseUpgradeAsync(Guid orderId, CancellationToken cancellationToken = default)
    { Released.Add(orderId); return Task.CompletedTask; }
    public Task<TrialConversionResult> ConvertPaidOrderAsync(Guid orderId, TrialResourceLimits limits, CancellationToken cancellationToken = default)
    { PaidLimits = limits; return Task.FromResult(Conversion); }
    public Task ProcessDueAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
