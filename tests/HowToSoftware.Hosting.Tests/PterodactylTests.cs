using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Services.Provisioning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The identifiers written into the panel, and the guard that decides what may be deleted.
/// </summary>
public class PterodactylNamingTests
{
    [Fact]
    public void ExternalIdsCarryTheirPrefixAndTheGuid()
    {
        var id = Guid.Parse("6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34");

        Assert.Equal("hts-customer:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34",
            PterodactylNaming.CustomerExternalId(id));
        Assert.Equal("hts-test-customer:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34",
            PterodactylNaming.TestCustomerExternalId(id));
        Assert.Equal("hts-test-server:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34",
            PterodactylNaming.TestServerExternalId(id));
        Assert.Equal("hts-server:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34",
            PterodactylNaming.ServerExternalId(id));
    }

    /// <summary>
    /// The panel routes <c>/servers/external/{id}</c> on a segment that cannot contain a slash,
    /// and stores the column as varchar(191).
    /// </summary>
    [Fact]
    public void ExternalIdsAreRoutableAndFitTheColumn()
    {
        var ids = new[]
        {
            PterodactylNaming.CustomerExternalId(Guid.NewGuid()),
            PterodactylNaming.TestCustomerExternalId(Guid.NewGuid()),
            PterodactylNaming.TestServerExternalId(Guid.NewGuid()),
            PterodactylNaming.ServerExternalId(Guid.NewGuid())
        };

        Assert.All(ids, id =>
        {
            Assert.DoesNotContain('/', id);
            Assert.True(id.Length <= PterodactylNaming.MaxExternalIdLength);
        });
    }

    // ── The deletion guard ────────────────────────────────────────────────

    [Fact]
    public void OnlyALabServerIsDeletable()
    {
        Assert.True(PterodactylNaming.IsTestServer(PterodactylNaming.TestServerExternalId(Guid.NewGuid())));
    }

    [Theory]
    // A real customer's server. The whole point of the guard.
    [InlineData("hts-server:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34")]
    // A user, not a server.
    [InlineData("hts-test-customer:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34")]
    // Someone else's billing system.
    [InlineData("billing-12345")]
    // Case games: the guard is ordinal, because every id we write is lower-case.
    [InlineData("HTS-TEST-SERVER:6f4c1b2e-9a3d-4f51-8b7c-2d0e5a1f9c34")]
    [InlineData("Hts-Test-Server:abc")]
    // Prefix smuggling.
    [InlineData("x-hts-test-server:abc")]
    [InlineData(" hts-test-server:abc")]
    [InlineData("")]
    [InlineData(null)]
    public void EverythingElseIsRefused(string? externalId)
    {
        Assert.False(PterodactylNaming.IsTestServer(externalId));
    }

    [Fact]
    public void TheTestCustomerGuardIsJustAsStrict()
    {
        Assert.True(PterodactylNaming.IsTestCustomer("hts-test-customer:abc"));
        Assert.False(PterodactylNaming.IsTestCustomer("hts-customer:abc"));
        Assert.False(PterodactylNaming.IsTestCustomer(null));
    }

    // ── Usernames ─────────────────────────────────────────────────────────

    /// <summary>
    /// The panel applies <c>/^[a-z0-9]([\w.-]+)[a-z0-9]$/</c> to the lower-cased value, which
    /// also forces a three-character minimum.
    /// </summary>
    [Theory]
    [InlineData("survivor")]
    [InlineData("first.last")]
    [InlineData("knox_county")]
    [InlineData("a")]
    [InlineData("jo")]
    [InlineData("...")]
    [InlineData("--")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Ana Beatriz")]
    [InlineData("josé.álvares")]
    [InlineData("a-very-long-local-part-that-goes-well-past-what-anyone-needs")]
    public void EveryGeneratedUsernameSatisfiesThePanelRule(string? seed)
    {
        var username = PterodactylNaming.BuildUsername(seed);

        Assert.Matches("^[a-z0-9]([a-z0-9_.-]+)[a-z0-9]$", username.ToLowerInvariant());
        Assert.True(username.Length >= 3, $"'{username}' is shorter than the panel's minimum");
        Assert.True(username.Length <= 191);
    }

    [Fact]
    public void UsernamesDoNotCollideForTheSameSeed()
    {
        var generated = Enumerable.Range(0, 50)
            .Select(_ => PterodactylNaming.BuildUsername("survivor"))
            .ToArray();

        Assert.True(generated.Distinct(StringComparer.Ordinal).Count() > 45,
            "the disambiguating suffix is not doing its job");
    }

    // ── Passwords ─────────────────────────────────────────────────────────

    /// <summary>
    /// The Application API enforces nothing on passwords - the rule is literally
    /// <c>sometimes|nullable|string</c> - so all of the strength has to come from here.
    /// </summary>
    [Fact]
    public void GeneratedPasswordsAreLongAndUnpredictable()
    {
        var passwords = Enumerable.Range(0, 25)
            .Select(_ => PterodactylNaming.GeneratePassword())
            .ToArray();

        Assert.All(passwords, password => Assert.True(password.Length >= 32));
        Assert.Equal(passwords.Length, passwords.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GeneratedPasswordsCarryEveryCharacterClass()
    {
        var password = PterodactylNaming.GeneratePassword();

        Assert.Contains(password, char.IsAsciiLetterUpper);
        Assert.Contains(password, char.IsAsciiLetterLower);
        Assert.Contains(password, char.IsAsciiDigit);
        Assert.Contains(password, character => !char.IsAsciiLetterOrDigit(character));
    }
}

/// <summary>
/// Configuration validation. An empty panel configuration is legitimate; a half-filled one is
/// not, because each way of getting it wrong produces a confusing failure much later.
/// </summary>
public class PterodactylOptionsValidatorTests
{
    private readonly PterodactylOptionsValidator _sut = new();

    private static PterodactylOptions Valid() => new()
    {
        BaseUrl = "https://panel.example.com",
        ApiKey = "ptla_" + new string('a', 43),
        LocationId = 1,
        NestId = 5,
        EggId = 15
    };

    /// <summary>
    /// Most deployments of this site never provision anything. They must still start.
    /// </summary>
    [Fact]
    public void AnEmptyConfigurationIsValid()
    {
        Assert.True(_sut.Validate(null, new PterodactylOptions()).Succeeded);
    }

    [Fact]
    public void AFullyConfiguredPanelIsValid()
    {
        Assert.True(_sut.Validate(null, Valid()).Succeeded);
    }

    [Fact]
    public void AKeyWithoutAUrlIsRejected()
    {
        var options = Valid();
        options.BaseUrl = string.Empty;

        Assert.True(_sut.Validate(null, options).Failed);
    }

    [Fact]
    public void AUrlWithoutAKeyIsRejected_AndSaysWhereToPutIt()
    {
        var options = Valid();
        options.ApiKey = string.Empty;

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("Pterodactyl__ApiKey", StringComparison.Ordinal));
    }

    /// <summary>
    /// A client key authenticates and then 403s on everything, which reads like a permissions
    /// problem rather than the wrong key entirely. Worth catching at startup.
    /// </summary>
    [Fact]
    public void AClientKeyIsCalledOutSpecifically()
    {
        var options = Valid();
        options.ApiKey = "ptlc_" + new string('a', 43);

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("Client API key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AKeyWithNoRecognisedPrefixIsRejected()
    {
        var options = Valid();
        options.ApiKey = "not-a-panel-key";

        Assert.True(_sut.Validate(null, options).Failed);
    }

    [Theory]
    [InlineData("panel.example.com")]
    [InlineData("ftp://panel.example.com")]
    [InlineData("not a url")]
    public void ABaseUrlThatIsNotAnHttpUrlIsRejected(string baseUrl)
    {
        var options = Valid();
        options.BaseUrl = baseUrl;

        Assert.True(_sut.Validate(null, options).Failed);
    }

    /// <summary>
    /// The client appends <c>/api/application</c> itself, so a base URL that already carries it
    /// produces a 404 on every call.
    /// </summary>
    [Fact]
    public void ABaseUrlThatAlreadyIncludesTheApiPathIsRejected()
    {
        var options = Valid();
        options.BaseUrl = "https://panel.example.com/api/application";

        Assert.True(_sut.Validate(null, options).Failed);
    }

    [Theory]
    [InlineData("16261", true)]
    [InlineData("16261-16281", true)]
    [InlineData("16261-16261", true)]
    [InlineData("16261-16260", false)]
    [InlineData("1000-9000", false)]
    [InlineData("abc", false)]
    [InlineData("16261-", false)]
    [InlineData("-16261", false)]
    [InlineData("1-2-3", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PortRangesAreCheckedAgainstWhatThePanelAccepts(string? range, bool expected)
    {
        Assert.Equal(expected, PterodactylOptionsValidator.IsValidPortRange(range));
    }

    [Fact]
    public void AnInvalidPortRangeFailsValidation()
    {
        var options = Valid();
        options.PortRange = ["16261-99999999"];

        Assert.True(_sut.Validate(null, options).Failed);
    }

    // ── Key redaction ─────────────────────────────────────────────────────

    /// <summary>
    /// Diagnostics need to answer "did it read the key I set?" without the key reaching a log,
    /// a page or an exception message.
    /// </summary>
    [Fact]
    public void TheKeyIsNeverDescribedInFull()
    {
        var options = Valid();
        var description = options.DescribeKey();

        Assert.DoesNotContain(options.ApiKey, description, StringComparison.Ordinal);
        Assert.StartsWith("ptla_", description, StringComparison.Ordinal);
        Assert.Contains("48", description, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnsetKeyIsDescribedAsUnset()
    {
        Assert.Equal("not set", new PterodactylOptions().DescribeKey());
    }

    /// <summary>
    /// Interpolating an options object into a log message must not leak the credential, so
    /// ToString is overridden to name the type and nothing else.
    /// </summary>
    [Fact]
    public void ToStringCannotLeakTheKey()
    {
        var options = Valid();

        Assert.DoesNotContain(options.ApiKey, $"{options}", StringComparison.Ordinal);
        Assert.DoesNotContain(options.BaseUrl, $"{options}", StringComparison.Ordinal);
    }

    [Fact]
    public void TheBaseUrlLosesItsTrailingSlash_SoPathsConcatenatePredictably()
    {
        var options = Valid();
        options.BaseUrl = "https://panel.example.com/";

        Assert.Equal("https://panel.example.com", options.NormalisedBaseUrl);
    }
}

/// <summary>
/// The gate in front of the provisioning lab.
/// </summary>
public class ProvisioningLabGuardTests
{
    private static ProvisioningLabGuard Build(string environmentName, bool enabled) =>
        new(new StubEnvironment(environmentName),
            Options.Create(new ProvisioningLabOptions { Enabled = enabled }));

    [Fact]
    public void DevelopmentOpensTheLab()
    {
        Assert.True(Build("Development", enabled: false).IsAvailable);
    }

    /// <summary>
    /// The default anywhere else is closed. A page that creates and deletes real servers is not
    /// something to leave reachable by accident.
    /// </summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("")]
    public void EverywhereElseIsClosedByDefault(string environmentName)
    {
        var guard = Build(environmentName, enabled: false);

        Assert.False(guard.IsAvailable);
        Assert.Throws<InvalidOperationException>(guard.EnsureAvailable);
    }

    [Fact]
    public void TheFlagOpensItDeliberately()
    {
        var guard = Build("Staging", enabled: true);

        Assert.True(guard.IsAvailable);
        guard.EnsureAvailable();
    }

    [Fact]
    public void TheClosedReasonNamesTheEnvironment_SoADeveloperKnowsWhy()
    {
        Assert.Contains("Production", Build("Production", enabled: false).ClosedReason, StringComparison.Ordinal);
    }

    private sealed class StubEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "HowToSoftware.Hosting";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
