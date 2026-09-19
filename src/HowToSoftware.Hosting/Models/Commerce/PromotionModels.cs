namespace HowToSoftware.Hosting.Models.Commerce;

/// <summary>A promotional code managed by HTS.</summary>
public sealed class PromotionCode
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    /// <summary>"percent" or "fixed".</summary>
    public required string DiscountType { get; set; }

    public int? DiscountPercent { get; set; }

    public long? FixedAmountCents { get; set; }

    public string? Currency { get; set; }

    public bool Active { get; set; } = true;

    /// <summary>Whether this promotion may stack with period discounts.</summary>
    public bool Stackable { get; set; }

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public int? MaxRedemptions { get; set; }

    public long? MinimumAmountCents { get; set; }

    /// <summary>Optional restriction to one game.</summary>
    public string? GameSlug { get; set; }

    /// <summary>Optional restriction to one plan.</summary>
    public string? PlanSlug { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Records a successful use of a promotional code.</summary>
public sealed class PromotionRedemption
{
    public Guid Id { get; set; }

    public Guid PromotionCodeId { get; set; }

    public Guid OrderId { get; set; }

    public long DiscountAmountCents { get; set; }

    public DateTimeOffset RedeemedAt { get; set; }
}
