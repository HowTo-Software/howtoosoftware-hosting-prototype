using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Services;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The sign-in form's shape checks.
/// </summary>
public class SignInValidatorTests
{
    [Theory]
    [InlineData("survivor@example.com")]
    [InlineData("first.last@example.co.uk")]
    [InlineData("knox+county@example.com")]
    [InlineData("  survivor@example.com  ")]
    public void ARealAddressIsAccepted(string email)
    {
        Assert.Equal(SignInFieldError.None, SignInValidator.ValidateEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AMissingAddressIsReportedAsMissing_NotAsMalformed(string? email)
    {
        Assert.Equal(SignInFieldError.EmailRequired, SignInValidator.ValidateEmail(email));
    }

    [Theory]
    [InlineData("survivor")]
    [InlineData("survivor@")]
    [InlineData("@example.com")]
    [InlineData("survivor@localhost")]
    [InlineData("survivor@example.")]
    [InlineData("survivor example@test.com")]
    public void AMalformedAddressIsRejected(string email)
    {
        Assert.Equal(SignInFieldError.EmailInvalid, SignInValidator.ValidateEmail(email));
    }

    [Fact]
    public void AMissingPasswordIsReported()
    {
        Assert.Equal(SignInFieldError.PasswordRequired, SignInValidator.ValidatePassword(null));
        Assert.Equal(SignInFieldError.PasswordRequired, SignInValidator.ValidatePassword(string.Empty));
    }

    /// <summary>
    /// A sign-in form must not enforce composition rules: the password was already accepted
    /// when the account was made, and re-checking it here only publishes the rules.
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("   ")]
    [InlineData("correct horse battery staple")]
    public void AnyNonEmptyPasswordIsAccepted(string password)
    {
        Assert.Equal(SignInFieldError.None, SignInValidator.ValidatePassword(password));
    }

    [Fact]
    public void AFormIsOnlyValidWhenBothFieldsAre()
    {
        Assert.True(SignInValidator.Validate("survivor@example.com", "hunter2").IsValid);
        Assert.False(SignInValidator.Validate("survivor@example.com", "").IsValid);
        Assert.False(SignInValidator.Validate("nope", "hunter2").IsValid);
        Assert.False(SignInValidator.Validate(null, null).IsValid);
    }

    [Fact]
    public void ACleanValidationIsValid()
    {
        Assert.True(SignInValidation.Clean.IsValid);
    }
}

/// <summary>
/// The stand-in that sits where a real identity provider will go.
/// </summary>
/// <remarks>
/// These tests exist to keep it a stand-in. A prototype that quietly starts accepting a demo
/// credential teaches everyone who tries it that sign-in works.
/// </remarks>
public class PrototypeAuthenticationGatewayTests
{
    private readonly TestTimeProvider _clock = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    [Fact]
    public void ItReportsThatNoProviderIsConnected()
    {
        Assert.False(new PrototypeAuthenticationGateway(_clock).IsConfigured);
    }

    [Theory]
    [InlineData("survivor@example.com", "hunter2")]
    [InlineData("admin@example.com", "admin")]
    [InlineData("admin", "admin")]
    [InlineData("", "")]
    public async Task NoCredentialIsEverAccepted(string email, string password)
    {
        var result = await SignInAsync(email, password);

        Assert.Equal(SignInOutcome.NotAvailable, result.Outcome);
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// "Not available" and "wrong password" have to stay distinguishable, or the screen ends up
    /// telling visitors they typed something wrong when the feature simply does not exist.
    /// </summary>
    [Fact]
    public async Task TheOutcomeIsNotAFailedLogin()
    {
        var result = await SignInAsync("survivor@example.com", "hunter2");

        Assert.NotEqual(SignInOutcome.InvalidCredentials, result.Outcome);
        Assert.NotEqual(SignInOutcome.Locked, result.Outcome);
    }

    [Fact]
    public async Task NothingIsHandedBackThatCouldCarryACredential()
    {
        var result = await SignInAsync("survivor@example.com", "hunter2");

        Assert.Null(result.RedirectTo);
    }

    [Fact]
    public async Task ARequestWithoutAnEmailIsRejectedByTheContract()
    {
        var gateway = new PrototypeAuthenticationGateway(_clock);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await gateway.SignInAsync(null!));
    }

    private async Task<SignInResult> SignInAsync(string email, string password)
    {
        var gateway = new PrototypeAuthenticationGateway(_clock);

        var attempt = gateway.SignInAsync(new SignInRequest
        {
            Email = email,
            Password = password
        });

        // The simulated round trip only exists so the busy state can be reviewed; move the
        // clock rather than waiting for it.
        _clock.Advance(PrototypeAuthenticationGateway.SimulatedLatency);

        return await attempt;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
