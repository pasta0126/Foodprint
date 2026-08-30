# Foodprint

A small personal food diary. Invited users log what they ate, review their
history, and see a light weekly habit summary. Trilingual UI (Catalan, Spanish,
English).

## Stack

- .NET 10, ASP.NET Core **Blazor Web App** — static server rendering, no client
  framework
- **EF Core + SQLite** (development and production)
- Auth: no email. The backend issues a one-time **activation link**; the user opens
  it to set a display name and password, then signs in normally. One configured
  account is the administrator.
- Tests: xUnit, bUnit, Playwright

## Layout

| Project | What |
| --- | --- |
| `src/Foodprint.Core` | Domain, `AppDbContext`, migrations, all services |
| `src/Foodprint.Web` | Blazor Web App |
| `src/Foodprint.Cli` | Admin CLI (invites, users, meal groups, migrations) |
| `tests/Foodprint.Tests` | Unit, component and end-to-end tests |

## Develop

```bash
dotnet build
dotnet test                                   # all tests
dotnet test --filter FullyQualifiedName~AuthServiceTests   # one class
dotnet test --filter "FullyQualifiedName~MealJourneyE2E"   # the Playwright journey

# first e2e run needs a browser:
pwsh tests/Foodprint.Tests/bin/Debug/net10.0/playwright.ps1 install chromium

dotnet run --project src/Foodprint.Web        # http://localhost:5170 (see launchSettings)
```

On first launch the app migrates the database, seeds the meal-group catalog, and
**prints an admin activation link** in the logs (`Admin activation link: …`). Open
it once to set the admin password.

`appsettings.Development.json` turns on self-registration and points at
`foodprint.dev.db`.

## Admin CLI

Run against the same database as the web app (set `ConnectionStrings:Default` or
`FOODPRINT_ConnectionStrings__Default`).

```bash
dotnet run --project src/Foodprint.Cli -- invite create alex@example.com [--expires=2026-12-31]
dotnet run --project src/Foodprint.Cli -- invite list
dotnet run --project src/Foodprint.Cli -- invite revoke <id>
dotnet run --project src/Foodprint.Cli -- user disable alex@example.com
dotnet run --project src/Foodprint.Cli -- user enable alex@example.com
dotnet run --project src/Foodprint.Cli -- mealgroup add brunch
dotnet run --project src/Foodprint.Cli -- mealgroup retire brunch
dotnet run --project src/Foodprint.Cli -- db migrate
dotnet run --project src/Foodprint.Cli -- db admin-link
```

A forgotten password is recovered by the admin issuing a fresh
`invite create <that-email>` link.

## Configuration (`Foodprint` section / `Foodprint__*` env vars)

| Key | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:Default` | `Data Source=foodprint.db` | SQLite file |
| `Foodprint:AdminEmail` | `pasta0126@gmail.com` | The administrator account |
| `Foodprint:PublicBaseUrl` | `http://localhost:5000` | Used to build activation links |
| `Foodprint:AllowSelfRegistration` | `false` | Public `/register` form on/off |
| `Foodprint:DefaultTimeZone` | `Europe/Madrid` | Default for new profiles |
| `Foodprint:DataProtectionKeyPath` | `dp-keys` | Persist this — losing it signs everyone out |
| `Foodprint:RegistrationLinkExpiryDays` | `30` | Activation-link lifetime |
| `ForwardedHeaders:Enabled` | `false` | Set `true` behind a reverse proxy |

## Deploy (void-server)

void-server is a Raspberry Pi (`linux/arm64`, Debian 13) running Docker with a
reverse proxy already in front. Foodprint is served at
`foodprint.northernarchive.com`, like the other containers there.

**CI/CD.** GitHub Actions runs CI only (free, public repo). void-server deploys
itself: `deploy/deploy.sh` runs from cron and redeploys when `main` moves and its
CI is green. Details and one-time setup in [`deploy/README.md`](deploy/README.md).

Manual deploy:

```bash
ssh void-server 'cd ~/foodprint && git pull && docker compose up -d --build'
```

Or from scratch on the Pi:

```bash
docker compose up -d --build   # builds the linux/arm64 image and runs it
```

- Attach the container to the reverse proxy's network and point the proxy at
  port 8080. The app trusts `X-Forwarded-*` when `ForwardedHeaders__Enabled=true`.
- Volumes: `/data` (SQLite DB) and `/keys` (Data Protection keys) — both must
  persist across redeploys.
- Migrations run automatically at startup.
- After the first deploy, get the admin activation link from
  `docker compose logs foodprint | grep 'activation link'`.

### Backups

No automated backup in the MVP. To back up, copy the database file while the app
is stopped (or use `sqlite3 /data/foodprint.db ".backup"`), plus a snapshot of the
`/keys` volume. Restore by putting both back and restarting.

## License

Unlicense (public domain) — see `LICENSE`.
