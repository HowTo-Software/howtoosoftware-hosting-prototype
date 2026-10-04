# Deployment and CI/CD

Every change to `main` is built, tested, packaged as a container image and, after a reviewer
approves it, deployed to the production host. Pull requests get the same build, tests and image
build, but never reach production.

```
pull request ──► Build and test ──► Container image (built, not pushed)

push to main ──► Build and test ──► Container image ──► [approval] ──► Deploy to production
                  ubuntu-latest      ghcr.io, sha-<commit>   "production"   self-hosted runner
                                                             environment    on 192.168.1.206
```

| Piece | Where |
| --- | --- |
| Workflow | [`.github/workflows/ci-cd.yml`](../.github/workflows/ci-cd.yml) |
| Deploy script (runs on the server) | [`deploy/deploy.sh`](../deploy/deploy.sh) |
| Production compose file | [`deploy/docker-compose.production.yml`](../deploy/docker-compose.production.yml) |
| One-time server setup | [`deploy/bootstrap-server.sh`](../deploy/bootstrap-server.sh) |
| Image | `ghcr.io/howto-software/howtoosoftware-hosting-prototype:sha-<commit>` |
| Live site | <https://host.howto.software> (reverse proxy → `192.168.1.206:5147`) |

## What a deploy does

[`deploy/deploy.sh`](../deploy/deploy.sh) runs on the production host, from `/opt/hts-hosting`:

1. **Refuses anything unexpected:** an image from another repository, or a secrets file that is
   missing or not mode `600`.
2. **Pulls** the commit's image from GHCR.
3. **Migrates** the commerce schema with `--migrate-commerce` in a throwaway container. This uses
   the DDL-capable login in `migrate.env`, never the runtime login. If migration fails, the
   running release is left untouched.
4. **Replaces the container** (`docker compose up -d`) and records the image in `image.env`.
5. **Health-checks** `http://127.0.0.1:5147/health` for up to about 90 seconds.
6. **Rolls back automatically** to the image that was running before if the new one never becomes
   healthy, and fails the job either way so the failure is visible.

Migrations run before the switch and are **not** rolled back, so a migration must keep working
with the previous release (add columns and tables first, remove them in a later release).

## Server layout

```
/opt/hts-hosting/             owner htsadmin, mode 750
├── .env                      runtime configuration and secrets   (mode 600, never committed)
├── migrate.env               SQLSERVER_CONNECTION_STRING for the migration login (mode 600)
├── docker-compose.yml        copied from deploy/ on every deploy
└── image.env                 HTS_IMAGE=<the image currently deployed>
~htsadmin/actions-runner-hts-hosting/   GitHub Actions runner (systemd service, label hts-production)
```

Data-protection keys live in the `hts-hosting-prototype_dataprotection-keys` Docker volume, so
antiforgery tokens and cookies stay valid across deploys.

## One-time setup

### 1. Bootstrap the server

The bootstrap script creates `/opt/hts-hosting`, copies the existing `.env` from the hand-run
deployment it replaces (`/home/malaio/howtoosoftware-hosting-prototype/.env`), prompts for the
migration connection string, and installs the runner as a service. It needs `sudo`, so run it
interactively on the server as `htsadmin`:

```bash
# On a machine with admin rights on the repository: a registration token, valid for one hour.
gh api -X POST repos/HowTo-Software/howtoosoftware-hosting-prototype/actions/runners/registration-token --jq .token

# On the server, as htsadmin:
scp deploy/bootstrap-server.sh htsadmin@192.168.1.206:~/
ssh -t htsadmin@192.168.1.206 'RUNNER_TOKEN=<token> bash ~/bootstrap-server.sh'
```

For the migration login, follow [`SQLSERVER-SETUP.md`](SQLSERVER-SETUP.md): a separate principal
with DDL rights on the commerce database only. The runtime login in `.env` should not have them.

### 2. Configure GitHub

These settings carry the security of the pipeline. **The repository is public**, and a
self-hosted runner must never run code from a fork.

- **Environment `production`:** Settings → Environments. Add yourself as a *required reviewer*,
  and set *Deployment branches and tags* to **Selected branches: `main`**.
- **Fork pull requests:** Settings → Actions → General → *Approval for running fork pull request
  workflows*: **Require approval for all external contributors**.
- **Runner:** Settings → Actions → Runners shows `hts-production-<hostname>` as *Idle*.
- **Package:** after the first push to `main`, the `howtoosoftware-hosting-prototype` package
  appears under the organisation's packages. Leave it linked to this repository so the deploy
  job's `GITHUB_TOKEN` can pull it.

Only the `deploy` job uses the self-hosted runner, and only on a push to `main` or a manual run
from `main`. Pull requests run entirely on GitHub-hosted runners.

## Day-to-day

- **Deploy:** merge to `main`, then approve the *Deploy to production* job in the Actions run.
- **Redeploy `main`:** Actions → CI/CD → *Run workflow* with the tag left empty.
- **Roll back:** Actions → CI/CD → *Run workflow*, set `image_tag` to an earlier
  `sha-<commit>`. This skips the build and deploys that image, still behind the approval.
- **Change configuration:** edit `/opt/hts-hosting/.env` on the server, then
  `cd /opt/hts-hosting && docker compose --env-file image.env up -d`.
- **Logs:** `docker logs -f hts-hosting-site`.

## Local compose files

`docker-compose.yml` at the repository root builds the image locally and is for running the site
on your own machine. Production uses only `deploy/docker-compose.production.yml`.
