# Configure SQL Server

> **Status:** Technical procedure checked against the code; external execution unverified
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

[Index](README.md) · [Configuration and precedence](phase-3-development/configuration.md) · [Architecture](phase-2-design/architecture.md)

The commerce/provisioning schema is separate from the primary HTS database. The backend accesses SQL Server through EF Core and Microsoft.Data.SqlClient; these tables have no public Data API.

## 1. Choose database and identities

Use a dedicated storefront database. Each context has its own migration history (`__EFMigrationsHistory_Commerce` and `__EFMigrationsHistory_Hosting`), but this does not replace a dedicated target with correct permissions.

The following examples create resources; execute them only on the chosen environment's server/database.

```sql
CREATE DATABASE [HowToSoftwareHosting];
GO
CREATE LOGIN [hts_hosting_app] WITH PASSWORD = N'<privately-generated-password>', CHECK_POLICY = ON;
GO
CREATE LOGIN [hts_hosting_migrator] WITH PASSWORD = N'<privately-generated-password>', CHECK_POLICY = ON;
GO
USE [HowToSoftwareHosting];
GO
CREATE USER [hts_hosting_app] FOR LOGIN [hts_hosting_app];
ALTER ROLE db_datareader ADD MEMBER [hts_hosting_app];
ALTER ROLE db_datawriter ADD MEMBER [hts_hosting_app];
CREATE USER [hts_hosting_migrator] FOR LOGIN [hts_hosting_migrator];
ALTER ROLE db_datareader ADD MEMBER [hts_hosting_migrator];
ALTER ROLE db_datawriter ADD MEMBER [hts_hosting_migrator];
ALTER ROLE db_ddladmin ADD MEMBER [hts_hosting_migrator];
GO
```

The production runtime must use `hts_hosting_app` (or an equivalent data-only principal); it must not use `sa`, be an owner, or have DDL rights. The deployment's `migrate.env` must use the separate `hts_hosting_migrator` identity (or an equivalent principal with these permissions) so EF Core can create and alter the commerce schema. Assign `db_ddladmin` only in the commerce database, never to the runtime principal or at the server level.

If a deployment migration fails with SQL Server error 262 (`CREATE TABLE permission denied`), check the database and login named by the failure, then grant the migration database user DDL rights in that database. For example, when the database is `Website_Application_HostingDb` and the migration user is `hts_hosting_migrator`:

```sql
USE [Website_Application_HostingDb];
GO
ALTER ROLE db_ddladmin ADD MEMBER [hts_hosting_migrator];
GO
```

Run this as a database administrator, and ensure `migrate.env` uses that migration login. Do not grant DDL rights to the runtime login to work around a misconfigured `migrate.env`.

If deployment fails with SQL Server error 229 (`SELECT permission was denied` on
`dbo.__EFMigrationsHistory_Commerce`), verify the database and migration login named in the error,
then grant the migration principal read access to that history table:

```sql
USE [Website_Application_HostingDb];
GO
GRANT SELECT ON OBJECT::dbo.__EFMigrationsHistory_Commerce TO [hts_hosting_migrator];
GO
```

Run this as a database administrator, substituting the actual migration database user if needed.
This is separate from DDL permissions and does not grant additional access to the runtime login.
If the grant does not resolve the error, check for an explicit `DENY SELECT` and verify that
`migrate.env` uses the intended login.

In the private environment:

```dotenv
SQLSERVER_CONNECTION_STRING=Server=HOST,1433;Database=HowToSoftwareHosting;User Id=hts_hosting_app;Password=...;Encrypt=True
```

If using `.env`, copy the example only when no file exists; keep values outside Git. Check precedence in the [configuration map](phase-3-development/configuration.md).

## 2. Transport

`SqlServerOptions` forces Encrypt=True, disables PersistSecurityInfo, and requires a server/database. Certificate validation stays enabled unless TrustServerCertificate=True is explicitly chosen. Prefer a trusted certificate; do not disable validation to hide production problems.

A populated invalid string stops startup with a sanitized error. A recognized placeholder selects unconfigured commerce mode; do not automatically connect to another database to hide configuration errors.

## 3. Apply schema

At the root in Development, after checking the dedicated target:

```powershell
dotnet run --project src/HowToSoftware.Hosting --launch-profile http -- --migrate-commerce --seed-commerce
```

The command migrates and exits. Seed is explicit, idempotent, and Development-only: it adds Zomboid, catalog plans, periods/discounts, and an initial profile without inventing prices. Do not use development seed in production.

Production deployment runs the artifact with `--migrate-commerce` and `migrate.env`; see [DEPLOYMENT.md](DEPLOYMENT.md). For manual code execution in a production environment:

```powershell
dotnet run --project src/HowToSoftware.Hosting -c Release --no-launch-profile -- --migrate-commerce
```

Verify the effective environment and migration identity. This command does not create/configure the SQL instance for you.

For the smaller orders context, used when commerce is unconfigured and ConnectionStrings:Hosting is configured:

```powershell
dotnet run --project src/HowToSoftware.Hosting -- --migrate-hosting
```

Choose only the context needed for the target's mode. **Normal startup does not apply migrations.** The worker may query tables at startup, so a missing schema generates query errors without being created automatically.

To review DDL before execution:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/HowToSoftware.Hosting --context CommerceDbContext --output commerce.sql
```

Review scripts privately before handing them to operators; do not version artifacts containing data/secrets.

## 4. Check health and functionality

### Trial schema required for this update

Migration `20261005170245_AddServerTrials` adds `server_trials` and `trial_upgrade_orders`, including unique email/panel-account claims, hashed-token indexes, lifecycle scheduling and restricted relationships to existing `orders`. It does not alter existing columns, seed products or apply itself at application startup. The permanent entitlement row remains after its panel server is deleted.

Apply the pending commerce migration before running this version against a configured commerce database, even when `Trials__Enabled=false`: that flag stops new requests, while the worker still maintains existing trials. The production deployment script already migrates before replacing the application. See [trial configuration](TRIAL-SERVERS.md) for SMTP, panel permissions and the 24-hour/72-hour policy.

Paid Minecraft tiers also require matching game and plan rows in commerce SQL (`Games.Slug` and `HostingPlans.Slug`). `MinecraftPlans__Tiers` configures quotes/resources; it does not create those rows. This migration does not insert Minecraft catalog data, and Development-only seed must not be used on production. Prepare the agreed catalog records before publishing paid tiers.

`GET /health` returns JSON with Healthy/Unhealthy and does not reveal connection details. Configured commerce checks connection opening and `select 1`, not every table/migration.

Without commerce, Healthy represents public-pages mode. The orders worker may still log a configuration error when querying an alternative context without a connection. Real purchases require database/tables as well as Stripe.

## Schema and provider differences

The [data model](phase-2-design/data-model.md) explains orders, customers, services, jobs, events, invoices, and promotions. Commerce uses bigint cents, datetimeoffset, and GUIDs; indexes and constraints protect relationships.

Historical SQL Server port details covered by foundation tests:

| Concern | Current implementation |
| --- | --- |
| Optional-field uniqueness | Filtered IS NOT NULL indexes |
| Multiple cascade paths | Mapping avoids conflicting paths; deployment events depend on the job |
| JSON | nvarchar(max) |
| Primitive collections | EF JSON conversion/mapping |
| Table access | SQL user permissions; no public Data API or provider RLS |

PostgreSQL is not the current provider. SQLite appears in specific tests, not in the production application.

## 5. Opt-in SQL test

Use a **dedicated test database**, not the runtime connection:

```powershell
$env:SQLSERVER_INTEGRATION_TESTS_ENABLED = 'true'
$env:SQLSERVER_TEST_CONNECTION_STRING = 'Server=...;Database=...dedicated-test-db...;User Id=...;Password=...;Encrypt=True'
dotnet test HowToSoftware.Hosting.slnx -c Release --filter Category=Integration
```

The test migrates, writes uniquely identified test records, verifies idempotency/relationships, and removes its data. It does not read SQLSERVER_CONNECTION_STRING. With opt-in disabled, it returns without exercising external integration; a pass does not then verify real SQL.

## Evolution

Services depend on IOrderStore, IBillingStore, and IProvisioningStateStore. Future primary-database integration can replace stores without transferring financial authority to the browser. That future migration is not implemented.

This documentation update did not execute these procedures in production.
