namespace HowToSoftware.Hosting.Components.Account;

/// <summary>
/// What the sign-in gateway diagram is currently depicting.
/// </summary>
/// <remarks>
/// Set from the sign-in form's Blazor state, so the illustration is a readout of the real
/// interaction rather than a loop that animates regardless of what the visitor does.
/// </remarks>
public enum GatewayPhase
{
    /// <summary>Waiting for credentials.</summary>
    Idle,

    /// <summary>A request is in flight.</summary>
    Verifying,

    /// <summary>The request stopped at the identity hop.</summary>
    Halted
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
