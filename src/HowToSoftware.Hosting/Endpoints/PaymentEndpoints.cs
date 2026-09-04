using System.Text;
using global::Stripe;
using HowToSoftware.Hosting.Infrastructure.Stripe;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Orders;
using HowToSoftware.Hosting.Services.Payments;

namespace HowToSoftware.Hosting.Endpoints;

/// <summary>
/// The two HTTP endpoints the payment flow needs outside Razor: the Stripe webhook receiver and
/// the order-status read the success page polls.
/// </summary>
public static class PaymentEndpoints
{
    /// <summary>Largest webhook body accepted, in bytes. Stripe events are a few kilobytes.</summary>
    private const int MaxWebhookBytes = 256 * 1024;

    /// <summary>Maps the payment endpoints.</summary>
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // Stripe posts here from its own servers with no antiforgery token, and authenticates
        // itself with the signature header instead - which is checked before anything else.
        app.MapPost(SiteRoutes.StripeWebhook, ReceiveWebhookAsync)
            .DisableAntiforgery()
            .WithName("StripeWebhook");

        app.MapGet(SiteRoutes.OrderStatus, ReadStatusAsync)
            .WithName("OrderStatus");

        return app;
    }

    private static async Task<IResult> ReceiveWebhookAsync(
        HttpRequest request,
        IStripeGateway stripe,
        IStripeWebhookHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("HowToSoftware.Hosting.StripeWebhook");

        if (request.ContentLength is > MaxWebhookBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!request.Headers.TryGetValue("Stripe-Signature", out var signature) || string.IsNullOrWhiteSpace(signature))
        {
            logger.LogWarning("Webhook delivery without a Stripe-Signature header was refused.");
            return Results.BadRequest();
        }

        // The body must reach the signature check byte for byte; no model binding, no
        // re-serialisation. Read it as UTF-8 text and hand that exact string over.
        string json;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (json.Length > MaxWebhookBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        Event stripeEvent;

        try
        {
            stripeEvent = stripe.ConstructEvent(json, signature.ToString());
        }
        catch (StripeException exception)
        {
            // The body is never logged: a forged delivery is attacker-controlled text.
            logger.LogWarning("Webhook signature rejected: {Reason}", exception.Message);
            return Results.BadRequest();
        }

        try
        {
            await handler.HandleAsync(stripeEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A 5xx makes Stripe retry, which is what we want for a transient failure on our side.
            logger.LogError(exception, "Handling Stripe event {EventId} ({Type}) failed.", stripeEvent.Id, stripeEvent.Type);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Ok();
    }

    private static async Task<IResult> ReadStatusAsync(
        string? session_id,
        IOrderStore orders,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session_id) || session_id.Length > 128)
        {
            return Results.NotFound();
        }

        var order = await orders.FindByCheckoutSessionAsync(session_id.Trim(), cancellationToken).ConfigureAwait(false);

        if (order is null)
        {
            return Results.NotFound();
        }

        // Only what the status page renders. No amounts, no email, no Stripe ids: the session
        // id in the URL is a capability to watch progress, not to read the order.
        return Results.Ok(new OrderStatusResponse(
            order.Id,
            order.Status.ToString(),
            order.ProvisioningStage.ToString(),
            OrderStatusResponse.StageIndexFor(order),
            order.IsTerminal,
            order.ServerIdentifier));
    }
}

/// <summary>What the success page polls.</summary>
/// <param name="OrderId">The order.</param>
/// <param name="Status">The <see cref="OrderStatus"/> name.</param>
/// <param name="Stage">The <see cref="FulfilmentStage"/> name.</param>
/// <param name="StageIndex">Which of the page's timeline steps is current.</param>
/// <param name="IsTerminal">Whether polling can stop.</param>
/// <param name="ServerIdentifier">The panel's short server id, once there is one.</param>
public sealed record OrderStatusResponse(
    Guid OrderId,
    string Status,
    string Stage,
    int StageIndex,
    bool IsTerminal,
    string? ServerIdentifier)
{
    /// <summary>
    /// Projects an order onto the six-step timeline the success page draws:
    /// 0 payment confirmed, 1 preparing, 2 node selected, 3 resources allocated,
    /// 4 installing, 5 online. -1 means payment is not confirmed yet; a paid order sits at 1,
    /// because confirmation is done and preparation is the step in hand.
    /// </summary>
    public static int StageIndexFor(Order order) => order.Status switch
    {
        OrderStatus.Pending => -1,
        OrderStatus.Cancelled => -1,
        OrderStatus.Failed => order.ProvisioningStage is FulfilmentStage.NotStarted ? -1 : StageOf(order.ProvisioningStage),
        // Paid means the first station is behind us: payment is confirmed, preparation is what is
        // happening (or about to), so the marker sits on "preparing", not on "payment".
        OrderStatus.Paid => 1,
        OrderStatus.Provisioning => StageOf(order.ProvisioningStage),
        OrderStatus.Active => 5,
        _ => -1
    };

    private static int StageOf(FulfilmentStage stage) => stage switch
    {
        FulfilmentStage.NotStarted => 0,
        FulfilmentStage.Preparing => 1,
        FulfilmentStage.NodeSelected => 2,
        FulfilmentStage.ResourcesAllocated => 3,
        FulfilmentStage.Installing => 4,
        FulfilmentStage.Online => 5,
        FulfilmentStage.Failed => 1,
        _ => 0
    };
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
