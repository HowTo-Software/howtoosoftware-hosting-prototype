# Pages and HTTP contracts

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

The application does not expose a complete public accounts, commerce CRUD, or administration API. Checkout begins with an SSR form. The endpoints below are the contracts present in the code.

## Pages

| Route | Behavior |
| --- | --- |
| `/` | Homepage |
| `/game-hosting` | Catalog |
| `/game-hosting/project-zomboid` | Product and plans; `#plans` is an anchor |
| `/game-hosting/minecraft` | Trial entry and configured upgrade tiers |
| `/trial` | Selection, confirmation landing, owner recovery and status |
| `/game-hosting/{GameSlug}/review` | SSR review of a sellable plan |
| `/infrastructure`, `/hardware` | Same infrastructure page |
| `/project-zomboid` | Permanent redirect to the current product |
| `/login`, `/register` | Local demonstrations |
| `/payment/success` | Tracking by `session_id`, without confirmation authority |
| `/payment/cancel` | Return/cancellation display |
| `/dev/provisioning` | Lab closed without Development + explicit flag |
| `/error`, `/not-found` | Error and missing-content handling |

## Review: GET and POST

GET accepts `plan`, `period`, and optional `promo`. Period slugs: `monthly`, `quarterly`, `annual`. A missing/invalid period defaults to monthly. A nonexistent, incompatible, or unpriced game/plan displays the unavailable-review state.

Example:

```text
/game-hosting/project-zomboid/review?plan=zomboid-4gb&period=monthly
```

POST uses the Blazor form `checkout`, antiforgery, `PlanSlug`, `Period`, and `PromoCode`. Internal form/token fields must come from the page, not an arbitrary manually constructed request. There is no `POST /api/checkout`.

The server resolves selection, validates promotion and origin, persists the order, and requests a session. Success redirects to Stripe; rejection/missing configuration displays a review message. The browser sends no total, resource limits, node, or paid state.

## POST /api/payments/stripe/webhook

Body: original Stripe JSON. Required header: `Stripe-Signature`. Antiforgery is disabled because the signature authenticates the external message.

| Result | HTTP |
| --- | ---: |
| Verified and handled event, including repeats under idempotency rules | 200 |
| Missing/invalid signature | 400 |
| Body above the 256 KiB limit, according to endpoint checks | 413 |
| Request limit exceeded | 429 |
| Processing failure requiring Stripe retry | 500 |

The endpoint limits Content-Length and read content; it is not a general upload endpoint. Failures do not return credentials or the complete JSON. Checkout, subscription, and invoice events are listed in [stripe-testing.md](../stripe-testing.md).

## GET /api/payments/status?session_id=…

Minimal response:

```json
{
  "status": "Active",
  "stageIndex": 5,
  "isTerminal": true
}
```

A missing, empty, longer-than-128-character, or unknown ID returns 404. The response includes no email, amount, internal order ID, or server. The session ID is a progress-query capability, not account ownership.

Public stages: not started -1; preparing/paid 1; node 2; resources 3; installation 4; active 5. Failures retain the observed stage according to the mapping. See `PaymentEndpoints` for the exact function.

## GET /culture/select

Parameters: `culture` and `redirect`. Sets a 365-day language cookie and redirects. Unresolved cultures use EN. The destination must be a local path: external destinations, `//host` forms, leading backslash forms, and control characters fall back to `/`.

## GET /health

Returns JSON `{"status":"Healthy"}` (200) or `{"status":"Unhealthy"}` (503). With commerce SQL configured, opens a connection and executes `select 1`. Without a commerce database, public-pages mode is Healthy. This endpoint does not verify Stripe, Pterodactyl, complete migrations, the orders fallback, or game telemetry.

## HTTP limits

Fixed one-minute windows, partitioned by IP and request type, without queuing: pages 300; checkout POST 10; status 60; webhook 120; health 20; trial request/recovery 3; trial confirmation 10. Excess returns 429 and `Retry-After: 60`.

Kestrel limits the general body to 1 MiB, 64 headers, and 32 KiB total header size; the webhook has a smaller body limit. Production uses HTTPS redirect 308 and HSTS. Forwarded headers depend on a known proxy.

Sources: [routes](../../src/HowToSoftware.Hosting/Models/SiteRoutes.cs), [PaymentEndpoints](../../src/HowToSoftware.Hosting/Endpoints/PaymentEndpoints.cs), [CultureEndpoints](../../src/HowToSoftware.Hosting/Localization/CultureEndpoints.cs), [SecurityHardening](../../src/HowToSoftware.Hosting/Infrastructure/Security/SecurityHardening.cs).

## Trial forms

`POST /trials/request` accepts `Email`, `DisplayName`, `GameSlug`, and optional Minecraft `ProfileId`/`Version`. `POST /trials/confirm` accepts `VerificationToken`; the verification GET is a landing page and never provisions a server. `POST /trials/recover` accepts `Email`. All three enforce antiforgery and dedicated IP limits. Local redirects carry status only, never email or tokens. Confirmation issues the HTTP-only `hts.trial-access` capability cookie; SQL verifies ownership and entitlement for checkout. Email recovery returns the existing trial and never resets its claim. See [trial guide](../TRIAL-SERVERS.md).
