# Test Stripe checkout and webhooks

> **Status:** Technical procedure checked against the code; external execution unverified
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

[Index](README.md) · [Configuration and precedence](phase-3-development/configuration.md) · [Architecture](phase-2-design/architecture.md)

Use **test mode only**, a dedicated database, and a test panel. A test Stripe purchase can create a real server in the configured Pterodactyl panel. Checkout is hosted; HTS does not collect card details.

## 1. Configure

Preserve an existing `.env`; copy `.env.example` only if no file exists. Do not use [isolated preview](phase-3-development/onboarding.md) overrides when exercising integration.

Configure order persistence through [SQLSERVER-SETUP.md](SQLSERVER-SETUP.md), a local origin, and test keys:

```dotenv
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_SUCCESS_URL=http://localhost:5147/payment/success?session_id={CHECKOUT_SESSION_ID}
STRIPE_CANCEL_URL=http://localhost:5147/payment/cancel?game={GAME_SLUG}&plan={PLAN_SLUG}&period={BILLING_PERIOD}
STRIPE_CURRENCY=usd
APP_BASE_URL=http://localhost:5147
```

Braced configuration tokens are literal and replaced by the corresponding flow. Current hosted Checkout does not need Stripe.js; the publishable key is not sent to the browser. Secret/webhook keys stay server-only.

Check precedence and `HTS_SKIP_DOTENV`: an isolated-preview session may still suppress the file. Use a dedicated integration session.

## 2. Forward signed events

After installing/authenticating Stripe CLI with a test account:

```powershell
stripe listen --forward-to http://localhost:5147/api/payments/stripe/webhook
```

Place the listener secret in private configuration and restart the application. Keep the listener running. This secret differs from another Stripe endpoint's secret.

For staging, configure an endpoint in Stripe's test environment. Events handled by the backend:

- checkout.session.completed
- checkout.session.async_payment_succeeded
- checkout.session.async_payment_failed
- checkout.session.expired
- customer.subscription.created
- customer.subscription.updated
- customer.subscription.deleted
- customer.subscription.paused
- customer.subscription.resumed
- invoice.paid
- invoice.payment_failed

The handler and [HTTP contract](phase-2-design/api-specification.md) define behavior. Receiving subscription/invoice events does not automatically suspend/delete servers.

## 3. Exercise the purchase

1. Start: `dotnet run --project src/HowToSoftware.Hosting --launch-profile http`.
2. Open `http://localhost:5147/game-hosting/project-zomboid#plans`.
3. Choose plan/period, review, and continue to Stripe.
4. Use Stripe's designated test instrument for the account/environment; never a real card in this scenario.
5. Return to the success page and track progress.
6. Verify signed event receipt and Paid/Provisioning state in the backend.
7. With the panel configured, verify a unique server and Active after installation.
8. Clean up test resources and retain only sanitized evidence.

The success page does not mark an order paid. Repeated events must be idempotent; handler failure returns 500 and permits retry. Do not resend without understanding persisted state and external effects.

## Stripe prices and configuration

Optionally map a recurring price per plan/period. Without mappings, the server builds an inline price. Amount/currency must match the stored quote; the interface does not accept visitor-supplied pricing.

Database catalog configuration does not automatically replace the static presentation service. [Technical design](phase-2-design/technical-design.md) identifies rates and discount sources.

Customer Portal and public invoice management remain closed until real identity/authorization. Choose live configuration only after appropriate integration and operational validation.

This documentation review did not execute production charges, external webhooks, or server creation. See [commerce](COMMERCE-ARCHITECTURE.md), [security](SECURITY-HARDENING.md), and [runbook](phase-6-operations/runbook.md).
