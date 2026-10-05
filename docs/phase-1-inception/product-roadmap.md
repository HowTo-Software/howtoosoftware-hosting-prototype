# Current state and proposed evolution

> **Status:** Unscheduled evolution proposal; current state checked against the code
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Implemented and documented

- Blazor/.NET 10 foundation, Zomboid catalog, resource pricing, and three billing periods.
- SSR review, Stripe Checkout, webhooks, and Pterodactyl delivery.
- SQL Server persistence, explicit migrations, recovery, and idempotency.
- Themes and EN/PT-BR; redesign with photography, comparison, and animation fallbacks.
- Image/deployment pipeline and documentation by phase.

The [CHANGELOG](../../CHANGELOG.md) distinguishes the previous baseline from unreleased local work. The existence of a pipeline does not prove that this branch is in production.

## Proposed next steps

| Dependency order | Evolution | Completion dependency |
| --- | --- | --- |
| 1 | Verify the real environment before publishing the revision | Domain, proxy, Stripe URLs, SQL, Pterodactyl, and GitHub Environment |
| 2 | Activate the implemented 24-hour trial | Review/apply migration, configure SMTP and panel, approve Minecraft profiles and paid tiers, then validate end to end |
| 3 | Connect HTS identity | Identity provider and per-order/server authorization |
| 4 | Enable billing portal and administration | Identity, auditing, and authorization tests |
| 5 | Automate subscription lifecycle | Nonpayment/cancellation policy, reconciliation, and operational rollback |
| 6 | Launch Minecraft | Egg/template, plans, artwork, and provisioning validation |
| 7 | Add Spanish | Culture, complete resources, language review, and tests |
| 8 | Measure operations and recovery | Alerts, tested backup/restore, and team-defined targets |

This order is a technical proposal based on dependencies, **without a schedule, budget, or formal product approval**. No tickets or delivery commitments are created automatically.
