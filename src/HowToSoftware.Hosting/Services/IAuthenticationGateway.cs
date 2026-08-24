using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// The seam a real identity provider will be plugged into.
/// </summary>
/// <remarks>
/// <para>
/// The contract is asynchronous and returns an outcome rather than a message, matching how an
/// identity API behaves. Replacing <c>PrototypeAuthenticationGateway</c> in <c>Program.cs</c>
/// with an implementation backed by ASP.NET Core Identity, an OIDC provider or the customer
/// database is the only change the sign-in screen needs.
/// </para>
/// <para>
/// There is no account creation or password reset on this interface yet, on purpose: adding
/// them before the storage and email decisions are made would only lock in guesses.
/// </para>
/// </remarks>
public interface IAuthenticationGateway
{
    /// <summary>
    /// Whether a real identity provider is connected. The sign-in screen uses this to explain
    /// itself up front instead of letting a visitor submit into a void.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>Attempts to sign a visitor in.</summary>
    /// <param name="request">Credentials from the form.</param>
    /// <param name="cancellationToken">Token used to cancel the attempt.</param>
    ValueTask<SignInResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
