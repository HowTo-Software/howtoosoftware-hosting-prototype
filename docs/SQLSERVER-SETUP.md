# SQL Server commerce database

The commerce and provisioning schema is independent from the primary HTS/Hank database. The
application talks to SQL Server only from the ASP.NET Core backend through EF Core and
`Microsoft.Data.SqlClient`; no commercial table is reachable from browser code.

## 1. Use a dedicated database

Give this application its **own database**, not a schema inside an existing one. Two applications
sharing a database also share a migrations history table, and a deploy of one can then reshape or
roll back the other. Each context here declares its own history table
(`__EFMigrationsHistory_Commerce`, `__EFMigrationsHistory_Hosting`) as a second line of defence,
but a separate database is the first.

```sql
CREATE DATABASE [HowToSoftwareHosting];
GO
```

Create a login for the application that is **not** `sa` and owns nothing else:

```sql
CREATE LOGIN [hts_hosting_app] WITH PASSWORD = N'<generated>', CHECK_POLICY = ON;
GO
USE [HowToSoftwareHosting];
GO
CREATE USER [hts_hosting_app] FOR LOGIN [hts_hosting_app];
ALTER ROLE db_datareader ADD MEMBER [hts_hosting_app];
ALTER ROLE db_datawriter ADD MEMBER [hts_hosting_app];
GO
```

Migrations need DDL rights, which the runtime account should not have. Either run migrations as a
separate deployment principal, or grant `db_ddladmin` only for the duration of the deploy.

Copy `.env.example` to `.env` and fill:

```dotenv
SQLSERVER_CONNECTION_STRING=Server=HOST,1433;Database=HowToSoftwareHosting;User Id=hts_hosting_app;Password=...;Encrypt=True
```

`.env` is git-ignored. In production prefer host environment variables or a secrets vault.

### Transport security

The application forces `Encrypt=True` onto whatever string you supply, so credentials and order
rows are never sent in the clear. Certificate validation stays **on** unless you add
`TrustServerCertificate=True` yourself. Only do that for a server whose certificate your host
cannot validate, and understand that it permits a machine-in-the-middle: anyone able to intercept
the connection can present their own certificate and read everything. Installing a trusted
certificate on the SQL Server is the correct fix.

## 2. Apply the schema and development seed

From the repository root, in Development:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-commerce --seed-commerce
```

The command applies every migration and exits. The seed is idempotent and development-only. It
adds Project Zomboid, the plans already defined by the application, monthly 0%, quarterly 5%
and annual 10%, plus the initial deployment profile. It never invents prices.

For a production schema deployment, omit `--seed-commerce`:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-commerce
```

The smaller orders-only schema, used when `SQLSERVER_CONNECTION_STRING` is absent and only
`ConnectionStrings__Hosting` is set, has its own flag:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-hosting
```

**Startup never migrates anything.** A restart must not be able to reshape a shared server.
Placeholder values are detected and do not open a connection.

To review the DDL before it touches a server:

```powershell
dotnet ef migrations script --idempotent --project src/HowToSoftware.Hosting --context CommerceDbContext --output commerce.sql
```

## 3. Verify

Start the site and request:

```text
GET http://localhost:5147/health
```

The response is limited to `Healthy` or `Unhealthy`; credentials and connection details are never
returned. An unconfigured database reports healthy, because serving the marketing pages with no
database is a supported mode rather than a degraded dependency.

## Schema and access policy

The migration creates customer profiles, games, plans, billing-price mappings, orders, hosting
services, Stripe event claims, provisioning jobs, nodes, deployment profiles/events and minimal
invoice references. Money is stored as `bigint` cents, timestamps as `datetimeoffset`, identifiers
as `uniqueidentifier`, and relational constraints/indexes protect important mappings.

Access control is the database user's permissions: the application login can read and write its
own tables and nothing else. There is no browser-reachable data API in front of this database, so
sensitive mutations have exactly one path — the trusted backend connection.

### Provider differences that survived the port from PostgreSQL

These are behavioural, not cosmetic, and are covered by tests in `SqlServerFoundationTests`:

| Concern | PostgreSQL | SQL Server |
| --- | --- | --- |
| `NULL` in a unique index | many rows allowed | treated as equal, so only one row | 
| Consequence | — | unique indexes on optional columns are filtered `IS NOT NULL` |
| Multiple cascade paths | permitted | rejected (error 1785); `deployment_events` cascades only from its provisioning job |
| JSON columns | `jsonb` | `nvarchar(max)` |
| `string[]` | native array | JSON, via EF primitive collections |
| Row Level Security | enabled per table, Data API roles revoked | not applicable; replaced by database user permissions |

## Opt-in integration test

Ordinary tests never contact SQL Server; the hermetic unit suite uses a throwaway SQLite file, so
anything provider-specific must be asserted here. Use a **dedicated non-production database** and
deliberately set:

```powershell
$env:SQLSERVER_INTEGRATION_TESTS_ENABLED = 'true'
$env:SQLSERVER_TEST_CONNECTION_STRING = 'Server=...;Database=...dedicated-test-db...;User Id=...;Password=...;Encrypt=True'
dotnet test --filter Category=Integration
```

The test applies the migration, exercises customer/order mapping, duplicate Stripe-event
protection, hosting-service/job transitions and Pterodactyl identifier persistence, then removes
only the uniquely named records it created. It never reads `SQLSERVER_CONNECTION_STRING`, so a
production connection cannot be selected accidentally.

## Future primary-database migration

Stripe and provisioning logic depend on `IOrderStore`, `IBillingStore` and
`IProvisioningStateStore`, not on provider-specific APIs. Replacing the SQL Server-backed stores
is the migration seam when the primary HTS database becomes available.
