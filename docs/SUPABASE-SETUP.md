# Supabase commerce database

The commerce and provisioning schema is independent from the primary HTS/Hank database. The
application talks to Supabase PostgreSQL only from the ASP.NET Core backend through EF Core and
Npgsql; no commercial table is writable from browser code.

## 1. Create a dedicated project

Create a Supabase project and open **Connect** in its dashboard. For a persistent backend, use:

- the direct connection when the host supports IPv6; or
- the session pooler when a persistent IPv4 connection is required.

Do not use the transaction pooler for EF migrations. Copy `.env.example` to `.env` and fill:

```dotenv
SUPABASE_URL=https://PROJECT_ID.supabase.co
SUPABASE_PUBLISHABLE_KEY=sb_publishable_...
SUPABASE_SECRET_KEY=sb_secret_...
SUPABASE_DB_CONNECTION_STRING=postgresql://USER:PASSWORD@HOST:5432/postgres
```

Only `SUPABASE_DB_CONNECTION_STRING` is required by the current backend. The publishable and
secret API keys are reserved for a future, intentionally designed Data API integration. The
secret key and database password are server-only.

## 2. Apply the schema and development seed

From the repository root, in Development:

```powershell
Copy-Item .env.example .env
# Edit .env locally. It is git-ignored.
dotnet run --project src/HowToSoftware.Hosting -- --migrate-commerce --seed-commerce
```

The command applies every migration and exits. The seed is idempotent and development-only. It
adds Project Zomboid, the plans already defined by the application, monthly 0%, quarterly 5%
and annual 10%, plus the initial deployment profile. It never invents prices.

For a production schema deployment, omit `--seed-commerce` and run the migration command from a
controlled deployment job:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-commerce
```

Startup never applies Supabase migrations automatically. Placeholder values are detected and do
not initiate a network connection or database initialization.

## 3. Verify

Start the site and request:

```text
GET http://localhost:5147/health/supabase
```

The response is limited to `Connected`, `Not configured`, or `Error`; credentials and connection
details are never returned.

## Schema and access policy

The migration creates customer profiles, games, plans, billing-price mappings, orders, hosting
services, Stripe event claims, provisioning jobs, nodes, deployment profiles/events and minimal
invoice references. Money is stored as `bigint` cents, timestamps as `timestamptz`, identifiers
as UUIDs, and relational constraints/indexes protect important mappings.

Row Level Security is enabled on every commerce table with no browser policy. Access for the
Supabase `anon` and `authenticated` Data API roles is explicitly revoked. Sensitive mutations
therefore have one path: the trusted HTS backend database connection. If customer-facing Data API
access is introduced later, add narrowly scoped policies before granting anything.

## Opt-in integration test

Ordinary tests never contact Supabase. Use a separate test project/database and deliberately set:

```powershell
$env:SUPABASE_INTEGRATION_TESTS_ENABLED = 'true'
$env:SUPABASE_TEST_DB_CONNECTION_STRING = 'postgresql://...dedicated-test-project...'
dotnet test --filter Category=Integration
```

The test applies the migration, exercises customer/order mapping, duplicate Stripe-event
protection, hosting-service/job transitions and Pterodactyl identifier persistence, then removes
only the uniquely named records it created. It never reads `SUPABASE_DB_CONNECTION_STRING`, so a
normal application/production connection cannot be selected accidentally.

## Future primary-database migration

Stripe and provisioning logic depend on `IOrderStore`, `IBillingStore` and
`IProvisioningStateStore`, not Supabase-specific APIs. Replacing the PostgreSQL-backed stores is
the migration seam when the primary HTS database becomes available.
