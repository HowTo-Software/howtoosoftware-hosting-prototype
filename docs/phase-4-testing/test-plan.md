# Test plan and evidence

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Risk-based strategy

| Layer | What it must protect | Existing validation |
| --- | --- | --- |
| Domain/catalog | Product, units, rates, discounts, and total | Catalog, mapping, billing, and custom build tests |
| Purchase | Invalid selection, return origin, snapshot, promotions | PurchaseFlowTests |
| Webhook | Signature, limits, repetition, and retry errors | StripeWebhookEndpointTests, PurchaseFlowTests |
| Fulfillment | Idempotency, states, recovery, and installation | OrderFulfillmentTests, PterodactylTests |
| Trials | Verified ownership, one entitlement, expiry, retention and same-server conversion | ServerTrialLifecycleTests, PterodactylTrialGatewayTests, TrialEmailTests, TrialEndpointTests |
| Public promotions | Explicit publication consent, valid code, safe copy and bounded announcements | PublicPromotionTests |
| Persistence | Mapping, SQL Server, and migrations | SqlServerFoundationTests and opt-in integration |
| Boundaries | Antiforgery, headers, rate limits, and minimal status | SecurityBoundaryTests, ShippedConfigurationTests |
| Presentation | Language, images, content, and declared simulation | LocalizationTests, library/service tests |
| Browser | Routes, restored DOM, themes, mobile, and fallback | Redesign Chromium reports |

## Normal cycle commands

```powershell
dotnet restore HowToSoftware.Hosting.slnx
dotnet build HowToSoftware.Hosting.slnx -c Release --no-restore
dotnet test HowToSoftware.Hosting.slnx -c Release --no-build --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
```

CI runs this cycle and retains TRX for 14 days. A class filter can speed diagnosis; before publishing code changes, follow pipeline checks.

## Evidence recorded on 2026-10-05

The current third iteration was checked in an isolated local preview: Release compilation with zero warnings/errors, **627 passing tests**, zero failures and zero skipped tests; **70 main Chromium checks + 7 trial checks**, all passing. The final automated command was `dotnet test HowToSoftware.Hosting.slnx -c Release --no-restore --nologo` with `HTS_SKIP_DOTENV=1`.

Before publication, dependencies were restored after synchronizing with `origin/main` at `277e7b1`. Release build and all 627 tests passed again. NuGet reported no outdated direct packages and no vulnerable packages in either project, including transitives. Available transitive upgrades are not forced over the versions selected by EF Core and other SDKs. The production database and integrations remain outside these checks.

Artifacts: [qa-v3.json](../frontend/qa-v3.json), [qa-v3-trial.json](../frontend/qa-v3-trial.json), [current screenshots and historical results](../FRONTEND-REDESIGN.md).

The browser was headless Chromium with SwiftShader. Current coverage focuses on home, infrastructure and trials at 320/390/768/1024/1440 px, EN/PT-BR, dark/light themes, reduced motion, no JavaScript, WebGL lifecycle, keyboard disclosures and enhanced navigation. Supplemental checks cover Java/Bedrock entry and explicit verification POST without real activation. Prior catalog, product and review evidence remains labelled historical. These results do not prove behavior on every device or production integrations.

Pterodactyl HTTP, SMTP and trial persistence tests use isolated doubles or test stores. No real email, charge, server lifecycle action or production migration was performed. The `AddServerTrials` migration was generated and remains unapplied.

## Opt-in SQL Server

Normal execution does not access production databases. `SqlServerIntegrationTests` exercises an external SQL target only when configured with:

```text
SQLSERVER_INTEGRATION_TESTS_ENABLED=true
SQLSERVER_TEST_CONNECTION_STRING=<dedicated-test-database>
```

Run `dotnet test HowToSoftware.Hosting.slnx -c Release --filter Category=Integration` after configuring the test target. This test migrates and writes to that database. **Do not use a production connection.**

With opt-in disabled, the test returns without performing the SQL scenario; the passing test count does not mean external integration was exercised.

## Acceptance criteria

Release build and relevant tests must pass. For visual changes, check [browser cases](test-cases.md), especially route sequences, keyboard access, SSR forms, languages, and fallbacks. For integrations, record the test environment, external effect, and cleanup.

Documentation-only review checks consistency, links, and paths; it does not change runtime or repeat purchases against external services.
