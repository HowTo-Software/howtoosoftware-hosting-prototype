using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Services.Orders;

/// <summary>
/// <see cref="IOrderStore"/> over <see cref="HostingDbContext"/>.
/// </summary>
/// <remarks>
/// Built on a context factory rather than a scoped context, because two of its callers are not
/// request-scoped: the fulfilment worker is a hosted service, and the webhook wants a short,
/// self-contained unit of work per event. Each call opens a context, does one thing, and
/// closes it.
/// </remarks>
public sealed class EfOrderStore : IOrderStore
{
    private readonly IDbContextFactory<HostingDbContext> _factory;

    /// <summary>Creates the store.</summary>
    public EfOrderStore(IDbContextFactory<HostingDbContext> factory) => _factory = factory;

    /// <inheritdoc />
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> FindByCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> FindBySubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return null;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.Orders.Update(order);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListAwaitingFulfilmentAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Ordered in memory: SQLite cannot sort a DateTimeOffset column, and the set here is
        // whatever was paid across one restart - a handful of rows at most.
        var waiting = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.ProvisioningStage == FulfilmentStage.NotStarted)
            .Select(o => new { o.Id, o.PaidAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return waiting.OrderBy(o => o.PaidAt).Select(o => o.Id).ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> TryRecordEventAsync(string eventId, string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        db.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            Id = eventId,
            Type = eventType,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            // The primary key is the event id, so a second delivery of the same event fails
            // the insert. That is the idempotency check: the unique constraint, not a read
            // followed by a write that two concurrent deliveries could both pass.
            return false;
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
