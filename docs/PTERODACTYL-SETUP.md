# Configure Pterodactyl and the lab

> **Status:** Technical procedure checked against the code; external execution unverified
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

[Index](README.md) · [Configuration and precedence](phase-3-development/configuration.md) · [Architecture](phase-2-design/architecture.md)

The backend creates servers through the panel's **Application API**. The HTS storefront, panel, and nodes are separate applications/services. Public pages work without the panel; options are validated when used.

## 1. Key and permissions

Create a key under **Admin → Application API**. Application keys start with `ptla_`; Client API keys starting with `ptlc_` are unsuitable and rejected by the validator.

This administrative key is backend-only. Do not put it in versioned JSON, browsers, screenshots, or logs.

| Resource | Required flow access |
| --- | --- |
| Users | Read/find by external ID or exact verified email, and create provisioning users |
| Servers | Read/find/create; trials additionally update limits, suspend/unsuspend and delete |
| Nodes and Locations | Connectivity/capacity reads |
| Nests / Eggs | Read game configuration |

Verify permissions against the actual panel version/policy. Do not broaden access just to bypass an error without understanding the failed endpoint.

## 2. Game configuration

LocationId is under Admin → Locations; NestId under Admin → Nests; EggId under that nest's egg. Nest and egg must match because egg queries are nested under the nest.

Nonsecret configuration example:

```json
{
  "Pterodactyl": {
    "BaseUrl": "https://panel.example.com",
    "LocationId": 1,
    "NestId": 5,
    "EggId": 15,
    "PortRange": ["16261-16281"],
    "Environment": {
      "SERVER_NAME": "HowToSoftware"
    }
  }
}
```

BaseUrl is the HTTPS root without /api/application. The egg supplies image, startup, and variables; overrides must be compatible. The location needs an eligible node and free allocations.

In the private environment:

```dotenv
PTERODACTYL_PANEL_URL=https://panel.example.com
PTERODACTYL_APPLICATION_API_KEY=ptla_...
PTERODACTYL_DEPLOY_TESTS_ENABLED=false
```

ASP.NET `Pterodactyl__...` names and Development user-secrets are also supported. See [all aliases](phase-3-development/configuration.md).

## 3. Prepare a development lab

`/dev/provisioning` requires **Development and ProvisioningTest.Enabled=true** simultaneously. The guard closes the connectivity screen without the flag as well as the create action.

In a dedicated session with a test panel:

```powershell
$env:PTERODACTYL_DEPLOY_TESTS_ENABLED = 'true'
dotnet run --project src/HowToSoftware.Hosting --launch-profile http
```

Check for a conflicting native `ProvisioningTest__Enabled` variable, which takes precedence over the alias. Do not enable this lab on a public host. Test payment and Development do not make panel creation simulated.

## 4. Test connectivity

Open `http://localhost:5147/dev/provisioning` and run the test. The service queries nodes, locations, and the configured egg.

| Result | Check |
| --- | --- |
| CONNECTED | Queries completed; capacity/installation still need verification |
| UNAUTHORIZED | Incorrect, revoked, or unsuitable key |
| FORBIDDEN | Insufficient ACL |
| UNREACHABLE | DNS, HTTPS/TLS, and BaseUrl |
| Egg not read | NestId/EggId relationship and permission |

The lab does not display the full secret. Do not publish administrative details or raw panel errors.

## 5. Create and remove a test server

Select a plan, review shown resources, request creation, and confirm. Outpost example:

```text
memory = 4096 MiB
cpu = 300%
disk = 25600 MiB
allocations = 2
databases = 0
backups = 1
```

Creation returns panel identification/resources. Deletion receives a provisioning request ID and rereads the server; the service permits deletion only for the `hts-test-server:` external prefix.

Do not pass a customer server ID to bypass the guard. After testing, remove lab-created resources and restore the flag to false. The flag cannot open the lab in production.

## Creation payload

The implementation sends the following structure to `POST /api/application/servers`, with backend Bearer Authorization. Values are illustrative:

```json
{
  "external_id": "hts-test-server:test-uuid",
  "name": "hts-lab-example",
  "user": 12,
  "egg": 15,
  "docker_image": "egg-configured-image",
  "startup": "egg-configured-startup-command",
  "environment": {},
  "limits": {
    "memory": 4096,
    "swap": 0,
    "disk": 25600,
    "io": 500,
    "cpu": 300
  },
  "feature_limits": {
    "databases": 0,
    "allocations": 2,
    "backups": 1
  },
  "deploy": {
    "locations": [1],
    "dedicated_ip": false,
    "port_range": []
  },
  "start_on_completion": true
}
```

RAM/disk are MiB; CPU is a shared percentage limit without dedicated cores. The payload does not send nest: nest is used to query the egg. Image, startup, and environment are part of the implemented contract.

The client does not follow redirects: a 302 to HTML login must not become a false HTTP 200 success. Deployment provides location and lets the panel choose a node with capacity/allocations; the site implements no load balancer.

The previous guide recorded contract verification against Panel v1.15.1. This is a historical reference; the documentation review did not inspect the installed production version.

Verified purchases use their own stable order-derived external ID, separate from lab resources.
Customer trials use `hts-trial-…` identifiers and their own durable lifecycle; upgrading a trial
keeps that original server/identifier. See [commerce](COMMERCE-ARCHITECTURE.md) and
[tests](phase-4-testing/test-plan.md).

## Customer trials and account matching

Customer trials are enabled with `Trials.Enabled`, not `ProvisioningTest.Enabled`. They also
require commerce SQL and SMTP email confirmation. The default payload uses memory 6144 MiB,
disk 25600 MiB, CPU 0 (unlimited), and swap 0 (disabled). The timer starts after installation;
24 hours later the worker suspends the server, retains its data for another 72 hours, then
deletes unpaid trials. Trial state and entitlement remain in SQL after deletion.

The gateway resolves an existing user by an exact verified email before creating one. It
does not overwrite another user's password or promote a customer to root administrator.
The SQL unique panel-user claim prevents a second trial against the same panel account,
including a request through another game.

Panel operations include `POST /api/application/servers/{id}/suspend`, the corresponding
`unsuspend` POST, `PATCH /api/application/servers/{id}/build`, and guarded deletion. These
are present in the [official Application API routes](https://github.com/pterodactyl/panel/blob/1.0-develop/routes/api-application.php).
Same-server paid conversion updates resource/feature limits and resumes the server; it does
not reinstall the egg, recreate the server or clear its save. Verify the deployed panel's
actual permissions and compatible build payload in a controlled environment.

Minecraft trials require enabled operator-approved profiles with existing nest/egg IDs,
edition, software label, explicit allowed versions and the egg's version-variable name.
Bedrock is not mapped to Java-only Forge/Fabric profiles. The client only posts a public
profile/version selector; it cannot supply the egg, node or startup command.

See [TRIAL-SERVERS.md](TRIAL-SERVERS.md) for full configuration, owner recovery and lifecycle
obligations. No production API call was performed during this local implementation.
