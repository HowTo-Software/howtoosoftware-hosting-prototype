namespace HowToSoftware.Hosting.Services.Trials;

/// <summary>No database means no entitlement claim; ordinary paid orders keep their existing flow.</summary>
public sealed class UnavailableTrialService : ITrialService
{
    public bool IsAvailable => false;
    public IReadOnlyList<TrialGameProfile> AvailableProfiles => [];
    public Task<TrialRequestResult> RequestAsync(TrialSelectionRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TrialRequestResult(TrialRequestOutcome.Unavailable));
    public Task<TrialRequestResult> RecoverAccessAsync(string email, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TrialRequestResult(TrialRequestOutcome.Unavailable));
    public Task<TrialConfirmationResult> ConfirmAsync(string verificationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TrialConfirmationResult(TrialConfirmationOutcome.Unavailable));
    public Task<TrialAccountView?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
        Task.FromResult<TrialAccountView?>(null);
    public Task<TrialUpgradeReservation> ReserveUpgradeAsync(Guid trialId, string accessToken, Guid orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TrialUpgradeReservation(false));
    public Task ReleaseUpgradeAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<TrialConversionResult> ConvertPaidOrderAsync(Guid orderId, TrialResourceLimits limits, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TrialConversionResult(TrialConversionOutcome.NotLinked));
    public Task ProcessDueAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
