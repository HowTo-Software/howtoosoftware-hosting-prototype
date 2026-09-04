namespace HowToSoftware.Hosting.Models.Commerce;

/// <summary>Stable mapping between an HTS identity and its billing identity.</summary>
public sealed class CustomerProfile
{
    public Guid Id { get; set; }
    public required string HtsUserId { get; set; }
    public string? Email { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A game offered by the hosting catalogue.</summary>
public sealed class CommerceGame
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool Active { get; set; }
    public bool PrimaryGame { get; set; }
    public string? ArtworkUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Commercial and Pterodactyl resource definition for one hosting tier.</summary>
public sealed class CommerceHostingPlan
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int RamMb { get; set; }
    public int CpuPercent { get; set; }
    public int DiskMb { get; set; }
    public int AllocationLimit { get; set; }
    public int DatabaseLimit { get; set; }
    public int BackupLimit { get; set; }
    public long MonthlyPriceCents { get; set; }
    public required string Currency { get; set; }
    public bool Active { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Stripe mapping for one plan and one recurring billing period.</summary>
public sealed class PlanBillingPrice
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public required string BillingPeriod { get; set; }
    public int DiscountPercent { get; set; }
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
    public long AmountCents { get; set; }
    public required string Currency { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Order snapshot persisted independently from the primary HTS database.</summary>
public sealed class CommerceOrder
{
    public Guid Id { get; set; }
    public Guid? CustomerProfileId { get; set; }
    public string? HtsUserId { get; set; }
    public string? CustomerEmail { get; set; }
    public Guid GameId { get; set; }
    public Guid PlanId { get; set; }
    public required string GameSlug { get; set; }
    public required string PlanSlug { get; set; }
    public required string PlanName { get; set; }
    public required string BillingPeriod { get; set; }
    public required string Currency { get; set; }
    public long MonthlyPriceCents { get; set; }
    public long BaseAmountCents { get; set; }
    public int DiscountPercent { get; set; }
    public long DiscountAmountCents { get; set; }
    public long FinalAmountCents { get; set; }
    public required string Status { get; set; }
    public required string ProvisioningStage { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string? ServerIdentifier { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

/// <summary>The durable hosting service created from one paid order.</summary>
public sealed class HostingServiceRecord
{
    public Guid Id { get; set; }
    public Guid? CustomerProfileId { get; set; }
    public Guid OrderId { get; set; }
    public Guid GameId { get; set; }
    public Guid PlanId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public int? PterodactylServerId { get; set; }
    public string? PterodactylServerUuid { get; set; }
    public int? PterodactylNodeId { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>One Stripe delivery, including failed attempts, for reliable idempotency.</summary>
public sealed class StripeEventRecord
{
    public Guid Id { get; set; }
    public required string StripeEventId { get; set; }
    public required string EventType { get; set; }
    public bool Processed { get; set; }
    public string? ProcessingError { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

/// <summary>Durable, retry-safe provisioning work for one hosting service.</summary>
public sealed class ProvisioningJobRecord
{
    public Guid Id { get; set; }
    public Guid HostingServiceId { get; set; }
    public Guid OrderId { get; set; }
    public required string Status { get; set; }
    public int AttemptCount { get; set; }
    public int? SelectedNodeId { get; set; }
    public int? PterodactylServerId { get; set; }
    public string? PterodactylServerUuid { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Operator-controlled eligibility and priority for one Pterodactyl node.</summary>
public sealed class HostingNodeRecord
{
    public Guid Id { get; set; }
    public int PterodactylNodeId { get; set; }
    public required string Name { get; set; }
    public string? Region { get; set; }
    public string? Location { get; set; }
    public bool Enabled { get; set; }
    public bool Maintenance { get; set; }
    public int Priority { get; set; }
    public string[] SupportedGames { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Non-secret Pterodactyl deployment configuration for one game.</summary>
public sealed class GameDeploymentProfileRecord
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public int PterodactylNestId { get; set; }
    public int PterodactylEggId { get; set; }
    public string? DockerImage { get; set; }
    public string? DefaultStartup { get; set; }
    public string DeploymentConfigJson { get; set; } = "{}";
    public bool Active { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Append-only audit entry for a deployment.</summary>
public sealed class DeploymentEventRecord
{
    public Guid Id { get; set; }
    public Guid ProvisioningJobId { get; set; }
    public Guid HostingServiceId { get; set; }
    public required string EventType { get; set; }
    public required string Message { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Small Stripe invoice reference/cache; Stripe remains the source of truth.</summary>
public sealed class BillingInvoiceReference
{
    public Guid Id { get; set; }
    public Guid? CustomerProfileId { get; set; }
    public Guid? HostingServiceId { get; set; }
    public required string StripeInvoiceId { get; set; }
    public required string Status { get; set; }
    public long AmountDueCents { get; set; }
    public long AmountPaidCents { get; set; }
    public required string Currency { get; set; }
    public DateTimeOffset? InvoiceDate { get; set; }
    public string? HostedInvoiceUrl { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}
