# Commerce and provisioning foundation

The implemented server-side flow is:

```text
game + plan + billing period
        -> server-side price
        -> internal order
        -> Stripe Checkout subscription
        -> verified, idempotent webhook
        -> paid order + background queue
        -> idempotent Pterodactyl create
        -> installation polling
        -> active hosting service
```

The browser never submits a price, resource limit, node or paid state. A redirect to
`/payment/success` is display-only; only a Stripe event whose signature and amount validate can
queue provisioning.

## Persistence modes

- With `SUPABASE_DB_CONNECTION_STRING`, all new commerce/order/provisioning state uses the
  independent PostgreSQL schema through Npgsql.
- Without it, the application uses a local SQLite order store so pages and automated tests work
  without credentials. It does not pretend Supabase is connected.

The process-local channel keeps webhook responses fast. On startup the worker requeues both paid
orders that never started and in-progress orders interrupted by a restart. Pterodactyl external
IDs are derived from the order UUID, so retries return the existing server instead of creating a
duplicate.

## Current production boundary

The payment/provisioning backend foundation is implemented, but public account billing pages,
Customer Portal links, invoice downloads, administrative retry controls and subscription-driven
server suspension remain disabled until HTS has real authentication and authorization. Exposing
those by email or query-string ownership would create an account-takeover/data-leak path.

The existing `IAuthenticationGateway` deliberately authenticates nobody. When HTS identity is
connected, pass its stable user ID into `CheckoutRequest`; the PostgreSQL customer profile mapping
then reuses the stored Stripe customer rather than creating one for each purchase.
