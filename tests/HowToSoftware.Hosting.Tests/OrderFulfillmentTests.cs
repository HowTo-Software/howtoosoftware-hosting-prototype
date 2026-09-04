using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Models;
using HowToSoftware.Hosting.Infrastructure.Pterodactyl.Requests;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Orders;
using HowToSoftware.Hosting.Services.Provisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The joint between a paid order and the existing Pterodactyl provisioning service.
/// </summary>
/// <remarks>
/// Nothing here talks to a panel. The provisioner and the panel client are fakes that answer
/// the way the real ones do, so what is under test is the order's journey - Paid to
/// Provisioning to Active - and the honesty of every stop on it.
/// </remarks>
public sealed class OrderFulfillmentTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hts-fulfil-{Guid.NewGuid():N}.db");
    private readonly EfOrderStore _orders;
    private readonly FakeProvisioner _provisioner = new();
    private readonly FakePanel _panel = new();
    private readonly PterodactylOptions _panelOptions = new() { BaseUrl = "https://panel.example", ApiKey = "ptla_not_a_real_key" };
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));

    public OrderFulfillmentTests()
    {
        var options = new DbContextOptionsBuilder<HostingDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        using (var db = new HostingDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        _orders = new EfOrderStore(new Factory(options));
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    private OrderFulfillmentService Service() => new(
        _orders,
        _provisioner,
        _panel,
        new StaticOptionsMonitor<PterodactylOptions>(_panelOptions),
        _clock,
        NullLogger<OrderFulfillmentService>.Instance);

    private async Task<Order> PaidOrderAsync(string? email = "survivor@example.com")
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            GameId = "project-zomboid",
            PlanId = "zomboid-8gb",
            PlanName = "Knox Cell",
            BillingPeriod = BillingPeriod.Monthly,
            MonthlyPrice = 14.99m,
            BaseAmount = 14.99m,
            FinalAmount = 14.99m,
            Currency = "usd",
            Status = OrderStatus.Paid,
            CustomerEmail = email,
            StripeCheckoutSessionId = $"cs_{Guid.NewGuid():N}",
            CreatedAt = _clock.GetUtcNow(),
            UpdatedAt = _clock.GetUtcNow(),
            PaidAt = _clock.GetUtcNow()
        };

        await _orders.AddAsync(order);
        return order;
    }

    [Fact]
    public async Task APaidOrderBecomesAServer_AndGoesActiveWhenThePanelReportsItInstalled()
    {
        var order = await PaidOrderAsync();
        _panel.StatusSequence.Enqueue("installing");
        _panel.StatusSequence.Enqueue(null);

        var run = Service().FulfilAsync(order.Id);

        // The install poll waits on the test clock; advance it past two polls.
        await AdvanceUntilDoneAsync(run);

        var done = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Active, done.Status);
        Assert.Equal(FulfilmentStage.Online, done.ProvisioningStage);
        Assert.Equal("a1b2c3d4", done.ServerIdentifier);

        var request = Assert.Single(_provisioner.Requests);
        Assert.Equal(order.Id, request.RequestId);
        Assert.Equal("zomboid-8gb", request.PlanSlug);
        Assert.Equal("survivor@example.com", request.CustomerEmail);
        Assert.Equal(CustomerIdentity.FromEmail("survivor@example.com"), request.CustomerId);
        Assert.False(request.IsTest);
    }

    [Fact]
    public async Task AFailedInstallMarksTheOrderFailed_WithTheReasonAnOperatorNeeds()
    {
        var order = await PaidOrderAsync();
        _panel.StatusSequence.Enqueue("install_failed");

        await AdvanceUntilDoneAsync(Service().FulfilAsync(order.Id));

        var failed = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Failed, failed.Status);
        Assert.Equal(FulfilmentStage.Failed, failed.ProvisioningStage);
        Assert.Contains("install_failed", failed.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhenTheProvisionerRefuses_TheOrderIsFailedAndNothingIsRetriedBlindly()
    {
        var order = await PaidOrderAsync();
        _provisioner.Outcome = ProvisioningOutcome.Failed;

        await Service().FulfilAsync(order.Id);

        var failed = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Failed, failed.Status);
        Assert.Contains("Provisioning failed", failed.FailureReason, StringComparison.Ordinal);
        Assert.Equal(0, _panel.Lookups);
    }

    [Fact]
    public async Task WithoutAPanelConfigured_APaidOrderStaysPaidForAnOperator()
    {
        var order = await PaidOrderAsync();
        _panelOptions.ApiKey = string.Empty;

        await Service().FulfilAsync(order.Id);

        var untouched = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Paid, untouched.Status);
        Assert.Equal(FulfilmentStage.NotStarted, untouched.ProvisioningStage);
        Assert.Empty(_provisioner.Requests);
        Assert.Equal([order.Id], await _orders.ListAwaitingFulfilmentAsync());
    }

    [Fact]
    public async Task WithoutACustomerEmail_ThereIsNobodyToHandTheServerTo()
    {
        var order = await PaidOrderAsync(email: null);

        await Service().FulfilAsync(order.Id);

        var failed = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Failed, failed.Status);
        Assert.Empty(_provisioner.Requests);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Active)]
    [InlineData(OrderStatus.Failed)]
    public async Task OnlyAPaidOrderIsFulfilled(OrderStatus status)
    {
        var order = await PaidOrderAsync();
        order.Status = status;
        await _orders.UpdateAsync(order);

        await Service().FulfilAsync(order.Id);

        Assert.Equal(status, (await _orders.FindAsync(order.Id))!.Status);
        Assert.Empty(_provisioner.Requests);
    }

    [Fact]
    public async Task AnUnknownOrderIsIgnored()
    {
        await Service().FulfilAsync(Guid.NewGuid());

        Assert.Empty(_provisioner.Requests);
    }

    [Fact]
    public async Task AnInstallThatNeverFinishesIsRecordedAsFailed()
    {
        var order = await PaidOrderAsync();
        _panel.DefaultStatus = "installing";

        await AdvanceUntilDoneAsync(Service().FulfilAsync(order.Id), maxAdvance: OrderFulfillmentService.InstallTimeout + TimeSpan.FromMinutes(1));

        var waiting = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Failed, waiting.Status);
        Assert.Equal(FulfilmentStage.Failed, waiting.ProvisioningStage);
        Assert.Contains("did not finish", waiting.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(_panel.Lookups > 1);
    }

    /// <summary>
    /// Drives the fake clock forward one poll interval at a time until the fulfilment task
    /// completes, so the install wait costs no real time.
    /// </summary>
    private async Task AdvanceUntilDoneAsync(Task run, TimeSpan? maxAdvance = null)
    {
        var limit = maxAdvance ?? TimeSpan.FromMinutes(5);
        var advanced = TimeSpan.Zero;

        while (!run.IsCompleted && advanced < limit)
        {
            // Let the service reach its Task.Delay before the clock moves past it.
            await Task.Delay(10);
            _clock.Advance(OrderFulfillmentService.InstallPollInterval);
            advanced += OrderFulfillmentService.InstallPollInterval;
        }

        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    // ── Doubles ────────────────────────────────────────────────────────────

    private sealed class Factory(DbContextOptions<HostingDbContext> options) : IDbContextFactory<HostingDbContext>
    {
        public HostingDbContext CreateDbContext() => new(options);
    }

    private sealed class FakeProvisioner : IProvisioningService
    {
        public List<ProvisioningRequest> Requests { get; } = [];

        public ProvisioningOutcome Outcome { get; set; } = ProvisioningOutcome.Created;

        public Task<PanelSnapshot> InspectPanelAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProvisioningResult> ProvisionAsync(ProvisioningRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            var result = Outcome is ProvisioningOutcome.Failed
                ? ProvisioningResult.Failed(request.RequestId, request.PlanSlug, PterodactylFailure.PanelError, "the panel said no")
                : new ProvisioningResult(
                    Outcome, request.RequestId, 42, "uuid", "a1b2c3d4", $"hts-order:{request.RequestId:N}",
                    NodeId: 1, AllocationId: 7, PanelUserId: 3, UserWasReused: false, Status: "installing",
                    PlanSlug: request.PlanSlug, Failure: PterodactylFailure.None, Message: "created");

            return Task.FromResult(result);
        }

        public Task<ProvisioningResult> DeleteTestServerAsync(Guid requestId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakePanel : IPterodactylClient
    {
        public Queue<string?> StatusSequence { get; } = new();

        public string? DefaultStatus { get; set; }

        public int Lookups { get; private set; }

        public bool IsConfigured => true;

        public Task<PterodactylServer?> FindServerByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        {
            Lookups++;
            var status = StatusSequence.Count > 0 ? StatusSequence.Dequeue() : DefaultStatus;

            return Task.FromResult<PterodactylServer?>(new PterodactylServer
            {
                Id = 42,
                ExternalId = externalId,
                Identifier = "a1b2c3d4",
                Status = status
            });
        }

        public Task<IReadOnlyList<PterodactylNode>> GetNodesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<PterodactylLocation>> GetLocationsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PterodactylEgg> GetEggAsync(int nestId, int eggId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PterodactylUser?> FindUserByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PterodactylUser> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PterodactylServer> CreateServerAsync(CreateServerRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteServerAsync(int serverId, bool force = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
