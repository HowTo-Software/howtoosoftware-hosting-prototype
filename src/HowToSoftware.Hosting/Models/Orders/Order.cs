namespace HowToSoftware.Hosting.Models.Orders;

/// <summary>
/// Where an order is in its life.
/// </summary>
/// <remarks>
/// Stored by numeric value; do not reorder. The transitions that are allowed between these are
/// enforced by <see cref="Order"/> itself, not by whoever holds a reference to one.
/// </remarks>
public enum OrderStatus
{
    /// <summary>Created; the customer has been sent to Stripe and nothing has come back.</summary>
    Pending = 0,

    /// <summary>Stripe has confirmed payment through a verified webhook.</summary>
    Paid = 1,

    /// <summary>Payment failed, or the paid amount did not match what the server computed.</summary>
    Failed = 2,

    /// <summary>The Checkout session expired or the customer backed out.</summary>
    Cancelled = 3,

    /// <summary>The server is being created on the panel.</summary>
    Provisioning = 4,

    /// <summary>The server exists and the subscription is live.</summary>
    Active = 5
}

/// <summary>
/// How far provisioning has got, for the status page.
/// </summary>
/// <remarks>
/// Only stages the fulfilment service can actually observe are here. There is no
/// "network ready" stage, for instance, because nothing in the pipeline reports one.
/// </remarks>
public enum FulfilmentStage
{
    /// <summary>Payment not yet confirmed, or confirmed and not yet picked up.</summary>
    NotStarted = 0,

    /// <summary>Picked up by the fulfilment worker.</summary>
    Preparing = 1,

    /// <summary>The panel has chosen a node and reserved an allocation.</summary>
    NodeSelected = 2,

    /// <summary>The container exists with the plan's resource limits applied.</summary>
    ResourcesAllocated = 3,

    /// <summary>The panel is running the egg's install script.</summary>
    Installing = 4,

    /// <summary>Installed and started.</summary>
    Online = 5,

    /// <summary>Provisioning stopped with an error somebody has to look at.</summary>
    Failed = 6
}

/// <summary>
/// One purchase: a plan on a game, for a billing period, at a price the server computed.
/// </summary>
/// <remarks>
/// <para>
/// The money columns are a snapshot of what the customer agreed to at the moment they clicked
/// through to Stripe, so a later change to the rate card cannot retroactively re-describe an
/// old order. The webhook compares Stripe's paid amount against <see cref="FinalAmount"/> and
/// refuses to fulfil a mismatch.
/// </para>
/// <para>
/// <see cref="UserId"/> is nullable because there is no identity provider yet. When one arrives
/// it fills this in at creation; until then the email Stripe collected identifies the customer.
/// </para>
/// </remarks>
public sealed class Order
{
    /// <summary>Primary key. Also the Stripe <c>client_reference_id</c>.</summary>
    public Guid Id { get; set; }

    /// <summary>The authenticated user, when there is one.</summary>
    public string? UserId { get; set; }

    /// <summary>Email Stripe collected at Checkout. Set by the webhook, never by the browser.</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>The hosted game's slug.</summary>
    public required string GameId { get; set; }

    /// <summary>The plan's slug.</summary>
    public required string PlanId { get; set; }

    /// <summary>The plan's display name at the time of the order.</summary>
    public required string PlanName { get; set; }

    /// <summary>How often the subscription bills.</summary>
    public BillingPeriod BillingPeriod { get; set; }

    /// <summary>The plan's list price per month at the time of the order.</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>List price multiplied by the months in the period.</summary>
    public decimal BaseAmount { get; set; }

    /// <summary>The period's discount, as a whole percentage.</summary>
    public int DiscountPercentage { get; set; }

    /// <summary>What the discount took off, in currency.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>What Stripe was told to charge for the period.</summary>
    public decimal FinalAmount { get; set; }

    /// <summary>ISO 4217 code, lower case, as Stripe uses it.</summary>
    public required string Currency { get; set; }

    /// <summary>Where the order is.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>How far provisioning has got.</summary>
    public FulfilmentStage ProvisioningStage { get; set; }

    /// <summary>The Checkout session created for this order.</summary>
    public string? StripeCheckoutSessionId { get; set; }

    /// <summary>The PaymentIntent when Stripe creates one for the Checkout lifecycle.</summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>The Stripe customer, once Checkout completes.</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>The subscription, once Checkout completes.</summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>The most recently observed Stripe invoice for this subscription.</summary>
    public string? StripeInvoiceId { get; set; }

    /// <summary>
    /// Stripe's own status word for the subscription (<c>active</c>, <c>past_due</c>,
    /// <c>canceled</c>…), mirrored from subscription webhooks. Informational: the order's
    /// <see cref="Status"/> is about the purchase and the server, not the billing relationship.
    /// </summary>
    public string? SubscriptionStatus { get; set; }

    /// <summary>The panel's short server identifier, once provisioned.</summary>
    public string? ServerIdentifier { get; set; }

    /// <summary>Why the order failed, in words safe to show an operator.</summary>
    public string? FailureReason { get; set; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When any column last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>When the verified payment arrived.</summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>Whether the order has reached a state nothing moves it out of.</summary>
    public bool IsTerminal => Status is OrderStatus.Active or OrderStatus.Failed;

    /// <summary>
    /// Whether the order may move to <paramref name="next"/> from where it is.
    /// </summary>
    /// <remarks>
    /// Paid is reachable from Cancelled as well as Pending: a Checkout session can expire on our
    /// side and still be completed on Stripe's if the customer had the page open. Stripe's word
    /// on payment outranks ours on expiry.
    /// </remarks>
    public bool CanTransitionTo(OrderStatus next) => (Status, next) switch
    {
        (OrderStatus.Pending, OrderStatus.Paid) => true,
        (OrderStatus.Pending, OrderStatus.Failed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Cancelled, OrderStatus.Paid) => true,
        (OrderStatus.Paid, OrderStatus.Provisioning) => true,
        (OrderStatus.Paid, OrderStatus.Failed) => true,
        (OrderStatus.Provisioning, OrderStatus.Active) => true,
        (OrderStatus.Provisioning, OrderStatus.Failed) => true,
        _ => false
    };

    /// <summary>
    /// Moves the order to <paramref name="next"/>, or throws if the move is not allowed.
    /// </summary>
    /// <param name="next">The state to move to.</param>
    /// <param name="now">The clock.</param>
    /// <exception cref="InvalidOperationException">The transition is not permitted.</exception>
    public void TransitionTo(OrderStatus next, DateTimeOffset now)
    {
        if (!CanTransitionTo(next))
        {
            throw new InvalidOperationException($"An order cannot go from {Status} to {next}.");
        }

        Status = next;
        UpdatedAt = now;

        if (next is OrderStatus.Paid)
        {
            PaidAt = now;
        }
    }
}

/// <summary>
/// A Stripe event that has already been handled, so a redelivery is acknowledged and ignored
/// rather than fulfilled twice.
/// </summary>
public sealed class ProcessedStripeEvent
{
    /// <summary>The Stripe event id (<c>evt_…</c>).</summary>
    public required string Id { get; set; }

    /// <summary>The event type, for operators reading the table.</summary>
    public required string Type { get; set; }

    /// <summary>When it was handled.</summary>
    public DateTimeOffset ProcessedAt { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
