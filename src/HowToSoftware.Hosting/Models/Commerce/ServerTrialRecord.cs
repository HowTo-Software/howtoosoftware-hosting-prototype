using HowToSoftware.Hosting.Services.Trials;

namespace HowToSoftware.Hosting.Models.Commerce;

/// <summary>Permanent entitlement claim and durable lifecycle state; deletion never removes this row.</summary>
public sealed class ServerTrialRecord
{
    public Guid Id { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public required string GameSlug { get; set; }
    public string Locale { get; set; } = "en";
    public string? ProfileId { get; set; }
    public string? Version { get; set; }
    public TrialState State { get; set; }
    public string? VerificationTokenHash { get; set; }
    public DateTimeOffset? VerificationExpiresAt { get; set; }
    public string? AccessTokenHash { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public int? PterodactylUserId { get; set; }
    public int? PterodactylServerId { get; set; }
    public string? ServerIdentifier { get; set; }
    public string? ServerUuid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? DeleteAfter { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? ConvertedAt { get; set; }
    public Guid? UpgradeOrderId { get; set; }
    public DateTimeOffset? ReservationExpiresAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? ReadyEmailSentAt { get; set; }
    public DateTimeOffset? ExpiredEmailSentAt { get; set; }
    public DateTimeOffset? DeletedEmailSentAt { get; set; }
    public string? LastFailureClass { get; set; }
}
