# Commerce, payment, and provisioning

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

[Index](README.md) · [Architecture](phase-2-design/architecture.md) · [Data](phase-2-design/data-model.md) · [HTTP contracts](phase-2-design/api-specification.md)

## Implemented flow

```text
game + plan + billing period
  → server-side quote
  → internal order with snapshot
  → Stripe subscription Checkout
  → signed webhook, verification, and idempotency
  → paid order + fulfillment queue
  → idempotent Pterodactyl lookup/creation
  → installation polling
  → active service or recorded failure
```

The browser selects slugs and may submit a promotion code. It does not define price, resources, node, or paid state. The review POST uses antiforgery. Payment confirmation belongs to the webhook; opening `/payment/success` only displays progress.

## Before Checkout

`OrderPricingService` resolves an available game, matching plan, and period quote. `StripeCheckoutService` validates a supplied promotion code, return origin, and Stripe configuration.

The service persists a Pending order and financial snapshot before requesting a session. Stripe IDs are associated with the order; the origin comes from `Site.BaseUrl`, not a client-provided Host. Checkout uses subscription mode with a configured price or inline pricing.

The storefront still has no general HTS authentication. Trial upgrades use a separate opaque owner capability issued only after email confirmation, and reserve the existing trial against the internal order. This does not turn the local sign-in prototype into a connected identity provider.

## Webhook and state

`PaymentEndpoints` validates size, header, and original-body signature through the Stripe gateway. The handler processes Checkout, subscription, and invoice events. Repeated events must retain the same result without new delivery.

Payment amount/currency are checked against the order snapshot. Later rate-card changes do not alter the agreed amount. Processing failures return 500 for retry; successful responses do not wait for the entire installation.

Commerce event references and unique indexes protect idempotency. Public status exposes only status, stage, and terminal state. It is neither an administrative endpoint nor proof of ownership.

## Queue, creation, and installation

`OrderFulfillmentQueue` is a process-local channel. `OrderFulfillmentWorker` drains it in the background and queries orders awaiting delivery at startup, including interrupted fulfillment as selected by the store.

The provisioner uses an external ID derived from the order to find an existing server before creation. The operational customer identity may be derived from Stripe-confirmed email; this does not implement login or authorization.

Creation uses catalog resources and the configured game template/egg. The service then polls installation in the panel and records progress. Active reflects the installation state accepted by the flow, without inventing a player-connectivity test.

Failures are recorded; there is no public administrative retry UI. After data loss/restore, reconcile external references before restarting fulfillment.

## Verified trials and same-server upgrades

`/trial` creates no server until the email confirmation is posted with antiforgery. A durable
commerce trial record grants one trial per panel account across Project Zomboid and Minecraft.
The default trial uses 6144 MiB memory, 25600 MiB disk and CPU 0 (unlimited), runs for 24 hours
after installation, then suspends the server and retains data for 72 hours before deletion.
New requests require explicit enablement, commerce SQL, SMTP and panel configuration.

Minecraft edition/software/version choices come from approved deployment profiles, not a
browser-supplied startup command. Paid tiers are configured independently and have no invented
fallback prices. A verified owner's paid upgrade reserves the trial against the order; after
the signed webhook confirms the agreed amount, fulfillment updates/resumes that same panel
server instead of creating another one. Its world and files remain in place.

The lifecycle worker uses durable leases, stable external IDs and notification flags. Paid or
unresolved reservations block deletion while reconciliation completes. Owner email recovery
rotates access without granting another trial, including when new trials are disabled.
See [TRIAL-SERVERS.md](TRIAL-SERVERS.md) for configuration, state and security boundaries.

Public promotional announcements are a separate opt-in publication list. Each existing code
is validated by the checkout promotion service for a current plan/period before rendering;
private coupons are never enumerated and announcements never redeem them. See
[PUBLIC-PROMOTIONS.md](PUBLIC-PROMOTIONS.md).

## Persistence

| Configuration | Selected components |
| --- | --- |
| Valid commerce SQL | CommerceDbContext, SqlServerOrderStore, provisioning state, billing, and promotions |
| Without commerce; Hosting configured | HostingDbContext / EfOrderStore, with null supplementary commerce services |
| No database | Public pages; no usable purchase storage |

Without a commerce database, promotion codes are not accepted unchecked. Configuring Stripe alone does not make purchases functional: order persistence is also required.

Migrations are explicit and separate by context. Commerce seed runs only in Development. See [SQLSERVER-SETUP.md](SQLSERVER-SETUP.md).

## Customer-area boundary

`PrototypeAuthenticationGateway` authenticates nobody. Local registration does not create an account, send a form to an account service, or save passwords.

Customer portal, downloadable invoices, administration/retry, and automatic subscription-driven suspension remain closed until real identity, resource-level authorization, and auditing exist. Receiving subscription/invoice events in the backend does not mean the full operational lifecycle is automated.

The 24-hour banner leads to the verified trial workflow described above. Customer subscription
billing suspension remains separate from trial expiry suspension; receiving Stripe invoice
events does not imply a complete subscription policy engine. Custom configuration still
provides an estimate and email draft rather than automatic bespoke server creation.

## Sources

- [StripeCheckoutService](../src/HowToSoftware.Hosting/Services/Payments/StripeCheckoutService.cs)
- [StripeWebhookHandler](../src/HowToSoftware.Hosting/Services/Payments/StripeWebhookHandler.cs)
- [OrderFulfillment](../src/HowToSoftware.Hosting/Services/Orders/OrderFulfillment.cs)
- [Order](../src/HowToSoftware.Hosting/Models/Orders/Order.cs)
- [Purchase tests](../tests/HowToSoftware.Hosting.Tests/PurchaseFlowTests.cs)
- [Fulfillment tests](../tests/HowToSoftware.Hosting.Tests/OrderFulfillmentTests.cs)
