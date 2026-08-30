# Deployment — void-server

Foodprint runs on **void-server** (Raspberry Pi, `linux/arm64`) as a Docker
Compose service behind the existing Traefik proxy. See the repo `README.md` for
the container/proxy details and `../compose.yaml` for the full config.

## How deploys happen

- **GitHub Actions** (`.github/workflows/ci.yml`) runs **CI only** — build, tests,
  migration check, a Dockerfile sanity build. It never touches the server.
  Runners are GitHub-hosted; free with unlimited minutes because the repo is
  public.
- **void-server deploys itself.** `deploy.sh` runs from the deploy user's cron
  every ~2 minutes: when `origin/main` moves and that commit's CI checks are
  green, it fast-forwards the checkout and, if anything app-relevant changed,
  runs `docker compose up -d --build`. No secrets, no inbound access to the Pi.

Manual deploy always works too:

```bash
ssh void-server 'cd ~/foodprint && git pull && docker compose up -d --build'
```

## One-time install on void-server

```bash
ssh void-server
cd ~/foodprint && git pull
mkdir -p ~/.local/log
( crontab -l 2>/dev/null; \
  echo '*/2 * * * * /home/pasta0126/foodprint/deploy/deploy.sh >> /home/pasta0126/.local/log/foodprint-deploy.log 2>&1' \
) | crontab -
```

Log: `~/.local/log/foodprint-deploy.log`.

## CI gate details

`deploy.sh` reads the commit's checks from the public GitHub API
(`/commits/{sha}/check-runs`, unauthenticated). States: `success`/`neutral`/
`skipped` → deploy; `pending` → wait for the next tick; `failure` → skip and log.
If the API returns nothing (commit too new / rate-limited) it waits up to 2
minutes, then deploys anyway so a stuck API never blocks releases.
