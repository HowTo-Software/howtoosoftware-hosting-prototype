using System.Net;
using System.Net.Http.Json;
using System.Text;
using HowToSoftware.Hosting.Models;
using HowToSoftware.Hosting.Models.Orders;
using HowToSoftware.Hosting.Services.Orders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace HowToSoftware.Hosting.Tests;

/// <summary>
/// The webhook receiver, hosted in-process: a real HTTP POST, a real signature check with the
/// Stripe SDK's own algorithm, and the order store behind it.
/// </summary>
/// <remarks>
/// The signing secret below is a test fixture, not a Stripe credential; the SDK only needs a
/// string it can HMAC with on both sides.
/// </remarks>
public sealed class StripeWebhookEndpointTests : IDisposable
{
    private const string Secret = "whsec_unit_test_only_not_a_real_secret";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hts-webhook-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StripeWebhookEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Hosting", $"Data Source={_dbPath}");
            builder.UseSetting("Stripe:WebhookSecret", Secret);
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();

        try
        {
            System.IO.File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    private IOrderStore Orders => _factory.Services.GetRequiredService<IOrderStore>();

    private async Task<Order> SeedPendingOrderAsync(string sessionId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            GameId = "project-zomboid",
            PlanId = "zomboid-4gb",
            PlanName = "Outpost",
            BillingPeriod = BillingPeriod.Quarterly,
            MonthlyPrice = 7.99m,
            BaseAmount = 23.97m,
            DiscountPercentage = 5,
            DiscountAmount = 1.20m,
            FinalAmount = 22.77m,
            Currency = "usd",
            Status = OrderStatus.Pending,
            StripeCheckoutSessionId = sessionId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await Orders.AddAsync(order);
        return order;
    }

    private static string CompletedEvent(string eventId, Order order, long amount = 2277) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2025-08-27.basil",
          "created": 1756900000,
          "livemode": false,
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "{{order.StripeCheckoutSessionId}}",
              "object": "checkout.session",
              "mode": "subscription",
              "status": "complete",
              "payment_status": "paid",
              "amount_total": {{amount}},
              "currency": "usd",
              "client_reference_id": "{{order.Id:D}}",
              "customer": "cus_test_123",
              "subscription": "sub_test_123",
              "customer_details": { "email": "survivor@example.com" },
              "metadata": { "internal_order_id": "{{order.Id:D}}", "plan_id": "zomboid-4gb", "billing_period": "quarterly" }
            }
          }
        }
        """;

    private static HttpRequestMessage Signed(string payload, string secret = Secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = EventUtility.ComputeSignature(secret, timestamp, payload);

        var request = new HttpRequestMessage(HttpMethod.Post, SiteRoutes.StripeWebhook)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");

        return request;
    }

    [Fact]
    public async Task ACorrectlySignedCompletedSessionMarksTheOrderPaid()
    {
        var order = await SeedPendingOrderAsync("cs_test_signed");

        var response = await _client.SendAsync(Signed(CompletedEvent("evt_signed_1", order)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paid = await Orders.FindAsync(order.Id);
        Assert.NotNull(paid);
        Assert.Equal(OrderStatus.Paid, paid.Status);
        Assert.Equal("survivor@example.com", paid.CustomerEmail);
        Assert.Equal("sub_test_123", paid.StripeSubscriptionId);
    }

    [Fact]
    public async Task AnUnsignedDeliveryIsRefused_AndTouchesNothing()
    {
        var order = await SeedPendingOrderAsync("cs_test_unsigned");

        var response = await _client.PostAsync(
            SiteRoutes.StripeWebhook,
            new StringContent(CompletedEvent("evt_unsigned", order), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(OrderStatus.Pending, (await Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task AWrongSecretIsRefused()
    {
        var order = await SeedPendingOrderAsync("cs_test_wrong");

        var response = await _client.SendAsync(Signed(CompletedEvent("evt_wrong", order), secret: "whsec_somebody_else"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(OrderStatus.Pending, (await Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task ATamperedBodyIsRefused()
    {
        var order = await SeedPendingOrderAsync("cs_test_tampered");
        var request = Signed(CompletedEvent("evt_tampered", order));

        // Same signature, different body: the amount has been edited after signing.
        request.Content = new StringContent(CompletedEvent("evt_tampered", order, amount: 1), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(OrderStatus.Pending, (await Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task AMismatchedAmountIsSignedButNotFulfilled()
    {
        var order = await SeedPendingOrderAsync("cs_test_mismatch");

        var response = await _client.SendAsync(Signed(CompletedEvent("evt_mismatch", order, amount: 799)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Failed, (await Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task ARedeliveryIsAcknowledgedOnce()
    {
        var order = await SeedPendingOrderAsync("cs_test_twice");
        var payload = CompletedEvent("evt_twice", order);

        var first = await _client.SendAsync(Signed(payload));
        var second = await _client.SendAsync(Signed(payload));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(OrderStatus.Paid, (await Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task TheStatusEndpointReportsTheOrderWithoutItsMoney()
    {
        var order = await SeedPendingOrderAsync("cs_test_status");
        await _client.SendAsync(Signed(CompletedEvent("evt_status", order)));

        var response = await _client.GetAsync($"{SiteRoutes.OrderStatus}?session_id=cs_test_status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Paid\"", body.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("22.77", body, StringComparison.Ordinal);
        Assert.DoesNotContain("survivor@example.com", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheStatusEndpointDoesNotGuess()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{SiteRoutes.OrderStatus}?session_id=cs_nope")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(SiteRoutes.OrderStatus)).StatusCode);
    }

    [Fact]
    public async Task TheOldProjectZomboidRouteRedirectsPermanently()
    {
        var response = await _client.GetAsync(SiteRoutes.LegacyProjectZomboid);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(SiteRoutes.ProjectZomboid, response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData(SiteRoutes.GameHosting)]
    [InlineData(SiteRoutes.ProjectZomboid)]
    [InlineData("/game-hosting/project-zomboid/review?plan=zomboid-8gb&period=annual")]
    [InlineData("/payment/cancel?order=not-an-order")]
    [InlineData("/payment/success?session_id=cs_missing")]
    public async Task EveryPageInTheFlowRenders(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
