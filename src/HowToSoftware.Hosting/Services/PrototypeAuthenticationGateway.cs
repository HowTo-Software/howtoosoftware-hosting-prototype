using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The placeholder that stands in for a real identity provider until one exists.
/// </summary>
/// <remarks>
/// <para>
/// It authenticates nobody. There is no user list, no demo account, no password comparison and
/// no storage of any kind - the request object is not even read. Every attempt returns
/// <see cref="SignInOutcome.NotAvailable"/>, and the screen says so in as many words.
/// </para>
/// <para>
/// That is deliberate. A prototype that accepts <c>admin/admin</c> teaches everyone who tries
/// it that the login works, and a stub that hashes and stores a password is a credential store
/// nobody reviewed. Doing nothing is the only honest behaviour until the real provider lands.
/// </para>
/// <para>
/// The short delay exists so the form's loading state can be reviewed; it is not a check.
/// </para>
/// </remarks>
public sealed class PrototypeAuthenticationGateway : IAuthenticationGateway
{
    /// <summary>How long the screen pretends to be talking to a provider.</summary>
    internal static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(650);

    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the gateway.</summary>
    /// <param name="timeProvider">Clock used for the simulated round trip.</param>
    public PrototypeAuthenticationGateway(TimeProvider timeProvider) => _timeProvider = timeProvider;

    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public async ValueTask<SignInResult> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Nothing above this line reads request.Password, and nothing below it will.
        await Task.Delay(SimulatedLatency, _timeProvider, cancellationToken).ConfigureAwait(false);

        return new SignInResult(SignInOutcome.NotAvailable);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
