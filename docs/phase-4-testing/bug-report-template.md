# Reporting a bug

> **Status:** Reporting guide; illustrative example, not a current incident
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

Record the minimum necessary to reproduce, without secrets or customer data. Security issues use the private channel in [SECURITY.md](../../SECURITY.md).

## Required information

- Summary: concrete action and incorrect behavior.
- Environment: local/staging/production, commit or image tag, browser, and OS.
- Route, language, theme, viewport width, and motion preference.
- Steps, observed result, and expected result.
- Frequency and impact: appearance, navigation, purchase, payment, or fulfillment.
- Evidence: screenshot without personal data, sanitized message, and timestamp.
- Validation: reproducing tests, if available.

## Example without real data

**Summary:** catalog title remains after returning to the homepage.

**Environment:** local preview; Chromium; PT-BR; dark theme; 375 px.

**Steps:** open homepage, visit catalog, return through the logo.

**Observed:** title and CTA belong to the previous route.

**Expected:** homepage content with correct menu and actions.

**Useful evidence:** route sequence and console error, if any.

For financial issues, order/session references must stay in private channels. Do not publish emails, full webhook bodies, Stripe/Pterodactyl keys, connection strings, or complete `docker inspect` output.
