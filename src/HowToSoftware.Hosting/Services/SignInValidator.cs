using System.Net.Mail;
using HowToSoftware.Hosting.Models;

namespace HowToSoftware.Hosting.Services;

/// <summary>
/// Client-side shape checks for the sign-in form.
/// </summary>
/// <remarks>
/// This decides whether a form is worth submitting, nothing more. It never decides whether an
/// account exists - that is the identity provider's job, and telling the two apart in the UI
/// is how account enumeration happens.
/// </remarks>
public static class SignInValidator
{
    /// <summary>Validates the two fields and returns a code per field.</summary>
    /// <param name="email">Email as typed.</param>
    /// <param name="password">Password as typed.</param>
    public static SignInValidation Validate(string? email, string? password) =>
        new(ValidateEmail(email), ValidatePassword(password));

    /// <summary>Checks the email field.</summary>
    /// <param name="email">Email as typed.</param>
    public static SignInFieldError ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return SignInFieldError.EmailRequired;
        }

        var candidate = email.Trim();

        // Deliberately permissive, and close to what the browser's own type="email" check
        // does: one address, an "@", and a host that at least looks routable. Anything
        // stricter starts rejecting addresses that really exist.
        if (!MailAddress.TryCreate(candidate, out var address) ||
            !address.Address.Equals(candidate, StringComparison.Ordinal) ||
            !address.Host.Contains('.', StringComparison.Ordinal) ||
            address.Host.StartsWith('.') ||
            address.Host.EndsWith('.'))
        {
            return SignInFieldError.EmailInvalid;
        }

        return SignInFieldError.None;
    }

    /// <summary>
    /// Checks the password field.
    /// </summary>
    /// <param name="password">Password as typed.</param>
    /// <remarks>
    /// Presence only. A sign-in form must not enforce composition rules: the password was
    /// already accepted when the account was created, and re-checking it here only tells an
    /// attacker what the rules are.
    /// </remarks>
    public static SignInFieldError ValidatePassword(string? password) =>
        string.IsNullOrEmpty(password) ? SignInFieldError.PasswordRequired : SignInFieldError.None;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
