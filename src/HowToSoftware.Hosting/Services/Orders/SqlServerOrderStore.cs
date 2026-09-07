using System.Security.Cryptography;
using System.Text;
using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Commerce;
using HowToSoftware.Hosting.Models.Orders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Services.Orders;

/// <summary>
/// SQL Server implementation of the existing order persistence seam. The payment and
/// provisioning services remain unaware of the active database provider.
/// </summary>
public sealed class SqlServerOrderStore(IDbContextFactory<CommerceDbContext> factory) : IOrderStore
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var game = await db.Games.SingleOrDefaultAsync(x => x.Slug == order.GameId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Game '{order.GameId}' is missing from the commerce database. Apply migrations and run the development seed.");

        var plan = await db.HostingPlans.SingleOrDefaultAsync(
                x => x.GameId == game.Id && x.Slug == order.PlanId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Plan '{order.PlanId}' is missing from the commerce database. Apply migrations and run the development seed.");

        db.Orders.Add(ToRecord(order, game.Id, plan.Id));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var record = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Order?> FindByCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var record = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StripeCheckoutSessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Order?> FindBySubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var record = await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StripeSubscriptionId == subscriptionId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var record = await db.Orders.SingleOrDefaultAsync(x => x.Id == order.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order {order.Id} no longer exists.");

        Copy(order, record);
        record.CustomerProfileId = await ResolveCustomerProfileAsync(db, order, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListAwaitingFulfilmentAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Orders.AsNoTracking()
            .Where(x => (x.Status == nameof(OrderStatus.Paid)
                    && x.ProvisioningStage == nameof(FulfilmentStage.NotStarted))
                || x.Status == nameof(OrderStatus.Provisioning))
            .OrderBy(x => x.PaidAt)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> TryRecordEventAsync(
        string eventId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        db.StripeEvents.Add(new StripeEventRecord
        {
            Id = Guid.NewGuid(),
            StripeEventId = eventId,
            EventType = eventType,
            Processed = true,
            ReceivedAt = now,
            ProcessedAt = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception) when (
            // 2627 unique constraint, 2601 unique index. Either means this delivery is a duplicate
            // and another worker already claimed it, which is exactly what the caller asked.
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }

    public async Task ReleaseEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var record = await db.StripeEvents.SingleOrDefaultAsync(
                x => x.StripeEventId == eventId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        db.StripeEvents.Remove(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CommerceOrder ToRecord(Order order, Guid gameId, Guid planId)
    {
        var record = new CommerceOrder
        {
            Id = order.Id,
            GameId = gameId,
            PlanId = planId,
            GameSlug = order.GameId,
            PlanSlug = order.PlanId,
            PlanName = order.PlanName,
            BillingPeriod = BillingPolicy.Slug(order.BillingPeriod),
            Currency = order.Currency.ToUpperInvariant(),
            Status = order.Status.ToString(),
            ProvisioningStage = order.ProvisioningStage.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
        Copy(order, record);
        return record;
    }

    private static void Copy(Order source, CommerceOrder target)
    {
        target.HtsUserId = source.UserId;
        target.CustomerEmail = source.CustomerEmail;
        target.MonthlyPriceCents = ToCents(source.MonthlyPrice);
        target.BaseAmountCents = ToCents(source.BaseAmount);
        target.DiscountPercent = source.DiscountPercentage;
        target.DiscountAmountCents = ToCents(source.DiscountAmount);
        target.FinalAmountCents = ToCents(source.FinalAmount);
        target.Status = source.Status.ToString();
        target.ProvisioningStage = source.ProvisioningStage.ToString();
        target.StripeCheckoutSessionId = source.StripeCheckoutSessionId;
        target.StripePaymentIntentId = source.StripePaymentIntentId;
        target.StripeCustomerId = source.StripeCustomerId;
        target.StripeSubscriptionId = source.StripeSubscriptionId;
        target.StripeInvoiceId = source.StripeInvoiceId;
        target.SubscriptionStatus = source.SubscriptionStatus;
        target.ServerIdentifier = source.ServerIdentifier;
        target.FailureReason = source.FailureReason;
        target.UpdatedAt = source.UpdatedAt;
        target.PaidAt = source.PaidAt;
    }

    private static Order ToDomain(CommerceOrder source)
    {
        if (!BillingPolicy.TryParse(source.BillingPeriod, out var period)
            || !Enum.TryParse<OrderStatus>(source.Status, out var status)
            || !Enum.TryParse<FulfilmentStage>(source.ProvisioningStage, out var stage))
        {
            throw new InvalidOperationException($"Order {source.Id} contains an unsupported persisted state.");
        }

        return new Order
        {
            Id = source.Id,
            UserId = source.HtsUserId,
            CustomerEmail = source.CustomerEmail,
            GameId = source.GameSlug,
            PlanId = source.PlanSlug,
            PlanName = source.PlanName,
            BillingPeriod = period,
            MonthlyPrice = FromCents(source.MonthlyPriceCents),
            BaseAmount = FromCents(source.BaseAmountCents),
            DiscountPercentage = source.DiscountPercent,
            DiscountAmount = FromCents(source.DiscountAmountCents),
            FinalAmount = FromCents(source.FinalAmountCents),
            Currency = source.Currency.ToLowerInvariant(),
            Status = status,
            ProvisioningStage = stage,
            StripeCheckoutSessionId = source.StripeCheckoutSessionId,
            StripePaymentIntentId = source.StripePaymentIntentId,
            StripeCustomerId = source.StripeCustomerId,
            StripeSubscriptionId = source.StripeSubscriptionId,
            StripeInvoiceId = source.StripeInvoiceId,
            SubscriptionStatus = source.SubscriptionStatus,
            ServerIdentifier = source.ServerIdentifier,
            FailureReason = source.FailureReason,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            PaidAt = source.PaidAt
        };
    }

    private static async Task<Guid?> ResolveCustomerProfileAsync(
        CommerceDbContext db,
        Order order,
        CancellationToken cancellationToken)
    {
        var identity = !string.IsNullOrWhiteSpace(order.UserId)
            ? order.UserId.Trim()
            : !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? GuestIdentity(order.CustomerEmail)
                : null;

        if (identity is null)
        {
            return null;
        }

        var profile = await db.CustomerProfiles.SingleOrDefaultAsync(
                x => x.HtsUserId == identity,
                cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            profile = new CustomerProfile
            {
                Id = Guid.NewGuid(),
                HtsUserId = identity,
                Email = order.CustomerEmail,
                StripeCustomerId = order.StripeCustomerId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
            db.CustomerProfiles.Add(profile);
        }
        else
        {
            profile.Email = order.CustomerEmail ?? profile.Email;
            profile.StripeCustomerId = order.StripeCustomerId ?? profile.StripeCustomerId;
            profile.UpdatedAt = order.UpdatedAt;
        }

        return profile.Id;
    }

    private static string GuestIdentity(string email)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return $"guest:{Convert.ToHexString(hash)}";
    }

    private static long ToCents(decimal amount) =>
        checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));

    private static decimal FromCents(long amount) => amount / 100m;
}
