namespace HowToSoftware.Hosting.Models;

/// <summary>
/// Credentials submitted by the sign-in form.
/// </summary>
/// <remarks>
/// The shape matches what a real identity provider would need, so the prototype gateway can be
/// swapped for one without changing the form. The prototype implementation never reads
/// <see cref="Password"/>: nothing is compared, hashed, logged or stored.
/// </remarks>
public sealed record SignInRequest
{
    /// <summary>Email address as typed.</summary>
    public required string Email { get; init; }

    /// <summary>Password as typed. Never persisted by this prototype.</summary>
    public required string Password { get; init; }

    /// <summary>Whether the visitor asked to stay signed in on this browser.</summary>
    public bool RememberMe { get; init; }
}

/// <summary>
/// How a sign-in attempt ended.
/// </summary>
public enum SignInOutcome
{
    /// <summary>Credentials accepted and a session was established.</summary>
    Succeeded,

    /// <summary>The identity provider rejected the credentials.</summary>
    InvalidCredentials,

    /// <summary>The account exists but is temporarily blocked from signing in.</summary>
    Locked,

    /// <summary>
    /// No identity provider is connected. This is the only outcome the prototype can return,
    /// and the form states it plainly rather than pretending a login failed.
    /// </summary>
    NotAvailable
}

/// <summary>
/// Result of a sign-in attempt.
/// </summary>
/// <param name="Outcome">How the attempt ended.</param>
/// <param name="RedirectTo">Where to send the visitor when <see cref="SignInOutcome.Succeeded"/>.</param>
/// <remarks>
/// There is no message on purpose. The outcome is the contract; the wording that goes with it
/// is a resource string chosen by the component, so it can be translated without a service
/// knowing which language a visitor reads.
/// </remarks>
public sealed record SignInResult(SignInOutcome Outcome, string? RedirectTo = null)
{
    /// <summary>Whether the visitor is now signed in.</summary>
    public bool IsSuccess => Outcome is SignInOutcome.Succeeded;
}

/// <summary>
/// What is wrong with a single sign-in field.
/// </summary>
/// <remarks>
/// A code rather than a message, so validation logic stays testable and language-free and the
/// component renders the matching localised string.
/// </remarks>
public enum SignInFieldError
{
    /// <summary>The field is fine.</summary>
    None,

    /// <summary>No email address was entered.</summary>
    EmailRequired,

    /// <summary>What was entered is not a usable email address.</summary>
    EmailInvalid,

    /// <summary>No password was entered.</summary>
    PasswordRequired
}

/// <summary>
/// The per-field outcome of validating the sign-in form.
/// </summary>
/// <param name="Email">Problem with the email field, if any.</param>
/// <param name="Password">Problem with the password field, if any.</param>
public sealed record SignInValidation(SignInFieldError Email, SignInFieldError Password)
{
    /// <summary>Nothing has been checked yet.</summary>
    public static readonly SignInValidation Clean = new(SignInFieldError.None, SignInFieldError.None);

    /// <summary>Whether the form can be submitted.</summary>
    public bool IsValid => Email is SignInFieldError.None && Password is SignInFieldError.None;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
