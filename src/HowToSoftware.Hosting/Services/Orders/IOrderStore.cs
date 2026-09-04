using HowToSoftware.Hosting.Models.Orders;

namespace HowToSoftware.Hosting.Services.Orders;

/// <summary>
/// Persistence for orders and for the Stripe events already acted on.
/// </summary>
/// <remarks>
/// Small on purpose. The payment and fulfilment services own the rules about what an order may
/// become; this only remembers what they decided. Every method takes a cancellation token
/// because the webhook and the fulfilment worker both run under one.
/// </remarks>
public interface IOrderStore
{
    /// <summary>Saves a new order.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Finds an order by its id.</summary>
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Finds the order a Checkout session was created for.</summary>
    Task<Order?> FindByCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Finds the order behind a Stripe subscription.</summary>
    Task<Order?> FindBySubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Writes an order's current state back.</summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of paid or in-progress orders a restart could have dropped from the in-memory queue.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListAwaitingFulfilmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a Stripe event as handled.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when this call recorded it; <see langword="false"/> when it was
    /// already there, in which case the caller must not act on the event again.
    /// </returns>
    Task<bool> TryRecordEventAsync(string eventId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an event claim after processing failed, allowing Stripe's next delivery to retry.
    /// </summary>
    Task ReleaseEventAsync(string eventId, CancellationToken cancellationToken = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
