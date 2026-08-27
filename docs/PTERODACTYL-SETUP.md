# Connecting the Pterodactyl panel

Everything in the provisioning pipeline is built and tested. What it needs to run against a real
panel is five values, four of which are configuration and one of which is a secret.

> **The API key is an administrative credential for the whole panel.** It can read every
> customer, create servers and delete them. It does not go in `appsettings.json`, it does not go
> in the repository, and it is never sent to a browser.

---

## 1. Create an Application API key

In the panel: **Admin → Application API → Create New**.

Grant **read and write** on:

| Resource | Why |
|---|---|
| Users | look a customer up by external id, and create one |
| Servers | create, look up by external id, delete |
| Nodes | the connectivity test, and the lab's capacity readout |
| Locations | the connectivity test |
| Nests / Eggs | read the egg's image, startup command and variables |

The key looks like `ptla_` followed by 43 characters. A key beginning `ptlc_` is a **client** key
from *Account → API Credentials* — it authenticates and then refuses every one of these
endpoints, which reads like a permissions problem rather than the wrong key. The application
refuses to start on a `ptlc_` key for exactly that reason.

## 2. Find the nest, egg and location ids

They are in the panel's own URLs:

| Value | Where |
|---|---|
| `NestId` | Admin → Nests — the number in `/admin/nests/view/{id}` |
| `EggId` | Admin → Nests → your Project Zomboid egg — `/admin/nests/egg/{id}` |
| `LocationId` | Admin → Locations — `/admin/locations/view/{id}` |

Both nest and egg are needed: the panel's egg endpoint is nested under its nest, so a correct egg
id under the wrong nest is a 404.

## 3. Supply the configuration

Non-secret values can live in `appsettings.json` (or `appsettings.Development.json`):

```jsonc
"Pterodactyl": {
  "BaseUrl": "https://panel.example.com",   // panel root, no /api/application suffix
  "LocationId": 1,
  "NestId": 5,
  "EggId": 15,
  "PortRange": ["16261-16281"],             // optional; empty lets the panel choose
  "Environment": {                          // egg variables, by env_variable name
    "SERVER_NAME": "HowToSoftware"
  }
}
```

The key comes from outside the repository:

```bash
# Development — stored in the user profile, never in the working tree
dotnet user-secrets --project src/HowToSoftware.Hosting set "Pterodactyl:ApiKey" "ptla_..."

# Windows, current user
setx Pterodactyl__ApiKey "ptla_..."

# Linux / container
export Pterodactyl__ApiKey="ptla_..."
```

The double underscore is the .NET convention for a nested key in an environment variable:
`Pterodactyl__ApiKey` binds to `Pterodactyl:ApiKey`. Every other setting can be supplied the same
way — `Pterodactyl__BaseUrl`, `Pterodactyl__EggId`, and so on.

## 4. Run the connectivity test

```bash
dotnet run --project src/HowToSoftware.Hosting
```

Open **<http://localhost:5147/dev/provisioning>** and press **Test connection**. It calls
`GET /api/application/nodes` and `GET /api/application/locations`, then reads the configured egg.

What the panel shows you:

- **CONNECTED**, the node list, and the egg's name and image — everything is wired up.
- **UNAUTHORIZED** — the key is wrong, revoked, or a client key.
- **FORBIDDEN** — the key is valid but missing an ACL bit from the table above.
- **UNREACHABLE** — DNS, TLS or the wrong `BaseUrl`.
- Connected, but *"the configured egg could not be read"* — `NestId`/`EggId` do not match.

The key is never printed. The panel shows its prefix and length only, which is enough to answer
"did it read the key I set?".

## 5. Deploy a test server

Pick a plan, press **Create test server**, then confirm. The lab shows the exact numbers it will
send before you confirm:

```
memory = 4096      # MiB
cpu    = 300       # percent of one thread
disk   = 25600     # MiB
```

On success it reports the panel's server id, UUID, node and allocation. **Delete test server**
removes it again.

Deletion is guarded twice: the button passes a provisioning request id rather than a server id,
and the service re-reads the server and refuses unless its external id begins with
`hts-test-server:`. A customer's server cannot be reached from this page.

## 6. Opening the lab outside Development

The lab is available in the Development environment automatically. Anywhere else it renders a
closed notice and every action refuses on the server. To open it deliberately on a staging host:

```jsonc
"ProvisioningTest": { "Enabled": true }
```

Do not set this in production. It is a button that creates and deletes real servers.

---

## What the panel receives

Verified against Panel source at **v1.15.1**; the request shape is stable across 1.11.x–1.15.x.

```jsonc
POST /api/application/servers
Authorization: Bearer ptla_…
Accept: application/json
Content-Type: application/json

{
  "external_id": "hts-test-server:{guid}",
  "name": "hts-lab-1a2b3c4d",
  "user": 12,                       // resolved or created from the customer's external id
  "egg": 15,
  "docker_image": "…",              // read from the egg
  "startup": "…",                   // read from the egg
  "environment": { … },             // egg defaults, overlaid with configured values
  "limits":  { "memory": 4096, "swap": 0, "disk": 25600, "io": 500, "cpu": 300 },
  "feature_limits": { "databases": 0, "allocations": 2, "backups": 1 },
  "deploy": { "locations": [1], "dedicated_ip": false, "port_range": [] },
  "start_on_completion": true
}
```

Notes that cost time if you learn them the hard way:

- **`memory` and `disk` are MiB**, not MB. 4 GB is 4096, 25 GB is 25600.
- **`cpu` is a percentage of one thread.** 100 = one thread, 300 = three. It is a ceiling on a
  shared pool, not pinned cores — which is why the site says "300% CPU allocation" and never
  "3 dedicated cores".
- **There is no `nest` field** on this endpoint in Panel 1.x. The panel derives it from the egg.
  Guides that list it as required are wrong.
- **`docker_image` and `startup` are required**, which is why the egg is read first.
- **`io` must be 10–1000.** There is no "unlimited" value; 0 is rejected.
- **`environment` must be present** even when empty. Omitting the key is a 422.
- **Redirects are not followed.** An unauthenticated request gets a 302 to the panel's HTML login
  page, and a client that followed it would see HTTP 200 and call the call a success.

Node placement is the panel's job: the `deploy` block names the location and the panel picks a
public node with memory and disk headroom and a free allocation on it. There is deliberately no
load balancer in this codebase.

<!--
    © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
-->
