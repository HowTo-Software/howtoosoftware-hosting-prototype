using HowToSoftware.Hosting.Models.Orders;

namespace HowToSoftware.Hosting.Services.Orders;

/// <summary>An unavailable order store for hosts without a configured database.</summary>
public sealed class NullOrderStore : IOrderStore
{
    public bool IsConfigured => false;

    public Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException("Order persistence is not configured."));

    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Order?>(null);

    public Task<Order?> FindByCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Order?>(null);

    public Task<Order?> FindBySubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Order?>(null);

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException("Order persistence is not configured."));

    public Task<bool> TryMarkPaidAsync(
        Guid orderId,
        DateTimeOffset now,
        string? customerEmail,
        string? stripeCustomerId,
        string? stripePaymentIntentId,
        string? stripeSubscriptionId,
        string? stripeCheckoutSessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<Guid>> ListAwaitingFulfilmentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<bool> TryRecordEventAsync(
        string eventId,
        string eventType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task ReleaseEventAsync(string eventId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
