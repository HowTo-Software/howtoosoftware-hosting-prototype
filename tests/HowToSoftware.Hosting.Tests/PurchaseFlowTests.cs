using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using HowToSoftware.Hosting.Localization;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services;
using HowToSoftware.Hosting.Services.Orders;
using HowToSoftware.Hosting.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The purchase flow from the server's side: the browser sends a game, a plan and a period; the
/// server prices it, records it, hands Stripe exactly that figure, and only fulfils what Stripe
/// confirms at that figure.
/// </summary>
public sealed class PurchaseFlowTests : IDisposable
{
    private readonly CultureScope _culture = new(SupportedCultures.Default);
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hts-tests-{Guid.NewGuid():N}.db");
    private readonly EfOrderStore _orders;
    private readonly OrderPricingService _pricing;
    private readonly FakeStripeGateway _stripe = new();
    private readonly OrderFulfillmentQueue _queue = new();
    private readonly StripeOptions _stripeOptions = new();

    public PurchaseFlowTests()
    {
        var factory = new FileContextFactory(_dbPath);
        using (var db = factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        _orders = new EfOrderStore(factory);

        var plans = new StaticPlanCatalogService(
            TestLocalizer.For<HomeText>(),
            Options.Create(new HostingPlanPricingOptions
            {
                Rates = new PlanRateCard
                {
                    CpuPer100Percent = "0.90",
                    MemoryPerGb = "1.20",
                    DiskPerBlock = "0.30",
                    DiskBlockGb = 20
                }
            }));
        var games = new StaticGameCatalogService(TestLocalizer.For<CheckoutText>(), plans);

        _pricing = new OrderPricingService(games, plans);
    }

    public void Dispose()
    {
        _culture.Dispose();

        try
        {
            System.IO.File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Another handle may still be closing; a stray temp file is not a test failure.
        }
    }

    private StripeCheckoutService Checkout() => new(
        _stripe,
        _pricing,
        _orders,
        _queue,
        new StaticOptionsMonitor<StripeOptions>(_stripeOptions),
        TimeProvider.System,
        NullLogger<StripeCheckoutService>.Instance);

    private static CheckoutRequest Request(
        string game = "project-zomboid",
        string plan = "zomboid-4gb",
        BillingPeriod period = BillingPeriod.Quarterly) =>
        new(game, plan, period, "https://hts.example", UserId: null, Locale: "pt-BR");

    // ── Pricing ────────────────────────────────────────────────────────────

    [Fact]
    public void TheServerPricesThePlan_FromTheCatalogueAndThePolicy()
    {
        var priced = _pricing.Price("project-zomboid", "zomboid-4gb", BillingPeriod.Quarterly);

        Assert.NotNull(priced);
        Assert.Equal("usd", priced.CurrencyCode);
        Assert.Equal(7.99m, priced.Quote.MonthlyPrice);
        Assert.Equal(22.77m, priced.Quote.FinalAmount);
        Assert.Equal(5, priced.Quote.DiscountPercent);
    }

    [Theory]
    [InlineData("minecraft", "zomboid-4gb")]      // planned game
    [InlineData("project-zomboid", "zomboid-99gb")] // unknown plan
    [InlineData("palworld", "zomboid-4gb")]       // unknown game
    [InlineData("", "")]
    public void NothingThatIsNotForSaleCanBePriced(string game, string plan)
    {
        Assert.Null(_pricing.Price(game, plan, BillingPeriod.Monthly));
    }

    // ── Creating the session ───────────────────────────────────────────────

    [Fact]
    public async Task CheckoutRecordsAPendingOrder_AndHandsStripeTheServersFigure()
    {
        var result = await Checkout().CreateCheckoutSessionAsync(Request());

        Assert.Equal(CheckoutOutcome.Redirect, result.Outcome);
        Assert.True(result.IsRedirect);
        Assert.Equal(_stripe.LastSession!.Url, result.RedirectUrl);

        var order = await _orders.FindAsync(result.OrderId!.Value);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal("project-zomboid", order.GameId);
        Assert.Equal("zomboid-4gb", order.PlanId);
        Assert.Equal(BillingPeriod.Quarterly, order.BillingPeriod);
        Assert.Equal(7.99m, order.MonthlyPrice);
        Assert.Equal(23.97m, order.BaseAmount);
        Assert.Equal(5, order.DiscountPercentage);
        Assert.Equal(1.20m, order.DiscountAmount);
        Assert.Equal(22.77m, order.FinalAmount);
        Assert.Equal("usd", order.Currency);
        Assert.Equal(_stripe.LastSession.Id, order.StripeCheckoutSessionId);

        var options = _stripe.LastOptions!;
        Assert.Equal("subscription", options.Mode);
        Assert.Equal(order.Id.ToString("D"), options.ClientReferenceId);

        var line = Assert.Single(options.LineItems);
        Assert.Null(line.Price);
        Assert.Equal(1, line.Quantity);
        Assert.Equal("usd", line.PriceData!.Currency);
        Assert.Equal(2277, line.PriceData.UnitAmount);
        Assert.Equal("month", line.PriceData.Recurring!.Interval);
        Assert.Equal(3, line.PriceData.Recurring.IntervalCount);
        Assert.Contains("Outpost", line.PriceData.ProductData!.Name, StringComparison.Ordinal);

        Assert.Equal(order.Id.ToString("D"), options.Metadata[StripeCheckoutService.MetadataKeys.OrderId]);
        Assert.Equal("project-zomboid", options.Metadata[StripeCheckoutService.MetadataKeys.GameId]);
        Assert.Equal("zomboid-4gb", options.Metadata[StripeCheckoutService.MetadataKeys.PlanId]);
        Assert.Equal("quarterly", options.Metadata[StripeCheckoutService.MetadataKeys.BillingPeriod]);
        Assert.False(options.Metadata.ContainsKey(StripeCheckoutService.MetadataKeys.UserId));
        Assert.Equal(options.Metadata, options.SubscriptionData!.Metadata);

        Assert.Equal("https://hts.example/payment/success?session_id={CHECKOUT_SESSION_ID}", options.SuccessUrl);
        Assert.Equal("https://hts.example/payment/cancel?game=project-zomboid&plan=zomboid-4gb&period=quarterly", options.CancelUrl);
        Assert.Equal("pt-BR", options.Locale);
    }

    [Fact]
    public async Task AnnualIsBilledEveryTwelveMonths_AtTenPercentOff()
    {
        await Checkout().CreateCheckoutSessionAsync(Request(period: BillingPeriod.Annual));

        var line = Assert.Single(_stripe.LastOptions!.LineItems);
        Assert.Equal(8629, line.PriceData!.UnitAmount);
        Assert.Equal(12, line.PriceData.Recurring!.IntervalCount);
    }

    [Fact]
    public async Task MonthlyIsBilledEveryMonth_AtListPrice()
    {
        await Checkout().CreateCheckoutSessionAsync(Request(period: BillingPeriod.Monthly));

        var line = Assert.Single(_stripe.LastOptions!.LineItems);
        Assert.Equal(799, line.PriceData!.UnitAmount);
        Assert.Equal(1, line.PriceData.Recurring!.IntervalCount);
    }

    [Fact]
    public async Task AConfiguredPriceIdIsUsedInsteadOfAnInlinePrice()
    {
        _stripeOptions.PriceIds["zomboid-4gb:quarterly"] = "price_configured";

        await Checkout().CreateCheckoutSessionAsync(Request());

        var line = Assert.Single(_stripe.LastOptions!.LineItems);
        Assert.Equal("price_configured", line.Price);
        Assert.Null(line.PriceData);
    }

    [Fact]
    public async Task ConfiguredReturnUrlsWin_AndPublicSlugsAreSubstituted()
    {
        _stripeOptions.SuccessUrl = "https://howtoosoftware.com/payment/success?session_id={CHECKOUT_SESSION_ID}";
        _stripeOptions.CancelUrl = "https://howtoosoftware.com/payment/cancel?game={GAME_SLUG}&plan={PLAN_SLUG}&period={BILLING_PERIOD}";

        var result = await Checkout().CreateCheckoutSessionAsync(Request());

        Assert.Equal(_stripeOptions.SuccessUrl, _stripe.LastOptions!.SuccessUrl);
        Assert.Equal("https://howtoosoftware.com/payment/cancel?game=project-zomboid&plan=zomboid-4gb&period=quarterly", _stripe.LastOptions.CancelUrl);
    }

    [Fact]
    public async Task AnUntrustedReturnOriginCannotCreateACheckoutSession()
    {
        var request = Request() with { ReturnOrigin = "http://attacker.example" };

        var result = await Checkout().CreateCheckoutSessionAsync(request);

        Assert.Equal(CheckoutOutcome.Rejected, result.Outcome);
        Assert.Null(_stripe.LastOptions);
    }

    [Fact]
    public async Task WithoutASecretKey_NothingIsCreatedAndTheButtonIsOff()
    {
        _stripe.IsConfigured = false;

        var service = Checkout();
        var result = await service.CreateCheckoutSessionAsync(Request());

        Assert.False(service.IsConfigured);
        Assert.Equal(CheckoutOutcome.NotConfigured, result.Outcome);
        Assert.Null(result.OrderId);
        Assert.Null(_stripe.LastOptions);
    }

    [Theory]
    [InlineData("minecraft", "zomboid-4gb")]
    [InlineData("project-zomboid", "zomboid-99gb")]
    public async Task AnUnpricedRequestIsRejected_BeforeAnOrderExists(string game, string plan)
    {
        var result = await Checkout().CreateCheckoutSessionAsync(Request(game, plan));

        Assert.Equal(CheckoutOutcome.Rejected, result.Outcome);
        Assert.Null(result.OrderId);
        Assert.Null(_stripe.LastOptions);
    }

    [Fact]
    public async Task WhenStripeRefuses_TheOrderIsMarkedFailed_AndNothingIsCharged()
    {
        _stripe.Throw = new StripeException("boom") { StripeError = new StripeError { Code = "resource_missing" } };

        var result = await Checkout().CreateCheckoutSessionAsync(Request());

        Assert.Equal(CheckoutOutcome.Failed, result.Outcome);
        var order = await _orders.FindAsync(result.OrderId!.Value);
        Assert.Equal(OrderStatus.Failed, order!.Status);
        Assert.Contains("resource_missing", order.FailureReason, StringComparison.Ordinal);
        Assert.DoesNotContain("sk_", order.FailureReason, StringComparison.Ordinal);
    }

    // ── The webhook's side ─────────────────────────────────────────────────

    private async Task<Order> PendingOrderAsync()
    {
        var result = await Checkout().CreateCheckoutSessionAsync(Request());
        return (await _orders.FindAsync(result.OrderId!.Value))!;
    }

    private static Session Completed(Order order, long amount = 2277, string currency = "usd", string paymentStatus = "paid") => new()
    {
        Id = order.StripeCheckoutSessionId,
        ClientReferenceId = order.Id.ToString("D"),
        PaymentStatus = paymentStatus,
        AmountTotal = amount,
        Currency = currency,
        CustomerId = "cus_test",
        SubscriptionId = "sub_test",
        CustomerDetails = new SessionCustomerDetails { Email = "survivor@example.com" }
    };

    [Fact]
    public async Task AVerifiedPaidSessionMarksTheOrderPaid_AndQueuesFulfilment()
    {
        var order = await PendingOrderAsync();

        await Checkout().HandleCompletedCheckoutAsync(Completed(order));

        var paid = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Paid, paid.Status);
        Assert.NotNull(paid.PaidAt);
        Assert.Equal("survivor@example.com", paid.CustomerEmail);
        Assert.Equal("cus_test", paid.StripeCustomerId);
        Assert.Equal("sub_test", paid.StripeSubscriptionId);
        Assert.Equal([order.Id], await DrainAsync(_queue));
    }

    [Fact]
    public async Task ThePaidAmountMustMatchTheServersFigure()
    {
        var order = await PendingOrderAsync();

        await Checkout().HandleCompletedCheckoutAsync(Completed(order, amount: 799));

        var failed = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Failed, failed.Status);
        Assert.Contains("does not match", failed.FailureReason, StringComparison.Ordinal);
        Assert.Empty(await DrainAsync(_queue));
    }

    [Fact]
    public async Task ThePaidCurrencyMustMatchToo()
    {
        var order = await PendingOrderAsync();

        await Checkout().HandleCompletedCheckoutAsync(Completed(order, currency: "brl"));

        Assert.Equal(OrderStatus.Failed, (await _orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task ASessionCompletedBeforeTheMoneyArrivedStaysPending()
    {
        var order = await PendingOrderAsync();

        await Checkout().HandleCompletedCheckoutAsync(Completed(order, paymentStatus: "unpaid"));

        Assert.Equal(OrderStatus.Pending, (await _orders.FindAsync(order.Id))!.Status);
        Assert.Empty(await DrainAsync(_queue));
    }

    [Fact]
    public async Task ARedeliveredPaidSessionDoesNotFulfilTwice()
    {
        var order = await PendingOrderAsync();
        var service = Checkout();

        await service.HandleCompletedCheckoutAsync(Completed(order));
        await service.HandleCompletedCheckoutAsync(Completed(order));

        Assert.Equal(OrderStatus.Paid, (await _orders.FindAsync(order.Id))!.Status);
        Assert.Single(await DrainAsync(_queue));
    }

    [Fact]
    public async Task AnUnknownSessionIsIgnored()
    {
        await Checkout().HandleCompletedCheckoutAsync(new Session
        {
            Id = "cs_unknown",
            ClientReferenceId = Guid.NewGuid().ToString("D"),
            PaymentStatus = "paid",
            AmountTotal = 2277,
            Currency = "usd"
        });

        Assert.Empty(await DrainAsync(_queue));
    }

    [Fact]
    public async Task AnExpiredSessionCancelsThePendingOrder_ButKeepsItsConfiguration()
    {
        var order = await PendingOrderAsync();

        await Checkout().HandleExpiredCheckoutAsync(Completed(order, paymentStatus: "unpaid"));

        var cancelled = (await _orders.FindAsync(order.Id))!;
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal("zomboid-4gb", cancelled.PlanId);
        Assert.Equal(BillingPeriod.Quarterly, cancelled.BillingPeriod);
    }

    [Fact]
    public async Task ACancelledOrderCanStillBePaid_BecauseStripeOutranksOurExpiry()
    {
        var order = await PendingOrderAsync();
        var service = Checkout();

        await service.HandleExpiredCheckoutAsync(Completed(order, paymentStatus: "unpaid"));
        await service.HandleCompletedCheckoutAsync(Completed(order));

        Assert.Equal(OrderStatus.Paid, (await _orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task AnExpiredSessionNeverUndoesAPayment()
    {
        var order = await PendingOrderAsync();
        var service = Checkout();

        await service.HandleCompletedCheckoutAsync(Completed(order));
        await service.HandleExpiredCheckoutAsync(Completed(order, paymentStatus: "unpaid"));

        Assert.Equal(OrderStatus.Paid, (await _orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task SubscriptionEventsMirrorStripesStatusOntoTheOrder()
    {
        var order = await PendingOrderAsync();
        var service = Checkout();
        await service.HandleCompletedCheckoutAsync(Completed(order));

        await service.HandleSubscriptionStateAsync(new Subscription { Id = "sub_test", Status = "past_due" });

        Assert.Equal("past_due", (await _orders.FindAsync(order.Id))!.SubscriptionStatus);
    }

    [Fact]
    public void ValidateOrderPriceComparesMinorUnitsAndCurrency()
    {
        var service = Checkout();
        var order = new Order { GameId = "g", PlanId = "p", PlanName = "P", Currency = "usd", FinalAmount = 22.77m };

        Assert.True(service.ValidateOrderPrice(order, 2277, "usd"));
        Assert.True(service.ValidateOrderPrice(order, 2277, "USD"));
        Assert.False(service.ValidateOrderPrice(order, 2276, "usd"));
        Assert.False(service.ValidateOrderPrice(order, 2277, "brl"));
        Assert.False(service.ValidateOrderPrice(order, null, "usd"));
    }

    // ── The webhook router ─────────────────────────────────────────────────

    [Fact]
    public async Task ARedeliveredEventIsAcknowledgedAndNotActedOnAgain()
    {
        var order = await PendingOrderAsync();
        var handler = new StripeWebhookHandler(Checkout(), _orders, NullLogger<StripeWebhookHandler>.Instance);
        var evt = new Event
        {
            Id = "evt_once",
            Type = EventTypes.CheckoutSessionCompleted,
            Data = new EventData { Object = Completed(order) }
        };

        Assert.True(await handler.HandleAsync(evt));
        Assert.False(await handler.HandleAsync(evt));

        Assert.Equal(OrderStatus.Paid, (await _orders.FindAsync(order.Id))!.Status);
        Assert.Single(await DrainAsync(_queue));
    }

    [Fact]
    public async Task AnUnrelatedEventTypeIsAcknowledgedWithoutBeingRecorded()
    {
        var handler = new StripeWebhookHandler(Checkout(), _orders, NullLogger<StripeWebhookHandler>.Instance);

        Assert.False(await handler.HandleAsync(new Event { Id = "evt_other", Type = "charge.refunded" }));
        Assert.True(await _orders.TryRecordEventAsync("evt_other", "charge.refunded"));
    }

    // ── The store ──────────────────────────────────────────────────────────

    [Fact]
    public async Task TheStoreRefusesToRecordTheSameEventTwice()
    {
        Assert.True(await _orders.TryRecordEventAsync("evt_1", "checkout.session.completed"));
        Assert.False(await _orders.TryRecordEventAsync("evt_1", "checkout.session.completed"));
    }

    [Fact]
    public async Task OnlyPaidAndUnprovisionedOrdersAwaitFulfilment()
    {
        var pending = await PendingOrderAsync();
        var paid = await PendingOrderAsync();
        var service = Checkout();
        await service.HandleCompletedCheckoutAsync(Completed(paid));

        Assert.Equal([paid.Id], await _orders.ListAwaitingFulfilmentAsync());
        Assert.NotEqual(pending.Id, paid.Id);
    }

    // ── Order state machine ────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Provisioning, true)]
    [InlineData(OrderStatus.Provisioning, OrderStatus.Active, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Active, OrderStatus.Failed, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Active, false)]
    public void TransitionsFollowTheLifeOfAnOrder(OrderStatus from, OrderStatus to, bool allowed)
    {
        var order = new Order { GameId = "g", PlanId = "p", PlanName = "P", Currency = "usd", Status = from };

        Assert.Equal(allowed, order.CanTransitionTo(to));

        if (!allowed)
        {
            Assert.Throws<InvalidOperationException>(() => order.TransitionTo(to, DateTimeOffset.UtcNow));
        }
    }

    [Fact]
    public void TheSameEmailAlwaysYieldsTheSameCustomerId()
    {
        Assert.Equal(CustomerIdentity.FromEmail("Survivor@Example.com"), CustomerIdentity.FromEmail("  survivor@example.com "));
        Assert.NotEqual(CustomerIdentity.FromEmail("a@example.com"), CustomerIdentity.FromEmail("b@example.com"));
    }

    private static async Task<List<Guid>> DrainAsync(OrderFulfillmentQueue queue)
    {
        var ids = new List<Guid>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        try
        {
            await foreach (var id in queue.ReadAllAsync(cts.Token))
            {
                ids.Add(id);
            }
        }
        catch (OperationCanceledException)
        {
            // The queue never completes; the timeout is how "nothing else" is observed.
        }

        return ids;
    }

    // ── Doubles ────────────────────────────────────────────────────────────

    private sealed class FileContextFactory(string path) : IDbContextFactory<HostingDbContext>
    {
        private readonly DbContextOptions<HostingDbContext> _options =
            new DbContextOptionsBuilder<HostingDbContext>().UseSqlite($"Data Source={path}").Options;

        public HostingDbContext CreateDbContext() => new(_options);
    }

    private sealed class FakeStripeGateway : IStripeGateway
    {
        public bool IsConfigured { get; set; } = true;

        public SessionCreateOptions? LastOptions { get; private set; }

        public Session? LastSession { get; private set; }

        public StripeException? Throw { get; set; }

        public Task<Session> CreateCheckoutSessionAsync(SessionCreateOptions options, CancellationToken cancellationToken = default)
        {
            LastOptions = options;

            if (Throw is { } exception)
            {
                throw exception;
            }

            LastSession = new Session
            {
                Id = $"cs_test_{Guid.NewGuid():N}",
                Url = "https://checkout.stripe.com/c/pay/cs_test"
            };

            return Task.FromResult(LastSession);
        }

        public Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Session { Id = sessionId });

        public Event ConstructEvent(string json, string signatureHeader) => throw new NotSupportedException();
    }
}

/// <summary>An <see cref="IOptionsMonitor{T}"/> that hands back one instance and never changes.</summary>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
