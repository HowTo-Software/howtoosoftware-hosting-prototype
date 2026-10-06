# Set up and run the project

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

## Requirements

.NET 10 SDK and Git. Docker/Compose are optional for image verification. SQL Server, Stripe CLI, and a Pterodactyl panel are needed only for integration scenarios. Node/npm is not required to serve or build the site.

Check `dotnet --info` and enter the root containing `HowToSoftware.Hosting.slnx`. The hosting repository is separate from the HTS website creation project.

```powershell
git clone https://github.com/HowTo-Software/howtoosoftware-hosting-prototype.git
Set-Location howtoosoftware-hosting-prototype
dotnet restore HowToSoftware.Hosting.slnx
dotnet build HowToSoftware.Hosting.slnx -c Release --no-restore
dotnet test HowToSoftware.Hosting.slnx -c Release --no-build
```

For an existing checkout, do not clone over it: follow the [Git workflow](git-workflow.md).

## Isolated preview without real integrations

Use a dedicated PowerShell session. The block disables `.env` discovery, fixes Development, and overrides database/payment/panel options through command-line arguments. Existing personal configuration is therefore not used by this preview.

```powershell
$env:HTS_SKIP_DOTENV = 'true'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project src/HowToSoftware.Hosting --launch-profile http -- --SQLSERVER_CONNECTION_STRING=REPLACE_ME --SqlServer:ConnectionString=REPLACE_ME --ConnectionStrings:Commerce=REPLACE_ME '--ConnectionStrings:Hosting= ' '--Stripe:SecretKey= ' '--Stripe:WebhookSecret= ' --Pterodactyl:ApiKey=REPLACE_ME --ProvisioningTest:Enabled=false --Site:BaseUrl=http://localhost:5147 '--AllowedHosts=localhost;127.0.0.1;[::1]'
```

Open `http://localhost:5147`. The homepage, catalog, product, infrastructure, and review should render; checkout explains the missing configuration. This does not simulate a charge or create a server. At startup, the worker queries orders through the alternative context and may log an uninitialized ConnectionString and `Could not scan for unprovisioned orders`. This is a known limitation of database-free mode; pages remain functional, and the block above does not access an external database.

If the port is busy, stop only the development instance you started, or select another port and adjust BaseUrl.

`HTS_SKIP_DOTENV` stays set in that session until removed or the session is closed. Use another session for integrations and follow the guides below.

## Development with test integrations

1. Prepare a **dedicated development database**, with the connection and migrations described in the [SQL Server guide](../SQLSERVER-SETUP.md).
2. If `.env` does not exist, copy the example without overwriting:
   `if (!(Test-Path .env)) { Copy-Item .env.example .env }`.
3. Fill only test credentials; check [variables and precedence](configuration.md).
4. Configure [Stripe test mode](../stripe-testing.md) and webhook forwarding.
5. Configure [Pterodactyl](../PTERODACTYL-SETUP.md) with a test panel/resources. Server creation is a real action even when payment is in test mode.
6. Start with the `http` profile, without the isolated-mode overrides.

Never reuse a production connection string for SQL tests. The opt-in test uses its own variable and may migrate/write to the target.

## Initial troubleshooting

| Symptom | Check |
| --- | --- |
| SDK does not support net10.0 | Installed SDK and `dotnet --info` |
| Port 5147 is busy | Existing local instance / launch profile |
| Release build fails on a warning | Fix the warning; Release treats warnings as errors |
| Text displays as a resource key | .resx family, localizer type, and parity |
| Site opens, payment button disabled | Effective Stripe options |
| Worker cannot query orders | Database, migrations, and permissions; not just public pages |
| Isolated preview logs an empty connection | Known database-free worker limitation; configure a store to test purchases |
| Layout unchanged after isolated CSS edit | Rebuild, .styles.css file, and browser cache |

See [standards](coding-standards.md), the [test plan](../phase-4-testing/test-plan.md), and [troubleshooting](../user/troubleshooting.md).
