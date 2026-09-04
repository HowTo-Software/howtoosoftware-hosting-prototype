using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Provisioning;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services.Orders;

/// <summary>
/// Orders waiting to be turned into servers.
/// </summary>
/// <remarks>
/// The webhook must answer Stripe quickly, and creating a server on the panel is neither quick
/// nor something to do inside a request Stripe will retry if it times out. So the webhook
/// enqueues an order id and returns; <see cref="OrderFulfillmentWorker"/> does the work.
/// </remarks>
public sealed class OrderFulfillmentQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    /// <summary>Queues an order for fulfilment.</summary>
    public void Enqueue(Guid orderId) => _channel.Writer.TryWrite(orderId);

    /// <summary>Order ids as they arrive.</summary>
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// Turns a paid order into a running server.
/// </summary>
public interface IOrderFulfillmentService
{
    /// <summary>
    /// Provisions the server for a paid order, moving it through
    /// <see cref="OrderStatus.Provisioning"/> to <see cref="OrderStatus.Active"/>.
    /// </summary>
    /// <remarks>
    /// Safe to call more than once for the same order: an order that is not
    /// <see cref="OrderStatus.Paid"/> is left alone, and provisioning itself is keyed on the
    /// order id, so a retry finds the server it already created.
    /// </remarks>
    Task FulfilAsync(Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="IOrderFulfillmentService"/> over the existing Pterodactyl provisioning service.
/// </summary>
/// <remarks>
/// <para>
/// This is the joint between payment and infrastructure, and it is deliberately thin:
/// <see cref="IProvisioningService"/> already knows how to create a panel user, pick a node,
/// build the container from the plan's limits and start the server. This class translates an
/// order into that request, records what came back, and waits for the install to finish.
/// </para>
/// <para>
/// When the panel is not configured on the host, a paid order stays paid with provisioning not
/// started, and a warning is logged. It is never marked failed for that: the customer's payment
/// is good, and the server will be created when an operator runs fulfilment on a configured host.
/// </para>
/// </remarks>
public sealed class OrderFulfillmentService : IOrderFulfillmentService
{
    /// <summary>How long to wait for the egg's install script before handing over to an operator.</summary>
    internal static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(12);

    /// <summary>How often to ask the panel whether the install has finished.</summary>
    internal static readonly TimeSpan InstallPollInterval = TimeSpan.FromSeconds(15);

    private readonly IOrderStore _orders;
    private readonly IProvisioningService _provisioning;
    private readonly IPterodactylClient _panel;
    private readonly IOptionsMonitor<PterodactylOptions> _panelOptions;
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderFulfillmentService> _logger;
    private readonly IProvisioningStateStore _state;

    /// <summary>Creates the service.</summary>
    public OrderFulfillmentService(
        IOrderStore orders,
        IProvisioningService provisioning,
        IPterodactylClient panel,
        IOptionsMonitor<PterodactylOptions> panelOptions,
        TimeProvider clock,
        ILogger<OrderFulfillmentService> logger,
        IProvisioningStateStore? state = null)
    {
        _orders = orders;
        _provisioning = provisioning;
        _panel = panel;
        _panelOptions = panelOptions;
        _clock = clock;
        _logger = logger;
        _state = state ?? new NullProvisioningStateStore();
    }

    /// <inheritdoc />
    public async Task FulfilAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.FindAsync(orderId, cancellationToken).ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogWarning("Fulfilment asked for unknown order {OrderId}.", orderId);
            return;
        }

        if (order.Status is not (OrderStatus.Paid or OrderStatus.Provisioning))
        {
            _logger.LogInformation("Order {OrderId} is {Status}, not Paid; fulfilment skipped.", order.Id, order.Status);
            return;
        }

        if (!_panelOptions.CurrentValue.IsConfigured)
        {
            _logger.LogWarning(
                "Order {OrderId} is paid but the panel is not configured on this host. Left as Paid for an operator.",
                order.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(order.CustomerEmail))
        {
            // Without an email there is no panel account to hand the server to. This is an
            // operator problem, not something to retry.
            await FailAsync(order, FulfilmentStage.Preparing, "The order carries no customer email, so no panel account can be created.", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (order.Status is OrderStatus.Paid)
        {
            order.TransitionTo(OrderStatus.Provisioning, _clock.GetUtcNow());
            order.ProvisioningStage = FulfilmentStage.Preparing;
            await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        }

        await _state.BeginAsync(order.Id, cancellationToken).ConfigureAwait(false);

        var request = new ProvisioningRequest
        {
            RequestId = order.Id,
            CustomerId = CustomerIdentity.FromEmail(order.CustomerEmail),
            CustomerEmail = order.CustomerEmail,
            PlanSlug = order.PlanId,
            ServerName = $"{order.PlanName} {order.Id.ToString("N")[..8]}",
            IsTest = false,
            StartOnCompletion = true
        };

        var result = await _provisioning.ProvisionAsync(request, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await FailAsync(order, FulfilmentStage.Failed, $"Provisioning failed: {result.Message}", cancellationToken).ConfigureAwait(false);
            return;
        }

        order.ServerIdentifier = result.ServerIdentifier;
        order.ProvisioningStage = result.NodeId is null ? FulfilmentStage.NodeSelected : FulfilmentStage.ResourcesAllocated;
        order.UpdatedAt = _clock.GetUtcNow();
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await _state.MarkCreatedAsync(order.Id, result, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Order {OrderId}: server {Server} created on node {Node}; waiting for install.",
            order.Id, result.ServerIdentifier, result.NodeId);

        await AwaitInstallAsync(order, result.ExternalId, cancellationToken).ConfigureAwait(false);
    }

    private async Task AwaitInstallAsync(Order order, string? externalId, CancellationToken cancellationToken)
    {
        order.ProvisioningStage = FulfilmentStage.Installing;
        order.UpdatedAt = _clock.GetUtcNow();
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await _state.MarkInstallingAsync(order.Id, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(externalId))
        {
            await FailAsync(
                order,
                FulfilmentStage.Failed,
                "Pterodactyl returned no external server id, so installation cannot be verified.",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var deadline = _clock.GetUtcNow() + InstallTimeout;

        while (_clock.GetUtcNow() < deadline)
        {
            await Task.Delay(InstallPollInterval, _clock, cancellationToken).ConfigureAwait(false);

            var server = await _panel.FindServerByExternalIdAsync(externalId, cancellationToken).ConfigureAwait(false);

            if (server is null)
            {
                continue;
            }

            // Pterodactyl reports a transient status while the egg installs and clears it when
            // the server is ready. "install_failed" and "suspended" are terminal for our purposes.
            switch (server.Status?.ToLowerInvariant())
            {
                case null or "":
                    order.ProvisioningStage = FulfilmentStage.Online;
                    order.TransitionTo(OrderStatus.Active, _clock.GetUtcNow());
                    await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
                    await _state.MarkOnlineAsync(order.Id, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Order {OrderId}: server {Server} is online. Order active.", order.Id, order.ServerIdentifier);
                    return;

                case "install_failed" or "suspended":
                    await FailAsync(order, FulfilmentStage.Failed, $"The panel reports the server as {server.Status}.", cancellationToken).ConfigureAwait(false);
                    return;
            }
        }

        await FailAsync(
            order,
            FulfilmentStage.Failed,
            $"Pterodactyl installation did not finish within {InstallTimeout}.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task FailAsync(Order order, FulfilmentStage stage, string reason, CancellationToken cancellationToken)
    {
        if (order.CanTransitionTo(OrderStatus.Failed))
        {
            order.TransitionTo(OrderStatus.Failed, _clock.GetUtcNow());
        }

        order.ProvisioningStage = stage;
        order.FailureReason = reason;
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await _state.MarkFailedAsync(order.Id, reason, cancellationToken).ConfigureAwait(false);

        _logger.LogError("Order {OrderId}: {Reason}", order.Id, reason);
    }
}

/// <summary>
/// Drains <see cref="OrderFulfillmentQueue"/> for as long as the site runs.
/// </summary>
/// <remarks>
/// On start it re-queues every order that was paid but never provisioned - the queue lives in
/// memory, so a restart between the webhook and the worker would otherwise lose the job.
/// </remarks>
public sealed class OrderFulfillmentWorker : BackgroundService
{
    private readonly OrderFulfillmentQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly IOrderStore _orders;
    private readonly ILogger<OrderFulfillmentWorker> _logger;

    /// <summary>Creates the worker.</summary>
    public OrderFulfillmentWorker(
        OrderFulfillmentQueue queue,
        IServiceScopeFactory scopes,
        IOrderStore orders,
        ILogger<OrderFulfillmentWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _orders = orders;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            foreach (var orderId in await _orders.ListAwaitingFulfilmentAsync(stoppingToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Re-queuing order {OrderId} left paid but unprovisioned.", orderId);
                _queue.Enqueue(orderId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Could not scan for unprovisioned orders at start-up.");
        }

        await foreach (var orderId in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var fulfilment = scope.ServiceProvider.GetRequiredService<IOrderFulfillmentService>();

                await fulfilment.FulfilAsync(orderId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // One order's failure must not stop the queue. The order itself records why.
                _logger.LogError(exception, "Fulfilment of order {OrderId} threw.", orderId);
            }
        }
    }
}

/// <summary>
/// Derives the customer id the provisioner keys panel accounts on.
/// </summary>
/// <remarks>
/// There is no user table yet, so the id is a stable function of the email address: the same
/// customer buying twice gets the same panel account rather than two. When accounts exist,
/// their ids replace this.
/// </remarks>
public static class CustomerIdentity
{
    /// <summary>A stable id for an email address, case-insensitively.</summary>
    public static Guid FromEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));

        return new Guid(hash.AsSpan(0, 16));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
