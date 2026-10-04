#!/usr/bin/env bash
# Deploys one image to the production host. Run by the "deploy" job of
# .github/workflows/ci-cd.yml on the self-hosted runner, from the repository root:
#
#   HTS_IMAGE=ghcr.io/howto-software/howtoosoftware-hosting-prototype:sha-<commit> bash deploy/deploy.sh
#
# Server layout (created once; see docs/DEPLOYMENT.md):
#   /opt/hts-hosting/.env          runtime configuration and secrets (mode 600)
#   /opt/hts-hosting/migrate.env   SQLSERVER_CONNECTION_STRING for a DDL-capable principal (mode 600)
#   /opt/hts-hosting/image.env     written here: the image currently deployed
#
# Steps: pull -> migrate -> replace the container -> health check -> roll back on failure.
# Schema migrations are not rolled back: they must stay compatible with the previous release.

set -Eeuo pipefail

readonly IMAGE_REPOSITORY="ghcr.io/howto-software/howtoosoftware-hosting-prototype"
readonly DEPLOY_DIR="${HTS_DEPLOY_DIR:-/opt/hts-hosting}"
readonly HEALTH_URL="${HTS_HEALTH_URL:-http://127.0.0.1:5147/health}"
# /health is rate limited to 20 requests per window, so stay well under it.
readonly HEALTH_ATTEMPTS=15
readonly HEALTH_INTERVAL_SECONDS=6
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR

log() { printf '[deploy] %s\n' "$*"; }
fail() { printf '::error::%s\n' "$*" >&2; exit 1; }

image="${HTS_IMAGE:-}"
# Compose lets a shell variable override --env-file, which would pin every `up` below - the
# rollback included - to this release. image.env is the single source of the image.
unset HTS_IMAGE
[[ -n "$image" ]] || fail "HTS_IMAGE is not set."
# Only this repository's images, by an explicit tag or digest, are ever deployed.
reference="${image#"$IMAGE_REPOSITORY"}"
[[ "$reference" != "$image" && "$reference" =~ ^(:[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}|@sha256:[a-f0-9]{64})$ ]] \
  || fail "Refusing to deploy unexpected image reference: $image"

[[ -d "$DEPLOY_DIR" ]] || fail "$DEPLOY_DIR does not exist. Bootstrap the server first (docs/DEPLOYMENT.md)."
for file in .env migrate.env; do
  [[ -f "$DEPLOY_DIR/$file" ]] || fail "$DEPLOY_DIR/$file is missing. Bootstrap the server first (docs/DEPLOYMENT.md)."
  if [[ "$(stat -c '%a' "$DEPLOY_DIR/$file")" != "600" ]]; then
    fail "$DEPLOY_DIR/$file holds secrets and must be mode 600."
  fi
done

install -m 644 "$SCRIPT_DIR/docker-compose.production.yml" "$DEPLOY_DIR/docker-compose.yml"
cd "$DEPLOY_DIR"

compose() { docker compose --env-file image.env -f docker-compose.yml "$@"; }

write_image_env() { printf 'HTS_IMAGE=%s\n' "$1" > image.env.tmp && mv image.env.tmp image.env; }

wait_healthy() {
  local attempt status
  for ((attempt = 1; attempt <= HEALTH_ATTEMPTS; attempt++)); do
    sleep "$HEALTH_INTERVAL_SECONDS"
    status="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$HEALTH_URL" || true)"
    if [[ "$status" == "200" ]]; then
      log "Healthy after ${attempt} check(s)."
      return 0
    fi
    log "Health check ${attempt}/${HEALTH_ATTEMPTS}: HTTP ${status}"
  done
  return 1
}

# The image that is running now, so a failed release can be put back. On the first pipeline
# deploy this is the hand-built image from the previous manual setup.
previous_image="$(docker inspect --format '{{.Config.Image}}' hts-hosting-site 2>/dev/null || true)"
log "Currently running: ${previous_image:-<nothing>}"
log "Deploying: $image"

docker pull --quiet "$image"

log "Applying commerce migrations."
docker run --rm --env-file migrate.env --read-only --tmpfs /tmp \
  --security-opt no-new-privileges:true "$image" --migrate-commerce \
  || fail "Migrations failed; the running release was left untouched."

write_image_env "$image"

if compose up -d --no-build --remove-orphans && wait_healthy; then
  log "Deployed $image"
  # Keep recent releases of this repository's image for fast rollback; GHCR keeps the rest.
  docker image prune --all --force \
    --filter "label=org.opencontainers.image.source=https://github.com/HowTo-Software/howtoosoftware-hosting-prototype" \
    --filter "until=336h" >/dev/null || true
  exit 0
fi

log "Release is unhealthy. Recent logs:"
docker logs --tail 80 hts-hosting-site 2>&1 || true

if [[ -n "$previous_image" && "$previous_image" != "$image" ]]; then
  log "Rolling back to $previous_image"
  write_image_env "$previous_image"
  if compose up -d --no-build --remove-orphans && wait_healthy; then
    fail "Deploy of $image failed its health check; rolled back to $previous_image."
  fi
  fail "Deploy of $image failed and the rollback to $previous_image is also unhealthy. Investigate now."
fi

fail "Deploy of $image failed its health check and there is no previous release to roll back to."
