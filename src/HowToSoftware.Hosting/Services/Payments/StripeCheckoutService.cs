using global::Stripe;
using global::Stripe.Checkout;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Orders;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Services.Payments;

/// <summary>
/// What the review page sends when the customer continues to payment. Note what is absent: a
/// price. The server works that out.
/// </summary>
/// <param name="GameSlug">The game.</param>
/// <param name="PlanSlug">The plan.</param>
/// <param name="Period">The billing period.</param>
/// <param name="ReturnOrigin">
/// Scheme and host of the site as the customer reached it (e.g. <c>https://howtoosoftware.com</c>),
/// used to build the return URLs when none are configured.
/// </param>
/// <param name="UserId">The authenticated user, when there is one.</param>
/// <param name="Locale">The customer's culture, so Checkout speaks the same language as the site.</param>
public sealed record CheckoutRequest(
    string GameSlug,
    string PlanSlug,
    BillingPeriod Period,
    string ReturnOrigin,
    string? UserId,
    string? Locale);

/// <summary>How an attempt to start Checkout ended.</summary>
public enum CheckoutOutcome
{
    /// <summary>A session exists; send the customer to <see cref="CheckoutStart.RedirectUrl"/>.</summary>
    Redirect,

    /// <summary>No Stripe secret key on this host. The button says so instead of failing.</summary>
    NotConfigured,

    /// <summary>The game, plan or period did not resolve to something for sale.</summary>
    Rejected,

    /// <summary>Stripe declined to create the session.</summary>
    Failed
}

/// <summary>Result of starting Checkout.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="OrderId">The order created, when one was.</param>
/// <param name="RedirectUrl">The Stripe-hosted Checkout page, on success.</param>
public sealed record CheckoutStart(CheckoutOutcome Outcome, Guid? OrderId, string? RedirectUrl)
{
    /// <summary>Whether the customer can be sent on to Stripe.</summary>
    public bool IsRedirect => Outcome is CheckoutOutcome.Redirect && RedirectUrl is not null;
}

/// <summary>
/// The application's side of the Stripe integration.
/// </summary>
/// <remarks>
/// Nothing in a Razor component talks to Stripe. The review page calls
/// <see cref="CreateCheckoutSessionAsync"/>; the webhook calls the two handlers; the success
/// page calls <see cref="GetCheckoutSessionAsync"/>. The price check is public so the webhook
/// handler and its tests can call it directly.
/// </remarks>
public interface IStripeCheckoutService
{
    /// <summary>Whether Checkout sessions can be created on this host.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Prices the request on the server, records a pending order, and creates a Stripe Checkout
    /// Session for it.
    /// </summary>
    Task<CheckoutStart> CreateCheckoutSessionAsync(CheckoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads a Checkout Session from Stripe, or <see langword="null"/> when it cannot be.</summary>
    Task<Session?> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acts on a completed Checkout Session delivered by a verified webhook: checks the amount,
    /// records the customer and subscription, marks the order paid and queues fulfilment.
    /// </summary>
    Task HandleCompletedCheckoutAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>Acts on an expired Checkout Session: the order is cancelled, its configuration kept.</summary>
    Task HandleExpiredCheckoutAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>Mirrors a subscription's Stripe status onto its order.</summary>
    Task HandleSubscriptionStateAsync(Subscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether what Stripe reports as paid is exactly what the server told it to charge.
    /// </summary>
    /// <param name="order">The order, carrying the amount the server computed.</param>
    /// <param name="amountTotalMinor">Stripe's <c>amount_total</c>, in the minor unit.</param>
    /// <param name="currency">Stripe's currency code.</param>
    bool ValidateOrderPrice(Order order, long? amountTotalMinor, string? currency);
}

/// <summary>
/// <see cref="IStripeCheckoutService"/> over <see cref="IStripeGateway"/>.
/// </summary>
public sealed class StripeCheckoutService : IStripeCheckoutService
{
    /// <summary>Metadata keys, identical on the session and the subscription it creates.</summary>
    public static class MetadataKeys
    {
        public const string OrderId = "internal_order_id";
        public const string UserId = "user_id";
        public const string GameId = "game_id";
        public const string PlanId = "plan_id";
        public const string BillingPeriod = "billing_period";
    }

    private readonly IStripeGateway _stripe;
    private readonly IOrderPricingService _pricing;
    private readonly IOrderStore _orders;
    private readonly OrderFulfillmentQueue _fulfilment;
    private readonly IOptionsMonitor<StripeOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<StripeCheckoutService> _logger;

    /// <summary>Creates the service.</summary>
    public StripeCheckoutService(
        IStripeGateway stripe,
        IOrderPricingService pricing,
        IOrderStore orders,
        OrderFulfillmentQueue fulfilment,
        IOptionsMonitor<StripeOptions> options,
        TimeProvider clock,
        ILogger<StripeCheckoutService> logger)
    {
        _stripe = stripe;
        _pricing = pricing;
        _orders = orders;
        _fulfilment = fulfilment;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured => _stripe.IsConfigured;

    /// <inheritdoc />
    public async Task<CheckoutStart> CreateCheckoutSessionAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_stripe.IsConfigured)
        {
            return new CheckoutStart(CheckoutOutcome.NotConfigured, null, null);
        }

        // The server prices the order. Whatever the browser thought the plan cost is not an
        // input to anything below this line.
        if (_pricing.Price(request.GameSlug, request.PlanSlug, request.Period) is not { } priced)
        {
            _logger.LogWarning(
                "Checkout rejected: {Game}/{Plan}/{Period} does not resolve to a priced plan.",
                request.GameSlug, request.PlanSlug, request.Period);

            return new CheckoutStart(CheckoutOutcome.Rejected, null, null);
        }

        var now = _clock.GetUtcNow();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            GameId = priced.Game.Slug,
            PlanId = priced.Plan.Slug,
            PlanName = priced.Plan.Name,
            BillingPeriod = priced.Quote.Period,
            MonthlyPrice = priced.Quote.MonthlyPrice,
            BaseAmount = priced.Quote.BaseAmount,
            DiscountPercentage = priced.Quote.DiscountPercent,
            DiscountAmount = priced.Quote.DiscountAmount,
            FinalAmount = priced.Quote.FinalAmount,
            Currency = priced.CurrencyCode,
            Status = OrderStatus.Pending,
            ProvisioningStage = FulfilmentStage.NotStarted,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _orders.AddAsync(order, cancellationToken).ConfigureAwait(false);

        var sessionOptions = BuildSessionOptions(order, priced, request);

        try
        {
            var session = await _stripe.CreateCheckoutSessionAsync(sessionOptions, cancellationToken).ConfigureAwait(false);

            order.StripeCheckoutSessionId = session.Id;
            order.UpdatedAt = _clock.GetUtcNow();
            await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Order {OrderId}: Checkout session created for {Plan}/{Period} at {Amount} {Currency}.",
                order.Id, order.PlanId, order.BillingPeriod, order.FinalAmount, order.Currency);

            return new CheckoutStart(CheckoutOutcome.Redirect, order.Id, session.Url);
        }
        catch (StripeException exception)
        {
            // The exception message describes the request Stripe rejected; it never carries a
            // key. Logged as the type and Stripe's own error code, which is what an operator
            // needs and nothing a customer would.
            order.TransitionTo(OrderStatus.Failed, _clock.GetUtcNow());
            order.FailureReason = $"Stripe rejected the Checkout session ({exception.StripeError?.Code ?? exception.GetType().Name}).";
            await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

            _logger.LogError(
                exception,
                "Order {OrderId}: Stripe refused to create a Checkout session (code {Code}).",
                order.Id, exception.StripeError?.Code);

            return new CheckoutStart(CheckoutOutcome.Failed, order.Id, null);
        }
    }

    /// <inheritdoc />
    public async Task<Session?> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!_stripe.IsConfigured || string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        try
        {
            return await _stripe.GetCheckoutSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
        catch (StripeException exception)
        {
            _logger.LogWarning(exception, "Checkout session lookup failed (code {Code}).", exception.StripeError?.Code);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task HandleCompletedCheckoutAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var order = await FindOrderForSessionAsync(session, cancellationToken).ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogWarning("checkout.session.completed for an unknown session; nothing to fulfil.");
            return;
        }

        // Delayed payment methods complete the session before the money arrives. Stripe sends
        // checkout.session.async_payment_succeeded later; until then the order stays pending.
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Order {OrderId}: session completed with payment_status={Status}; waiting for payment.",
                order.Id, session.PaymentStatus);
            return;
        }

        var now = _clock.GetUtcNow();

        if (!ValidateOrderPrice(order, session.AmountTotal, session.Currency))
        {
            if (order.CanTransitionTo(OrderStatus.Failed))
            {
                order.TransitionTo(OrderStatus.Failed, now);
            }

            order.FailureReason =
                $"Paid amount {session.AmountTotal} {session.Currency} does not match the order's {order.FinalAmountMinorUnits()} {order.Currency}.";
            await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

            _logger.LogError(
                "Order {OrderId}: amount mismatch - Stripe reports {Paid} {PaidCurrency}, order expects {Expected} {Currency}. Not fulfilling.",
                order.Id, session.AmountTotal, session.Currency, order.FinalAmountMinorUnits(), order.Currency);
            return;
        }

        // Redeliveries of a paid session, or a session completing after the order already moved
        // on, must not throw and must not re-queue fulfilment.
        if (!order.CanTransitionTo(OrderStatus.Paid))
        {
            _logger.LogInformation("Order {OrderId} is already {Status}; completed session ignored.", order.Id, order.Status);
            return;
        }

        order.CustomerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail ?? order.CustomerEmail;
        order.StripeCustomerId = session.CustomerId ?? order.StripeCustomerId;
        order.StripeSubscriptionId = session.SubscriptionId ?? order.StripeSubscriptionId;
        order.StripeCheckoutSessionId ??= session.Id;
        order.TransitionTo(OrderStatus.Paid, now);

        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Order {OrderId}: paid. Queued for provisioning.", order.Id);

        _fulfilment.Enqueue(order.Id);
    }

    /// <inheritdoc />
    public async Task HandleExpiredCheckoutAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var order = await FindOrderForSessionAsync(session, cancellationToken).ConfigureAwait(false);

        if (order is null || !order.CanTransitionTo(OrderStatus.Cancelled))
        {
            return;
        }

        // Cancelled, not deleted: the plan and period are kept so the cancel page can offer
        // to try again with the same configuration.
        order.TransitionTo(OrderStatus.Cancelled, _clock.GetUtcNow());
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Order {OrderId}: Checkout session expired; order cancelled.", order.Id);
    }

    /// <inheritdoc />
    public async Task HandleSubscriptionStateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var order = await _orders.FindBySubscriptionAsync(subscription.Id, cancellationToken).ConfigureAwait(false);

        if (order is null
            && subscription.Metadata is { } metadata
            && metadata.TryGetValue(MetadataKeys.OrderId, out var raw)
            && Guid.TryParse(raw, out var orderId))
        {
            order = await _orders.FindAsync(orderId, cancellationToken).ConfigureAwait(false);
        }

        if (order is null)
        {
            _logger.LogWarning("Subscription event for an unknown subscription; ignored.");
            return;
        }

        order.StripeSubscriptionId ??= subscription.Id;
        order.SubscriptionStatus = subscription.Status;
        order.UpdatedAt = _clock.GetUtcNow();
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Order {OrderId}: subscription status is now {Status}.", order.Id, subscription.Status);
    }

    /// <inheritdoc />
    public bool ValidateOrderPrice(Order order, long? amountTotalMinor, string? currency)
    {
        ArgumentNullException.ThrowIfNull(order);

        return amountTotalMinor is { } paid
            && paid == order.FinalAmountMinorUnits()
            && string.Equals(currency, order.Currency, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Order?> FindOrderForSessionAsync(Session session, CancellationToken cancellationToken)
    {
        var order = await _orders.FindByCheckoutSessionAsync(session.Id, cancellationToken).ConfigureAwait(false);

        if (order is not null)
        {
            return order;
        }

        // The session id is written to the order as soon as Stripe returns it, but a webhook can
        // race that write. client_reference_id is set at creation and cannot race anything.
        return Guid.TryParse(session.ClientReferenceId, out var orderId)
            ? await _orders.FindAsync(orderId, cancellationToken).ConfigureAwait(false)
            : null;
    }

    private SessionCreateOptions BuildSessionOptions(Order order, PricedPlan priced, CheckoutRequest request)
    {
        var options = _options.CurrentValue;
        var periodSlug = BillingPolicy.Slug(order.BillingPeriod);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MetadataKeys.OrderId] = order.Id.ToString("D"),
            [MetadataKeys.GameId] = order.GameId,
            [MetadataKeys.PlanId] = order.PlanId,
            [MetadataKeys.BillingPeriod] = periodSlug
        };

        if (!string.IsNullOrWhiteSpace(order.UserId))
        {
            metadata[MetadataKeys.UserId] = order.UserId;
        }

        var lineItem = new SessionLineItemOptions { Quantity = 1 };

        if (options.FindPriceId(order.PlanId, periodSlug) is { } priceId)
        {
            // A Price created in the dashboard for this plan and period. The webhook still
            // checks the paid amount against the server's figure, so a dashboard price that
            // drifts from the rate card is caught rather than honoured.
            lineItem.Price = priceId;
        }
        else
        {
            lineItem.PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = order.Currency,
                UnitAmount = priced.Quote.FinalAmountMinor,
                Recurring = new SessionLineItemPriceDataRecurringOptions
                {
                    Interval = "month",
                    IntervalCount = priced.Quote.Months
                },
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"{priced.Game.Name} - {priced.Plan.Name}",
                    Description = DescribePlan(priced),
                    Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [MetadataKeys.GameId] = order.GameId,
                        [MetadataKeys.PlanId] = order.PlanId
                    }
                }
            };
        }

        var successUrl = string.IsNullOrWhiteSpace(options.SuccessUrl)
            ? $"{request.ReturnOrigin.TrimEnd('/')}{SiteRoutes.PaymentSuccess}?session_id={{CHECKOUT_SESSION_ID}}"
            : options.SuccessUrl;

        var cancelUrl = (string.IsNullOrWhiteSpace(options.CancelUrl)
            ? $"{request.ReturnOrigin.TrimEnd('/')}{SiteRoutes.PaymentCancel}?order={{ORDER_ID}}"
            : options.CancelUrl)
            .Replace("{ORDER_ID}", order.Id.ToString("D"), StringComparison.Ordinal);

        return new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems = [lineItem],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = order.Id.ToString("D"),
            Metadata = metadata,
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
            Locale = ToStripeLocale(request.Locale)
        };
    }

    private static string DescribePlan(PricedPlan priced)
    {
        var plan = priced.Plan;

        return $"{plan.MemoryGb} GB RAM, {plan.CpuPercent}% CPU allocation, {plan.DiskGb} GB NVMe. "
            + $"Billed every {priced.Quote.Months} month(s).";
    }

    /// <summary>Stripe accepts a short list of locales; anything else falls back to auto-detect.</summary>
    private static string ToStripeLocale(string? locale) =>
        locale?.Trim().ToLowerInvariant() switch
        {
            "pt-br" or "pt" => "pt-BR",
            "en" or "en-us" or "en-gb" => "en",
            _ => "auto"
        };
}

/// <summary>Money helpers for orders.</summary>
public static class OrderMoney
{
    /// <summary>The order's final amount in the currency's minor unit, as Stripe reports it.</summary>
    public static long FinalAmountMinorUnits(this Order order) =>
        (long)Math.Round(order.FinalAmount * 100m, MidpointRounding.AwayFromZero);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
