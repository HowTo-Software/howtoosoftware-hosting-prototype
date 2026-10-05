# Data, privacy, and compliance review

> **Status:** Technical inventory checked; policies and external review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

This document inventories data and technical gaps. It does not record certification, legal approval, or an implemented retention policy.

## Inventory

| Data | Origin / storage | Purpose |
| --- | --- | --- |
| Plan slug and period | Browser; backend order | Purchase selection |
| Amounts and currency | Backend; SQL snapshot and Stripe | Billing and verification |
| Customer email | Stripe Checkout; order/commerce/panel as used by the flow | Operational identification and fulfillment |
| Customer/subscription/invoice references | Stripe; commerce tables | Billing correlation |
| Server ID and state | Pterodactyl; commerce state | Fulfillment and tracking |
| Language preference | .AspNetCore.Culture cookie | EN/PT-BR on future visits |
| Theme preference | Browser storage | Light/dark appearance |
| Complete card number | Hosted Stripe Checkout | Not collected/stored by the HTS site |

Demonstration registration does not persist or send its fields. Estimates open an email draft; sending depends on the visitor's email client. Public status excludes personal/financial data.

## Outstanding tasks for the responsible team

- Define and publish terms/privacy notices matching actual operations.
- Assign ownership for access, correction, and deletion requests, with approved commercial retention.
- Verify operator access, logs, backups, and providers.
- Document retention, disposal, and any contracted-service data transfers.
- Review policies before enabling identity, analytics, portal, and trials.
- Check UI credits/licenses and asset rights.

The documentation reorganization did not perform these tasks. The repository has no formal schedule or named compliance owner.

The proprietary license remains in [LICENSE](../../LICENSE). Third-party use and UI code origins are in [FRONTEND-REDESIGN.md](../FRONTEND-REDESIGN.md). Security and reporting guidance is in [SECURITY.md](../../SECURITY.md).
