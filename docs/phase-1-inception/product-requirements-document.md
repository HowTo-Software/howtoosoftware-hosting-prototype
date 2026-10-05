# Product requirements

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Main journey

1. Discover HTS and understand the service.
2. Choose an available game.
3. Compare RAM, CPU allocation, storage, backups, and billing period.
4. Review the total before leaving the site.
5. Complete payment in Stripe.
6. Follow real provisioning stages and access the external panel.

## Requirements and state

| ID | Need | Project state | Verifiable criterion |
| --- | --- | --- | --- |
| PR-01 | Offer by game | Implemented | Zomboid available; Minecraft has no purchase action |
| PR-02 | Plan comparison | Implemented | Eight tiers, catalog resources, and selectable period |
| PR-03 | Transparent pricing | Implemented | Monthly equivalent and period total match the quote |
| PR-04 | Review before payment | Implemented | Plan/period visible; server recalculates on submission |
| PR-05 | Payment and delivery | Implemented; requires integrations | Only a verified event releases the worker |
| PR-06 | Theme and language | Implemented | Light/dark; EN/PT-BR; persistent language selection |
| PR-07 | Useful motion | Implemented | Focus/hover/reveals with content available without animation |
| PR-08 | Understandable infrastructure | Implemented | Photos and specifications from project services |
| PR-09 | Special configuration | Implemented as an estimate | Normalize resources, show estimate, and open email draft |
| PR-10 | Customer area on the site | Pending | Requires identity, resource-level authorization, and auditing |
| PR-11 | 24-hour trial | Email verification, SQL claim, provisioning, suspension and 72-hour retention implemented | Needs deployment configuration and unapplied migration; one per account across games |

## Presentation rules

CPU is Pterodactyl's shared percentage limit: 100% is equivalent to one logical thread, without a dedicated-core promise. RAM/disk resources are stored in MiB; interface GB labels represent tiers derived using 1024.

Financial values must not use animated counting. A planned game must not receive an invented price. An inapplicable promotion code is rejected when checkout starts, without assuming a discount. The default quote uses USD; selecting PT-BR does not convert currency.

No numeric conversion, availability, or revenue targets are configured in this documentation. Measuring commercial performance requires instrumentation and team decisions; these goals must not be presented as already collected data.

Sources: [plan catalog](../../src/HowToSoftware.Hosting/Services/StaticPlanCatalogService.cs), [games](../../src/HowToSoftware.Hosting/Services/GameCatalog.cs), [review](../../src/HowToSoftware.Hosting/Components/Pages/PlanReview.razor).
