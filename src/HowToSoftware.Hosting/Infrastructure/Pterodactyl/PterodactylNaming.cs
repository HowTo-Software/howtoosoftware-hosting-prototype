using System.Security.Cryptography;
using System.Text;

namespace HowToSoftware.Hosting.Infrastructure.Pterodactyl;

/// <summary>
/// The identifiers this application writes into the panel, and the rules they must satisfy.
/// </summary>
/// <remarks>
/// <para>
/// External ids are the correlation between our records and the panel's. They are what makes
/// provisioning idempotent: before creating anything the provisioner looks the id up, and reuses
/// what it finds. Without them a retried request - or a payment webhook delivered twice - creates
/// a second server the customer never asked for and we still pay to host.
/// </para>
/// <para>
/// They also gate deletion. Only a server whose external id carries the test prefix may be
/// deleted through the provisioning lab, so no form on this site can reach a customer's server.
/// </para>
/// </remarks>
public static class PterodactylNaming
{
    /// <summary>Prefix for a real customer's panel user.</summary>
    public const string CustomerPrefix = "hts-customer:";

    /// <summary>Prefix for a panel user created by the provisioning lab.</summary>
    public const string TestCustomerPrefix = "hts-test-customer:";

    /// <summary>Prefix for a server created by the provisioning lab.</summary>
    /// <remarks>This exact string is the deletion guard. Changing it orphans existing test servers.</remarks>
    public const string TestServerPrefix = "hts-test-server:";

    /// <summary>Prefix for a server created by a real order.</summary>
    public const string ServerPrefix = "hts-server:";

    /// <summary>The panel stores external ids in a <c>varchar(191)</c>.</summary>
    public const int MaxExternalIdLength = 191;

    /// <summary>External id for a customer's panel user.</summary>
    /// <param name="customerId">Our customer identifier.</param>
    public static string CustomerExternalId(Guid customerId) => CustomerPrefix + customerId.ToString("D");

    /// <summary>External id for a panel user created by the lab.</summary>
    /// <param name="customerId">Synthetic customer identifier.</param>
    public static string TestCustomerExternalId(Guid customerId) =>
        TestCustomerPrefix + customerId.ToString("D");

    /// <summary>External id for a server created by the lab.</summary>
    /// <param name="requestId">Provisioning request identifier.</param>
    public static string TestServerExternalId(Guid requestId) => TestServerPrefix + requestId.ToString("D");

    /// <summary>External id for a server created by a real order.</summary>
    /// <param name="requestId">Provisioning request identifier.</param>
    public static string ServerExternalId(Guid requestId) => ServerPrefix + requestId.ToString("D");

    /// <summary>
    /// Whether an external id marks a server the provisioning lab created, and may therefore
    /// delete.
    /// </summary>
    /// <param name="externalId">External id to test.</param>
    /// <remarks>
    /// Deliberately ordinal and case-sensitive. A guard that accepts <c>HTS-TEST-SERVER:</c> is a
    /// guard that can be talked around, and every id this application writes is lower-case.
    /// </remarks>
    public static bool IsTestServer(string? externalId) =>
        externalId is not null && externalId.StartsWith(TestServerPrefix, StringComparison.Ordinal);

    /// <summary>Whether an external id marks a panel user the lab created.</summary>
    /// <param name="externalId">External id to test.</param>
    public static bool IsTestCustomer(string? externalId) =>
        externalId is not null && externalId.StartsWith(TestCustomerPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Builds a panel username that satisfies the panel's own rule.
    /// </summary>
    /// <param name="seed">Something recognisable, usually the local part of an email address.</param>
    /// <returns>A username the panel will accept.</returns>
    /// <remarks>
    /// The panel applies <c>/^[a-z0-9]([\w.-]+)[a-z0-9]$/</c> to the lower-cased value. That
    /// means: first and last characters alphanumeric, only word characters, dots and hyphens in
    /// between, and - because the middle group is one-or-more - a minimum of three characters.
    /// A short seed is padded rather than rejected, so a customer called "Jo" still provisions.
    /// </remarks>
    public static string BuildUsername(string? seed)
    {
        var builder = new StringBuilder(32);

        foreach (var character in (seed ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-')
            {
                builder.Append(character);
            }
        }

        // Trim to alphanumeric at both ends, which is what the regex anchors demand.
        var trimmed = builder.ToString().Trim('_', '.', '-');

        if (trimmed.Length > 24)
        {
            trimmed = trimmed[..24].TrimEnd('_', '.', '-');
        }

        // A random tail keeps two customers with the same email local part from colliding on the
        // panel's unique index, and guarantees the three-character minimum on its own.
        var suffix = RandomToken(6);

        return trimmed.Length == 0 ? $"hts{suffix}" : $"{trimmed}{suffix}";
    }

    /// <summary>
    /// Generates a cryptographically random password for a newly created panel user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Application API enforces no length or complexity of its own - <c>"a"</c> is an
    /// accepted password - so the strength has to come from here.
    /// </para>
    /// <para>
    /// This value is written to the panel and then dropped. It is never logged, never persisted,
    /// never emailed and never returned to a browser. A customer reaches their account through a
    /// password-reset flow, which is the only way that does not require a plaintext password to
    /// exist somewhere for longer than one HTTP call.
    /// </para>
    /// </remarks>
    public static string GeneratePassword()
    {
        // 48 bytes of entropy, rendered base64url so every character is safe in JSON and in a
        // form field, then guaranteed to carry all four classes some panels check for.
        Span<byte> entropy = stackalloc byte[48];
        RandomNumberGenerator.Fill(entropy);

        var body = Convert.ToBase64String(entropy)
            .Replace('+', 'x')
            .Replace('/', 'y')
            .TrimEnd('=');

        return $"Hts{body}9!";
    }

    /// <summary>A short lower-case alphanumeric token, for disambiguating usernames.</summary>
    /// <param name="length">Characters to produce.</param>
    private static string RandomToken(int length)
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

        return RandomNumberGenerator.GetString(Alphabet, length);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
