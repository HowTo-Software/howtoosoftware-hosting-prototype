# Security policy

> **Status:** Existing policy; technical context updated
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Reporting

Do not open a public issue containing keys, personal data, internal URLs, or exploitation steps. Send a report to `henry.cahill@howtoosoftware.com`, starting the subject with `[SECURITY]`, and include only what is necessary to reproduce the issue. Revoke exposed credentials; do not use them to validate impact.

## Current scope

The maintained version is the latest version of the main branch. The login screen remains a closed demonstration interface: it authenticates nobody and does not persist passwords. Accounts, a customer portal, and administrative routes may only be published after real identity, resource-level authorization, and auditing are in place.

## Repository rules

- `.env`, local databases, certificates, dumps, and publishing output are ignored by Git.
- Stripe, SQL Server, and Pterodactyl credentials are server-only and must come from the hosting environment or a secrets vault.
- Never publish logs, screenshots, or exports containing customer data.
- If a leak is suspected, revoke and replace the credential before investigating the cause.

The deployment checklist is in [`docs/SECURITY-HARDENING.md`](docs/SECURITY-HARDENING.md). Also see the [threat model](docs/security/threat-model.md) and [data inventory](docs/security/compliance.md).
