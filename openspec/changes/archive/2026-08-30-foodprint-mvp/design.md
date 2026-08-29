## Context

The repo is empty (README + LICENSE only). This change stands up the whole stack
and seven capabilities at once. See `proposal.md` — Why. Requirements live in
`specs/*/spec.md`. The stack is recorded in `openspec/config.yaml`: .NET 10,
ASP.NET Core Blazor Web App, EF Core + SQLite, xUnit + bUnit + Playwright. .NET 10
SDK (10.0.302) is installed. Deployment target is void-server: a Raspberry Pi
(linux-arm64), Debian 13, Docker present, an existing reverse proxy in front.

## Goals / Non-Goals

**Goals:**

- One deployable ASP.NET Core app covering token-link + password auth, per-user profiles,
  trilingual UI, meal logging, diary views, weekly summary, and a design system.
- Everything scoped by `UserId`; the app is multi-user from day one (a handful of
  invited people), not single-owner.
- Static server rendering by default; interactive render mode only where needed
  (forms, theme/language toggles, day navigation).

**Non-Goals:**

- Self-service signup, passwords, email, or external identity providers.
- Nutrition/calorie estimation or a food database.
- Photos on entries; a mobile/offline client.
- A separate API project or SPA frontend. Admin actions are a CLI + a minimal
  guarded endpoint, not a full admin UI.

## Decisions

### App shape: single Blazor Web App project

One ASP.NET Core Blazor Web App (`Foodprint.Web`). Global render mode is static
SSR; components opt into `@rendermode InteractiveServer` when they need
interactivity. Server interactivity is preferred over WASM: no client download,
direct DB access, simpler auth. Pages under `Components/Pages`, shared UI under
`Components/Shared`. Sign-in, activation, and sign-out are minimal API endpoints
(`MapGet`/`MapPost`) so they work under static SSR.

### Auth: email + password, token-link activation, cookie sessions (no Identity, no email)

- `User` entity gains `Email` (unique, lowercased), `PasswordHash?` (null until
  activated), `IsAdmin`, `DisabledAt?`.
- `RegistrationLink` entity: `Id`, `Email`, `TokenHash` (SHA-256 of a 32-byte
  random token, unique), `ExpiresAt` (default now + 30 days), `RevokedAt?`,
  `UsedAt?`, `CreatedByAdmin` (bool), `CreatedAt`. The link is `/activate/{token}`.
- Creating a link: admin via `Foodprint.Cli` (`invite create <email>`), or — when
  `Foodprint:AllowSelfRegistration` is true — a public `/register` form that
  displays the URL once (no email is sent). Rejected (neutral response) if that
  email already has a `PasswordHash`.
- Activation `/activate/{token}`: look up an active, unused link; the form
  collects display name + password (≥10 chars); on submit create the `User`
  (if new) with `PasswordHash` (Argon2id via `Konscious.Security.Cryptography`, or
  ASP.NET's PBKDF2 `PasswordHasher<T>`), create the default `Profile`, set
  `UsedAt`, and start a session.
- Password sign-in `/auth/sign-in`: look up by email, verify hash, check not
  disabled, create a `Session`. Generic failure message for wrong password /
  unknown email / disabled.
- `Session` entity: `Id`, `UserId`, `TokenHash` (unique), `ExpiresAt`,
  `LastSeenAt`. Cookie `fp_session` holds the raw token: `HttpOnly`, `Secure`,
  `SameSite=Lax`, 30-day rolling (renewed past a refresh threshold).
- `SessionAuthenticationHandler` (scheme `Foodprint`) resolves the cookie each
  request; rejects if the session is missing/expired or the user is disabled.
  Admin identity = `User.IsAdmin` (seeded from `Foodprint:AdminEmail`).
- Password change (`/profile/password`, current + new) replaces the hash and
  deletes the user's other sessions. Forgotten password: admin issues a fresh
  `RegistrationLink` for that email (works even though `PasswordHash` is set,
  because the admin created it) → user sets a new password.
- Admin startup: ensure a `User` row for `Foodprint:AdminEmail` with `IsAdmin`;
  if `PasswordHash` is null, create/print a `RegistrationLink` for it.
- Rate limiting (`Microsoft.AspNetCore.RateLimiting`, fixed window): 10 / IP /
  15 min across activation + sign-in; 5 / email / 15 min for password sign-in.
- Data Protection keys persisted to a mounted volume so cookies survive redeploys.
- Admin CLI/endpoint: `Foodprint.Cli` verbs (`invite create|list|revoke`,
  `user disable|enable`, `mealgroup add|retire`, `db migrate`). An `/admin` HTTP
  group is deferred — CLI-only for the MVP (see Open Questions).

Alternatives considered: ASP.NET Core Identity — its full surface (roles, external
logins, email confirmation, UI scaffolding) is more than a handful of users and a
no-email flow need; we reuse only its `PasswordHasher<T>` if convenient. Magic
links only (no password) — rejected: the user wants a normal password login after
setup. JWT sessions — rejected so disabling/expiry take effect immediately.

### User profile & time zone

- `Profile` entity (1:1 with `User`): `DisplayName`, `TimeZoneId` (IANA string),
  `Language` (`ca`/`es`/`en`). Defaults on creation: name from the activation form,
  `TimeZoneId = "Europe/Madrid"`, `Language` = the request-resolved language.
- `TimeZoneId` validated via `TimeZoneInfo.TryFindSystemTimeZoneById`. The base
  image must include `tzdata` (the `Debian`/`Ubuntu` .NET images do; verify in the
  Dockerfile).
- A `CurrentUser` scoped accessor exposes the profile to pages and services.

### Time-zone handling

Meal timestamps are `DateTime` UTC from `TimeProvider.System` (server clock). A
single helper `DayRange(DateOnly date, TimeZoneInfo tz) -> (DateTime StartUtc,
DateTime EndUtc)` is the only place day math lives; unit-tested against DST
boundaries. All day/history/summary queries filter `EatenAt >= StartUtc &&
EatenAt < EndUtc` using the acting user's `Profile.TimeZoneId`.

### Localization

- `RequestLocalizationOptions` with supported cultures `ca`, `es`, `en`; default
  `es`. Provider order: a custom provider reading `Profile.Language` for
  authenticated users, then `AcceptLanguageHeaderRequestCultureProvider`, then the
  default.
- Strings via `IStringLocalizer` backed by `.resx` per area
  (`Resources/*.ca.resx`, `.es.resx`, `.en.resx`); `es` is the neutral/base set.
- Dates/numbers via `CultureInfo` (`toLocalTime` + `ToString` with the current
  culture). Weekday and month names come from the culture.
- Language switch: an interactive control that POSTs to `/profile/language`,
  updates `Profile.Language`, and reloads.
- A small CI check (or unit test) asserts every key present in the `es` resource
  exists in `ca` and `en` — enforces the "no raw keys" requirement.

### Data model (EF Core entities)

```
User        Id, Email (unique, lower), PasswordHash?, IsAdmin, DisabledAt?, CreatedAt
Profile     UserId (PK/FK), DisplayName, TimeZoneId, Language
RegistrationLink  Id, Email, TokenHash (unique), ExpiresAt, RevokedAt?, UsedAt?,
            CreatedByAdmin, CreatedAt
Session     Id, UserId -> User, TokenHash (unique), ExpiresAt, LastSeenAt
MealGroup   Id, Key (unique, stable), SortOrder, RetiredAt?    -- catalog, seeded
MealEntry   Id, UserId -> User, Name, EatenAt (UTC),
            PortionSize (string?), PortionGrams (int?),
            MealGroupId? -> MealGroup, Notes (string?), CreatedAt, UpdatedAt
Tag         Id, UserId -> User, Name        -> unique (UserId, Name)
MealEntryTag  MealEntryId, TagId            -> composite key
```

- **Portion**: `PortionSize` XOR `PortionGrams` — a check constraint plus app
  validation. `PortionSize` constrained in code to `small|medium|large`;
  `PortionGrams` 1–5000.
- **MealGroup** is a closed catalog: rows come from a seed run in an EF migration
  (`breakfast`, `lunch`, `dinner`, `snack`, `other`). Display names are localized
  from resources keyed by `MealGroup.Key`, not stored in the row. Retiring a group
  sets `RetiredAt` (kept for old entries, hidden from the picker). Only the admin
  CLI can add/retire groups; the Blazor UI cannot.
- Tags normalized (trim + lowercase), de-duplicated per entry, upserted per user;
  the join table makes "top tags" a cheap `GroupBy`.
- All timestamps UTC.

### Data access

`AppDbContext` with `UseSqlite`; register both a scoped context and
`IDbContextFactory<AppDbContext>` (interactive Server components use the factory to
avoid circuit-lifetime concurrency issues). Query services (`MealEntryService`,
`DiaryService`, `SummaryService`, `ProfileService`, `InviteService`) wrap the
context (`AuthService`, `RegistrationService` too); pages inject services.
Migrations checked in; `Migrate()` at startup in
Development, `dotnet ef database update` (or a CLI `db migrate` verb) for prod.
SQLite in WAL mode; the `.db` file lives on a mounted volume.

### Design system

- Tokens as CSS custom properties on `:root` and `[data-theme="dark"]` in one
  stylesheet. No Tailwind — plain CSS keeps the toolchain to just the .NET SDK.
- Theme: a pre-paint inline script reads `prefers-color-scheme` + a `localStorage`
  override and sets `data-theme` on `<html>`; the toggle is an `InteractiveServer`
  component.
- Primitives as Razor components (`FpButton`, `FpInput`, `FpTextarea`, `FpSelect`,
  `FpChip`, `FpCard`, `AppShell`) with visible focus, ARIA, keyboard operation;
  bUnit tested. Delete confirmation uses the native `<dialog>`.

### Testing

- `Foodprint.Tests` (xUnit): portion XOR validation, grams range, meal-group
  validation, tag normalization, `DayRange` DST boundaries, streak, top-tag
  ranking, link activation (first/weak-password/expired/revoked/used), password
  sign-in (correct/wrong/unknown/disabled), rate limits, session expiry, password
  change invalidating other sessions, admin-only guard, language resolution order,
  resource-key completeness. EF against a fresh SQLite connection per test class.
- bUnit: primitive components (keyboard, labels, focus ring).
- Playwright: activate an account from a CLI-created link (set password), sign out,
  sign back in with email + password, set profile language, log an entry with
  grams + a meal group, edit it, delete it, see it in day view, history, and the
  weekly summary.

## Risks / Trade-offs

- **SQLite + multiple users** → still single-writer; fine for a handful of low-rate
  users. WAL + short transactions. Revisit if contention appears.
- **`DbContext` lifetime on Blazor Server circuits** → use `IDbContextFactory` in
  interactive components; never share a context across circuit awaits.
- **tzdata missing in a slim base image** → `TimeZoneInfo` lookups throw. Pin a
  base image that ships tzdata (or `apt-get install tzdata`) and cover it in a
  smoke test.
- **Registration link leakage before activation** → whoever opens it first sets the
  password. Mitigate: 30-day expiry, single-use, immediate revocation, token hashed
  at rest and never logged. After activation the link is spent.
- **Self-registration abuse** (`AllowSelfRegistration=true`) → anyone can mint a
  link for any email. Mitigate: rate limit, admin can disable accounts and turn
  self-registration off; default it off unless the user wants it on.
- **No email = no self-service password reset** → admin must issue reset links. Fine
  for a handful of users; documented.
- **Data Protection key loss** → all sessions drop; users sign in again with their
  password (no reset needed). Persist keys to the mounted volume.
- **Translation drift** → the resource-key completeness test fails the build if
  `ca`/`en` miss a key.
- **arm64 build** → publish `-r linux-arm64` or build the image on the Pi / via
  buildx; CI must target arm64 or the Pi pulls a multi-arch image.

## Migration Plan

Greenfield — no data migration. Build a `linux-arm64` image (multi-stage
Dockerfile, base image with tzdata). Deploy on void-server via Docker at
`foodprint.northernarchive.com` (same pattern as the other void-server
containers), behind the existing reverse proxy; set
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (or configure `ForwardedHeaders` for the
proxy's network), mount volumes for the SQLite DB and the Data Protection keys, and
set `ConnectionStrings:Default`, `Foodprint:AdminEmail=pasta0126@gmail.com`,
`Foodprint:PublicBaseUrl=https://foodprint.northernarchive.com`,
`Foodprint:AllowSelfRegistration`, and culture/time-zone config. Run migrations on
release (startup `Migrate()` or `db migrate` verb), which also seeds the
`MealGroup` catalog. On first start, retrieve the admin activation link from the
logs / `db admin-link` verb and set the admin password. Rollback = redeploy the
previous image; schema changes here are additive.

## Open Questions

- Exact reverse-proxy mechanics on void-server (which proxy, how a container is
  attached to it, TLS termination) — needed for the `ForwardedHeaders` known-network
  config, not for the specs or task breakdown. Hostname is settled:
  `foodprint.northernarchive.com`.
- Whether to expose `AllowSelfRegistration` publicly from launch or keep it
  admin-issued-only until the invited group is set up — a config flip, no code or
  spec change.
