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

[`deploy/deploy.sh`](../deploy/deploy.sh) runs on the production host, from `~/hts-hosting`:

1. **Refuses anything unexpected:** an image from another repository, or a secrets file that is
   missing or not mode `600`.
2. **Pulls** the commit's image from GHCR. The job logs in with its own short-lived token in an
   isolated Docker config, so it never touches the registry logins of the other stacks on the
   host.
3. **Migrates** the commerce schema with `--migrate-commerce` in a throwaway container, using the
   login in `migrate.env`. If migration fails, the running release is left untouched.
4. **Replaces the container** (`docker compose up -d`) and records the image in `image.env`.
5. **Health-checks** `http://127.0.0.1:5147/health` for up to about 90 seconds.
6. **Rolls back automatically** to the image that was running before if the new one never becomes
   healthy, and fails the job either way so the failure is visible.

Migrations run before the switch and are **not** rolled back, so a migration must keep working
with the previous release (add columns and tables first, remove them in a later release).

## Server layout

Everything belongs to `htsadmin`; nothing needs root.

```
~htsadmin/hts-hosting/                 mode 750
├── .env                               runtime configuration and secrets   (mode 600, never committed)
├── migrate.env                        SQLSERVER_CONNECTION_STRING used for migrations (mode 600)
├── docker-compose.yml                 copied from deploy/ on every deploy
└── image.env                          HTS_IMAGE=<the image currently deployed>
~htsadmin/actions-runner-hts-hosting/  GitHub Actions runner, label hts-production
~htsadmin/.config/systemd/user/actions-runner-hts-hosting.service
```

Both env files are literal `KEY=value` lines: no quotes, and no `${}` interpolation, so a
password containing `$` or quotes is passed through exactly as written.

The runner is a systemd **user** service. Lingering is enabled for `htsadmin`, so it starts at
boot and survives logouts:

```bash
systemctl --user status actions-runner-hts-hosting.service
journalctl --user -u actions-runner-hts-hosting.service -f
```

Data-protection keys live in the `hts-hosting-prototype_dataprotection-keys` Docker volume, so
antiforgery tokens and cookies stay valid across deploys.

## One-time setup

This has been done for `192.168.1.206`. Use these steps to rebuild the host or set up a new one.

### 1. Bootstrap the server

[`deploy/bootstrap-server.sh`](../deploy/bootstrap-server.sh) runs as `htsadmin` without `sudo`:

- creates `~/hts-hosting`;
- captures `.env` from the running `hts-hosting-site` container, so it matches the live
  configuration exactly, without printing any value;
- writes `migrate.env`;
- installs and registers the runner;
- enables lingering and starts the runner as a user service.

It never overwrites an existing `.env` or `migrate.env`.

```bash
# On a machine with admin rights on the repository: a registration token, valid for one hour.
gh api -X POST repos/HowTo-Software/howtoosoftware-hosting-prototype/actions/runners/registration-token --jq .token

# On the server, as htsadmin:
scp deploy/bootstrap-server.sh htsadmin@192.168.1.206:~/
ssh -t htsadmin@192.168.1.206 'RUNNER_TOKEN=<token> bash ~/bootstrap-server.sh'
```

Run interactively, it prompts for the migration connection string. With
`MIGRATE_WITH_RUNTIME_LOGIN=1` it copies the runtime `SQLSERVER_CONNECTION_STRING` instead.

> **Current state:** `migrate.env` reuses the runtime login. To tighten this, create a separate
> principal with DDL rights on the commerce database only (see
> [`SQLSERVER-SETUP.md`](SQLSERVER-SETUP.md)), put its connection string in
> `~/hts-hosting/migrate.env`, and then remove the DDL rights from the runtime login.

### 2. Configure GitHub

These settings carry the security of the pipeline. **The repository is public**, and a
self-hosted runner must never run code from a fork. All are already set:

- **Environment `production`:** required reviewer, *Deployment branches* restricted to `main`.
- **Fork pull requests:** approval required for all external contributors.
- **Runner:** Settings → Actions → Runners shows `hts-production-website-applications`.
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
- **Change configuration:** edit `~/hts-hosting/.env` on the server, then
  `cd ~/hts-hosting && docker compose --env-file image.env up -d`.
- **Logs:** `docker logs -f hts-hosting-site`.
## Local compose files

`docker-compose.yml` at the repository root builds the image locally and is for running the site
on your own machine. Production uses only `deploy/docker-compose.production.yml`.
