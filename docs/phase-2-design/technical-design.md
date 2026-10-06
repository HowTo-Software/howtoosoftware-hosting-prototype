# Technical design and core rules

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Catalog, resources, and pricing

[StaticPlanCatalogService](../../src/HowToSoftware.Hosting/Services/StaticPlanCatalogService.cs) supplies the public tiers and limits used in provisioning.

| Slug | Name | RAM GiB | CPU % | Disk GiB | Backups |
| --- | --- | ---: | ---: | ---: | ---: |
| zomboid-4gb | Outpost | 4 | 300 | 25 | 1 |
| zomboid-5gb | Settlement | 5 | 400 | 25 | 2 |
| zomboid-6gb | Stronghold | 6 | 400 | 25 | 3 |
| zomboid-8gb | Knox Cell | 8 | 500 | 40 | 5 |
| zomboid-10gb | Rosewood | 10 | 500 | 40 | 6 |
| zomboid-12gb | West Point | 12 | 600 | 40 | 7 |
| zomboid-14gb | Louisville | 14 | 600 | 40 | 8 |
| zomboid-16gb | Knox County | 16 | 700 | 40 | 10 |

All use two allocations and zero game databases. Settlement is recommended. Player slots are not inferred from RAM and have no agreed commercial number. RAM/disk are converted to MiB by multiplying by 1024.

Calculated monthly price:

```text
CPU = rate_per_100% × cpuPercent / 100
RAM = rate_per_GiB × memoryGb
Disk = rate_per_block × diskGb / block_size
Monthly = CPU + RAM + Disk
```

Amounts are rounded to cents. `CharmPricing` adjusts calculated prices to end in .99; a valid per-slug override takes precedence and is not adjusted. With the versioned defaults, Outpost calculates 7.88 before adjustment and displays USD 7.99. This illustrates local checkout configuration, without claiming the price active in production.

Rates are strings parsed with invariant culture. An empty override does not mean an unpriced plan: calculation uses the rate card. With neither an override nor complete rates, the plan remains unpriced instead of receiving an invented value.

## Periods and promotions

`BillingPolicy` centralizes months and discounts: monthly 1/0%, quarterly 3/5%, and annual 12/10%. Quotes separate base, discount, and period total; the monthly equivalent is not a separate charge.

`PromotionService` checks normalized code, active state, time window, game/plan scope, minimum amount, currency, and stacking rules. Without a commerce database, `NullPromotionService` treats every code as inapplicable. There is no public promotion-management interface.

## Purchase and state

The review POST submits plan, period, and code; pricing and resources are resolved in the backend. The order stores the purchase's financial values. The webhook later compares payment against that snapshot, not against a subsequently changed rate.

`StripeCheckoutService` uses a configured, validated return origin, never the browser's arbitrary Host header. Card data stays in hosted Checkout.

The [commerce guide](../COMMERCE-ARCHITECTURE.md) details confirmation, queue, and integration; the [HTTP contract](api-specification.md) describes fields and responses.

## Language, theme, and motion

EN is the default; PT-BR is the other culture. Cookie selection precedes Accept-Language; general query strings do not change culture. `/culture/select` writes the cookie and redirects only to a local path.

Theme preference is applied in the browser. Visual changes use CSS tokens without changing commercial rules. The wordmark's WebGL introduction lasts 4.4 seconds, limited to 30 fps, with a maximum DPR of 1.5 on desktop and 1 on mobile, and pauses when unnecessary.

No script may replace Blazor's semantic children with generated spans. The decision and consequences are in [ADR-0004](adr/0004-preserve-blazor-dom.md).
