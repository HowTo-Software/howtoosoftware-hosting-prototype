using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Models.Commerce;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Services.Provisioning;

/// <summary>Durable service, job and audit state behind the provisioning pipeline.</summary>
public interface IProvisioningStateStore
{
    Task BeginAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task MarkCreatedAsync(Guid orderId, ProvisioningResult result, CancellationToken cancellationToken = default);
    Task MarkInstallingAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task MarkOnlineAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>Local-development implementation used while the independent database is absent.</summary>
public sealed class NullProvisioningStateStore : IProvisioningStateStore
{
    public Task BeginAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkCreatedAsync(Guid orderId, ProvisioningResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkInstallingAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkOnlineAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkFailedAsync(Guid orderId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>SQL Server implementation with an append-only deployment event trail.</summary>
public sealed class SqlServerProvisioningStateStore(
    IDbContextFactory<CommerceDbContext> factory,
    TimeProvider clock) : IProvisioningStateStore
{
    public async Task BeginAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Two constraints, not one. A retrying strategy will not drive a transaction it did not
        // open, and each attempt must build its own context: a reused one still tracks the failed
        // attempt's inserts, so the replay would add a second hosting service for this order and
        // die on the unique index instead of recovering.
        await using var strategyContext = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await strategyContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(token => BeginCoreAsync(orderId, token), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task BeginCoreAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var order = await db.Orders.SingleAsync(x => x.Id == orderId, cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();

        var service = await db.HostingServices.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);
        if (service is null)
        {
            service = new HostingServiceRecord
            {
                Id = Guid.NewGuid(),
                CustomerProfileId = order.CustomerProfileId,
                OrderId = order.Id,
                GameId = order.GameId,
                PlanId = order.PlanId,
                StripeSubscriptionId = order.StripeSubscriptionId,
                Status = "provisioning",
                CreatedAt = now,
                UpdatedAt = now
            };
            db.HostingServices.Add(service);
        }

        var job = await db.ProvisioningJobs.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);
        if (job is null)
        {
            job = new ProvisioningJobRecord
            {
                Id = Guid.NewGuid(),
                HostingServiceId = service.Id,
                OrderId = order.Id,
                Status = "creating_server",
                AttemptCount = 1,
                CreatedAt = now,
                StartedAt = now,
                UpdatedAt = now
            };
            db.ProvisioningJobs.Add(job);
            AddEvent(db, job, service, "provisioning_queued", "Payment confirmed; provisioning started.", now);
        }
        else if (job.Status == "failed")
        {
            job.Status = "creating_server";
            job.AttemptCount++;
            job.StartedAt = now;
            job.CompletedAt = null;
            job.LastError = null;
            job.UpdatedAt = now;
            AddEvent(db, job, service, "provisioning_retried", "Provisioning retry started.", now);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task MarkCreatedAsync(Guid orderId, ProvisioningResult result, CancellationToken cancellationToken = default) =>
        UpdateAsync(
            orderId,
            "installing",
            "server_created",
            "Pterodactyl server created; installation is in progress.",
            result,
            null,
            cancellationToken);

    public Task MarkInstallingAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        UpdateAsync(orderId, "installing", "installation_started", "Game installation started.", null, null, cancellationToken);

    public Task MarkOnlineAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        UpdateAsync(orderId, "online", "server_online", "Server installation completed and the service is active.", null, null, cancellationToken);

    public Task MarkFailedAsync(Guid orderId, string reason, CancellationToken cancellationToken = default) =>
        UpdateAsync(orderId, "failed", "deployment_failed", reason, null, reason, cancellationToken);

    private async Task UpdateAsync(
        Guid orderId,
        string status,
        string eventType,
        string message,
        ProvisioningResult? result,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var job = await db.ProvisioningJobs.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);
        if (job is null)
        {
            return;
        }

        var service = await db.HostingServices.SingleAsync(x => x.Id == job.HostingServiceId, cancellationToken)
            .ConfigureAwait(false);
        var now = clock.GetUtcNow();
        job.Status = status;
        job.UpdatedAt = now;
        service.Status = status == "online" ? "active" : status;
        service.UpdatedAt = now;

        if (result is not null)
        {
            job.SelectedNodeId = result.NodeId;
            job.PterodactylServerId = result.ServerId;
            job.PterodactylServerUuid = result.ServerUuid;
            service.PterodactylNodeId = result.NodeId;
            service.PterodactylServerId = result.ServerId;
            service.PterodactylServerUuid = result.ServerUuid;
        }

        if (status == "online")
        {
            job.CompletedAt = now;
            service.ActivatedAt ??= now;
        }
        else if (status == "failed")
        {
            job.CompletedAt = now;
            job.LastError = error;
        }

        AddEvent(db, job, service, eventType, message, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddEvent(
        CommerceDbContext db,
        ProvisioningJobRecord job,
        HostingServiceRecord service,
        string eventType,
        string message,
        DateTimeOffset now) => db.DeploymentEvents.Add(new DeploymentEventRecord
        {
            Id = Guid.NewGuid(),
            ProvisioningJobId = job.Id,
            HostingServiceId = service.Id,
            EventType = eventType,
            Message = message,
            MetadataJson = "{}",
            CreatedAt = now
        });
}
