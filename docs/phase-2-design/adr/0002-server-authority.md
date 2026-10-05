# ADR-0002 — Backend financial authority

> **Status:** Retrospective record of an implemented decision; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Context

Browser fields, URLs, and state can be modified. Confirming payment through a redirect or accepting client-supplied pricing would allow inconsistent charging and provisioning.

## Observed decision

Resolve catalog and quotes on the server, persist an order snapshot, use hosted Stripe Checkout, and release fulfillment only after a signed webhook with verified amount/currency. Expose minimal progress to the browser.

## Consequences

Later rate changes do not rewrite the agreed amount. Repeated webhooks require idempotency; processing failures must permit retries. Account interfaces remain closed until ownership authorization exists.

Trusting client-supplied totals, paid state, or email was rejected because it violates the trust boundary.

Sources: [commerce](../../COMMERCE-ARCHITECTURE.md), [StripeCheckoutService](../../../src/HowToSoftware.Hosting/Services/Payments/StripeCheckoutService.cs), [StripeWebhookHandler](../../../src/HowToSoftware.Hosting/Services/Payments/StripeWebhookHandler.cs).
