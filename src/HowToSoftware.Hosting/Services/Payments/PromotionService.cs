using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Commerce;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Services.Payments;

public sealed record PromotionResult(
    PromotionCode Promotion,
    decimal DiscountAmount,
    decimal FinalAmount);

public interface IPromotionService
{
    Task<PromotionResult?> ValidateAsync(
        string? code,
        string gameSlug,
        string planSlug,
        BillingQuote quote,
        CancellationToken cancellationToken = default);

    Task RedeemAsync(
        Guid promotionId,
        Guid orderId,
        long discountAmountCents,
        DateTimeOffset redeemedAt,
        CancellationToken cancellationToken = default);
}

public sealed class PromotionService(
    IDbContextFactory<CommerceDbContext> factory)
    : IPromotionService
{
    public async Task<PromotionResult?> ValidateAsync(
        string? code,
        string gameSlug,
        string planSlug,
        BillingQuote quote,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var promotion = await db.PromotionCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Code == normalizedCode,
                cancellationToken);

        if (promotion is null || !promotion.Active)
        {
            return null;
        }

        if (promotion.StartsAt is not null && now < promotion.StartsAt.Value)
        {
            return null;
        }

        if (promotion.ExpiresAt is not null && now >= promotion.ExpiresAt.Value)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(promotion.GameSlug) &&
            !string.Equals(
                promotion.GameSlug,
                gameSlug,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(promotion.PlanSlug) &&
            !string.Equals(
                promotion.PlanSlug,
                planSlug,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var redemptionCount = await db.PromotionRedemptions
            .CountAsync(x => x.PromotionCodeId == promotion.Id, cancellationToken);

        if (promotion.MaxRedemptions is not null &&
            redemptionCount >= promotion.MaxRedemptions.Value)
        {
            return null;
        }

        var baseAmount = quote.FinalAmount;

        if (promotion.MinimumAmountCents is not null &&
            decimal.Multiply(promotion.MinimumAmountCents.Value, 0.01m) > baseAmount)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(promotion.Currency) &&
            !string.Equals(
                promotion.Currency,
                "usd",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!promotion.Stackable && quote.DiscountPercent > 0)
        {
            baseAmount = quote.BaseAmount;
        }

        var discount = promotion.DiscountType switch
        {
            "percent" when promotion.DiscountPercent is not null
                => BillingPolicy.RoundToCent(
                    baseAmount * promotion.DiscountPercent.Value / 100m),

            "fixed" when promotion.FixedAmountCents is not null
                => Math.Min(
                    baseAmount,
                    BillingPolicy.RoundToCent(
                        promotion.FixedAmountCents.Value / 100m)),

            _ => 0m
        };

        if (discount <= 0m)
        {
            return null;
        }

        var finalAmount = BillingPolicy.RoundToCent(
            Math.Max(0m, baseAmount - discount));

        return new PromotionResult(
            promotion,
            discount,
            finalAmount);
    }

    public async Task RedeemAsync(
        Guid promotionId,
        Guid orderId,
        long discountAmountCents,
        DateTimeOffset redeemedAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var exists = await db.PromotionRedemptions
            .AnyAsync(
                x => x.PromotionCodeId == promotionId &&
                     x.OrderId == orderId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        db.PromotionRedemptions.Add(new PromotionRedemption
        {
            Id = Guid.NewGuid(),
            PromotionCodeId = promotionId,
            OrderId = orderId,
            DiscountAmountCents = discountAmountCents,
            RedeemedAt = redeemedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
