# Vision and scope

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

HTS Hosting sells game server hosting. Its public experience should explain the infrastructure, present comparable resources, and guide visitors toward a purchase they can review. The internal system connects confirmed payment to server creation.

## Audience

- Players and communities seeking a Project Zomboid server.
- Customers estimating a special configuration and contacting HTS.
- HTS maintainers responsible for catalog, deployment, billing, and provisioning.

## Implemented scope

Project Zomboid and Minecraft share the implemented trial policy. Zomboid paid plans are available; Minecraft paid tiers are operator-configured and empty by default. There are eight Zomboid plans, monthly/quarterly/annual periods, configurable pricing, SSR review, promotion codes backed by commerce persistence, hosted Checkout, verified webhooks, order states, and a provisioning worker.

The site presents infrastructure and project photographs, light/dark themes, EN/PT-BR, and accessible animation fallbacks. Public hardware data is editorial content from the infrastructure service, not live measurements.

## Demonstrations and limitations

| Item | Current limitation |
| --- | --- |
| Legacy panel preview component | Retained in source, removed from the homepage |
| Local login | Gateway that authenticates nobody |
| Local registration | Does not create accounts, send data, or persist passwords |
| Custom configuration | Estimate and email draft, not automatic checkout |
| Automatic 24-hour trial | Requires commerce migration, SMTP and panel configuration; new requests default off |
| Minecraft paid upgrades | Require configured tiers and matching commerce rows; the chosen trial egg/world is preserved |
| Spanish | No implemented culture or resources |
| Portal/invoices/administration | No public exposure until identity and authorization exist |
| Subscriptions | Backend handles events, without automatic server suspension/deletion |

External panel login does not authenticate the storefront. A success screen is not proof of payment: that authority belongs to the webhook.

## Redesign completion criteria

The interface must use the existing catalog and rules, work across commercial routes, preserve Blazor's DOM, provide focus states, and respect reduced motion. Build, test, and responsive review evidence must be available. The [visual review](../FRONTEND-REDESIGN.md) records this work; publication depends on the pipeline and production configuration.

See [product requirements](product-requirements-document.md), [software requirements](software-requirements-specification.md), and the [roadmap](product-roadmap.md).
