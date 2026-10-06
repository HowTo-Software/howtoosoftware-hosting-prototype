# Software requirements

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Functional requirements

| ID | Contract | Source / validation |
| --- | --- | --- |
| RF-01 | Resolve game, plan, and period in the backend | `OrderPricingService`; PurchaseFlowTests |
| RF-02 | Persist the financial order snapshot before Checkout | `StripeCheckoutService`; PurchaseFlowTests |
| RF-03 | Validate Stripe signature, amount, and currency | `PaymentEndpoints`, `StripeWebhookHandler`; Stripe tests |
| RF-04 | Avoid duplicate creation on repeated payment/provisioning | Stable external IDs, stores, and worker; OrderFulfillmentTests |
| RF-05 | Resume orders awaiting delivery after restart | `OrderFulfillmentWorker`; OrderFulfillmentTests |
| RF-06 | Expose minimal public status only | `/api/payments/status`; SecurityBoundaryTests |
| RF-07 | Select language by cookie, then browser preference | `SiteLocalization`; LocalizationTests |
| RF-08 | Serve pages without integrations | Alternative stores and visible configuration state; configuration tests |
| RF-09 | Validate promotions in the backend | `PromotionService`; promotion codes require CommerceDbContext |
| RF-10 | Run migrations only through explicit commands | `Program.cs`; SqlServerFoundationTests |

## Nonfunctional requirements

- .NET 10, C# 14, nullable enabled; Release builds treat warnings as errors.
- SSR for public content; component-level interactivity when needed.
- Commercial content and controls accessible without depending on hover, WebGL, or animation completion.
- Scripts preserve Blazor-rendered children; navigation must dispose of previous effects.
- Reduced motion, hidden tabs, and offscreen elements pause the wordmark animation.
- Credentials remain backend-only; public HTML contains no SQL connection strings or administrative keys.
- Antiforgery on form POSTs; webhook signatures use the original body.
- Configured SQL Server uses encrypted transport; an invalid connection must not cause silent fallback.
- A pre-deployment migration must remain compatible with the previous image because rollback does not undo database changes.
- Detailed circuit exceptions remain restricted to Development.

HTTP limits and response codes are in the [endpoint contract](../phase-2-design/api-specification.md). The repository defines no formal SLA, latency budget, RPO, or RTO. Those commitments require measurement and operational decisions.

## Traceability

The [project map](../PROJECT-MAP.md) locates classes. [Test cases](../phase-4-testing/test-cases.md) connect risks to existing tests; the [threat model](../security/threat-model.md) explains trust boundaries.
