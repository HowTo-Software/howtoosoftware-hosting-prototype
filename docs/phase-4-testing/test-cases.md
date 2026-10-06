# Test cases and traceability

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Existing automated tests

| Case | Expected result | File |
| --- | --- | --- |
| Tiers and units | Eight plans, consistent MiB and CPU% limits | StaticPlanCatalogServiceTests, HostingPlanMappingTests |
| Discounts and cents | Reproducible period total independent of culture | BillingPolicyTests |
| Planned/invalid game | No purchase for an unavailable product | GameCatalogTests, PurchaseFlowTests |
| Tampered checkout | Backend rejects invalid selection/origin/code | PurchaseFlowTests |
| Invalid or oversized webhook | Appropriate rejection without fulfillment release | StripeWebhookEndpointTests |
| Repeated event | No duplicate internal charge or server | PurchaseFlowTests, OrderFulfillmentTests |
| Restart during delivery | Resume from persisted state | OrderFulfillmentTests |
| Panel error | Record failure without exposing sensitive response | PterodactylTests |
| Invalid commerce SQL | No silent fallback at startup | SqlServerFoundationTests, ShippedConfigurationTests |
| Public read | Status without financial/personal data | SecurityBoundaryTests |
| Culture and fallback | Cookie wins; consistent EN/PT-BR | LocalizationTests |
| Demonstration login | No authentication or password retention | SignInTests |

Files are in [tests/HowToSoftware.Hosting.Tests](../../tests/HowToSoftware.Hosting.Tests). Names describe existing coverage, without inventing tests in this documentation review.

## Browser

1. Home → product/plans → review → home: text, CTAs, header, and images must match the current route.
2. At 320/375/768/1024/1440 px: avoid horizontal overflow, clipped controls, or inaccessible review totals.
3. Both themes and EN/PT-BR: check contrast, strings, wrapping, and currency.
4. Keyboard: navigate links, open/close menu, Escape, visible focus, and plan controls.
5. Without JavaScript: content, links, language, and trial forms and native plan controls remain usable; effects have fallbacks.
6. With prefers-reduced-motion: static wordmark, readable content, and preserved navigation.
7. Unavailable WebGL or lost context: keep static branding; recover/dispose without errors.
8. Hidden tab/offscreen: stop drawing; after navigation, remove old handlers/resources.
9. Review: antiforgery and slug submission, without browser-controlled financial values.
10. Estimate: slider normalization, coherent amounts, and email draft.

Recorded execution is in [FRONTEND-REDESIGN.md](../FRONTEND-REDESIGN.md). A complete browser runner is not versioned as a required CI stage; the JSON files are evidence, not a ready-made Playwright command.

## External staging integration

Test purchase → signed webhook → Paid order → unique server → Active, including retry and restart. Check redacted logs and cleanup. Opt-in SQL integration and Pterodactyl creation require dedicated targets; they were not exercised by the documentation review.
