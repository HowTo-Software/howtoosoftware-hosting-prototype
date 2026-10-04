#!/usr/bin/env bash
# One-time preparation of the production host for the CI/CD pipeline. Run it ON the server, as
# the account the runner will use (htsadmin, a member of the docker group), from a checkout or
# a copy of this file. It prompts for sudo where it needs it.
#
#   RUNNER_TOKEN=<registration token> bash bootstrap-server.sh
#
# Get a registration token (valid for one hour) from a machine with repository admin rights:
#   gh api -X POST repos/HowTo-Software/howtoosoftware-hosting-prototype/actions/runners/registration-token --jq .token
#
# Safe to re-run: every step checks before it acts.

set -Eeuo pipefail

readonly REPO_URL="https://github.com/HowTo-Software/howtoosoftware-hosting-prototype"
readonly DEPLOY_DIR="/opt/hts-hosting"
readonly LEGACY_ENV="/home/malaio/howtoosoftware-hosting-prototype/.env"
readonly RUNNER_DIR="$HOME/actions-runner-hts-hosting"
readonly RUNNER_VERSION="2.337.0"
readonly RUNNER_SHA256="70920811a4f8ad4328818682bca5c6469c1c942fab52448868071d0063816613"
RUNNER_NAME="hts-production-$(hostname)"
readonly RUNNER_NAME
readonly RUNNER_LABELS="hts-production"

log() { printf '[bootstrap] %s\n' "$*"; }
fail() { printf '[bootstrap] ERROR: %s\n' "$*" >&2; exit 1; }

user="$(id -un)"
[[ "$user" != "root" ]] || fail "Run as the runner account, not root."
id -nG "$user" | grep -qw docker || fail "$user must be in the docker group."

# 1. Deployment directory, owned by the runner account.
if [[ ! -d "$DEPLOY_DIR" ]]; then
  log "Creating $DEPLOY_DIR"
  sudo install -d -o "$user" -g "$user" -m 750 "$DEPLOY_DIR"
fi

# 2. Runtime configuration: carried over from the hand-run deployment it replaces.
if [[ ! -f "$DEPLOY_DIR/.env" ]]; then
  if sudo test -f "$LEGACY_ENV"; then
    log "Copying runtime configuration from $LEGACY_ENV"
    sudo install -o "$user" -g "$user" -m 600 "$LEGACY_ENV" "$DEPLOY_DIR/.env"
  else
    fail "No $DEPLOY_DIR/.env and no $LEGACY_ENV to copy. Create $DEPLOY_DIR/.env (mode 600) from .env.example."
  fi
fi
chmod 600 "$DEPLOY_DIR/.env"

# 3. Migration credential: a separate, DDL-capable principal. Never the runtime login.
if [[ ! -f "$DEPLOY_DIR/migrate.env" ]]; then
  log "Enter the SQL Server connection string for the MIGRATION principal (input hidden)."
  read -r -s -p "SQLSERVER_CONNECTION_STRING: " migrate_connection
  printf '\n'
  [[ -n "$migrate_connection" ]] || fail "A migration connection string is required."
  (umask 077 && printf 'SQLSERVER_CONNECTION_STRING=%s\n' "$migrate_connection" > "$DEPLOY_DIR/migrate.env")
  unset migrate_connection
fi
chmod 600 "$DEPLOY_DIR/migrate.env"

# 4. Self-hosted GitHub Actions runner, as a systemd service under this account.
if [[ ! -f "$RUNNER_DIR/.runner" ]]; then
  [[ -n "${RUNNER_TOKEN:-}" ]] || fail "Set RUNNER_TOKEN to a repository registration token."
  mkdir -p "$RUNNER_DIR"
  cd "$RUNNER_DIR"
  archive="actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
  log "Downloading runner $RUNNER_VERSION"
  curl -fsSL -o "$archive" "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${archive}"
  echo "${RUNNER_SHA256}  ${archive}" | sha256sum -c - || fail "Runner download failed its checksum."
  tar xzf "$archive"
  rm -f "$archive"
  ./config.sh --unattended --replace \
    --url "$REPO_URL" \
    --token "$RUNNER_TOKEN" \
    --name "$RUNNER_NAME" \
    --labels "$RUNNER_LABELS" \
    --work _work
  sudo ./svc.sh install "$user"
fi

cd "$RUNNER_DIR"
sudo ./svc.sh start >/dev/null 2>&1 || true
sudo ./svc.sh status | sed -n '1,5p'

log "Done. $DEPLOY_DIR is ready and the runner '$RUNNER_NAME' is labelled '$RUNNER_LABELS'."
