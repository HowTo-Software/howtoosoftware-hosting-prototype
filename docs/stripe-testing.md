# Stripe test checkout

Use Stripe test mode only. HTS never collects or stores card data; the customer enters payment
details on Stripe Checkout.

## Configure

Copy `.env.example` to `.env`, then replace the Stripe placeholders with test credentials:

```dotenv
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_SUCCESS_URL=http://localhost:5147/payment/success?session_id={CHECKOUT_SESSION_ID}
STRIPE_CANCEL_URL=http://localhost:5147/payment/cancel?game={GAME_SLUG}&plan={PLAN_SLUG}&period={BILLING_PERIOD}
STRIPE_CURRENCY=usd
```

The publishable key is currently not sent to the browser because hosted Checkout does not need
Stripe.js. The secret and webhook values remain backend-only.

## Forward signed webhooks locally

Install and authenticate the Stripe CLI, then run:

```powershell
stripe listen --forward-to http://localhost:5147/api/payments/stripe/webhook
```

Copy the `whsec_...` value printed by that command into `.env`, restart the application, and keep
the listener running. Configure the same endpoint in Stripe Workbench/Webhooks for deployed test
environments. Subscribe at minimum to:

- `checkout.session.completed`
- `checkout.session.async_payment_succeeded`
- `checkout.session.async_payment_failed`
- `checkout.session.expired`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `customer.subscription.paused`
- `customer.subscription.resumed`
- `invoice.paid`
- `invoice.payment_failed`

## Run the flow

1. Start the app: `dotnet run --project src/HowToSoftware.Hosting --launch-profile http`.
2. Open `http://localhost:5147/game-hosting/project-zomboid#plans`.
3. Select a plan and billing period, review it, and continue to Stripe.
4. Use Stripe's standard successful test card `4242 4242 4242 4242`, any future expiry and any
   CVC/postal code.
5. Return to the HTS success page. It only displays status; it never marks an order paid.
6. Confirm the CLI received a signed event and the order moved from Pending to Paid/Provisioning.
7. With Pterodactyl configured, confirm the background worker creates exactly one server and the
   service becomes Active after installation completes.

Delivering the same Stripe event twice is safe: `stripe_events.stripe_event_id` is unique. A
handler failure releases its event claim and returns HTTP 500 so Stripe can retry.

## Stripe Dashboard work still required

- Create or confirm the test-mode webhook endpoint and its event list.
- Optionally create one recurring Price for each plan/period and put each `price_...` mapping in
  configuration/database. Without mappings, HTS sends an inline recurring price calculated on
  the server.
- Enable and configure Customer Portal only after real HTS authentication exists.
- Switch to live keys/webhook only after end-to-end staging validation and operational review.

The customer billing/portal UI is intentionally not public yet: this prototype has no real
identity provider, so it cannot securely prove which Stripe customer an HTTP visitor owns.
