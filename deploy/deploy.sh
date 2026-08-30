#!/usr/bin/env bash
# Pull-based self-deploy for Foodprint on void-server.
#
# Runs from cron every couple of minutes. When origin/main moves it fast-forwards
# the checkout and, if the commit's CI checks are green (best-effort), redeploys.
# GitHub only runs CI (free on this public repo); the Pi owns deployment — no
# secrets, no inbound access. Manual deploy still works:
#   cd ~/foodprint && git pull && docker compose up -d --build

set -euo pipefail
export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

REPO_DIR="${FOODPRINT_REPO_DIR:-$HOME/foodprint}"
REPO_SLUG="pasta0126/Foodprint"
BRANCH="main"

cd "$REPO_DIR"
log() { echo "[$(date -Is)] $*"; }

git fetch --quiet origin "$BRANCH"
local_sha="$(git rev-parse HEAD)"
remote_sha="$(git rev-parse "origin/$BRANCH")"
[ "$local_sha" = "$remote_sha" ] && exit 0

log "new $BRANCH $local_sha -> $remote_sha"

# Best-effort CI gate via the public check-runs API (unauthenticated: 60 req/h).
ci_state="$(
  curl -sf -m 15 -H 'Accept: application/vnd.github+json' \
    "https://api.github.com/repos/$REPO_SLUG/commits/$remote_sha/check-runs" 2>/dev/null \
  | jq -r '
      if (.check_runs | length) == 0 then "unknown"
      elif any(.check_runs[]; .status != "completed") then "pending"
      elif all(.check_runs[]; .conclusion == "success" or .conclusion == "neutral" or .conclusion == "skipped") then "success"
      else "failure" end
    ' 2>/dev/null || echo unknown
)"

if [ "$ci_state" = "unknown" ]; then
  # No checks reported yet — give CI time to register before falling open.
  age=$(( $(date +%s) - $(git show -s --format=%ct "$remote_sha") ))
  if [ "$age" -lt 120 ]; then
    log "no CI status yet (commit ${age}s old), waiting"
    exit 0
  fi
  log "no CI status after ${age}s, deploying anyway"
fi

case "$ci_state" in
  success|unknown) ;;
  pending) log "CI still running, retry next tick"; exit 0 ;;
  failure) log "CI red for $remote_sha, not deploying"; exit 0 ;;
esac

before="$(git rev-parse HEAD)"
git reset --hard "origin/$BRANCH"

if git diff --name-only "$before" HEAD \
   | grep -qE '^(src/|Dockerfile|compose\.yaml|\.dockerignore|Directory\.(Build|Packages)\.props)'; then
  log "rebuilding container"
  docker compose up -d --build
  docker image prune -f >/dev/null 2>&1 || true
  log "deployed $(git rev-parse --short HEAD)"
else
  log "no app changes; checkout at $(git rev-parse --short HEAD), container untouched"
fi
