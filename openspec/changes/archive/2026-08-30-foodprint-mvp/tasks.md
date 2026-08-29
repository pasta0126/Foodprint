## 1. Solution scaffold

- [x] 1.1 Create `Foodprint.sln` with `src/Foodprint.Web` (Blazor Web App, .NET 10), `src/Foodprint.Cli`, `tests/Foodprint.Tests` (xUnit)
- [x] 1.2 Set global render mode to static SSR; add `.editorconfig`, `Directory.Build.props`, nullable + warnings-as-errors
- [x] 1.3 Add configuration keys: `ConnectionStrings:Default`, `Foodprint:AdminEmail`, `Foodprint:PublicBaseUrl`, `Foodprint:AllowSelfRegistration`, `ForwardedHeaders`, culture defaults; add `appsettings.Development.json`
- [x] 1.4 Add EF Core (`Sqlite`, `Design`) and configure Data Protection key persistence to a configurable path
- [x] 1.5 Add Playwright (`Microsoft.Playwright`) and bUnit to the test project
- [x] 1.6 Multi-stage `Dockerfile` targeting `linux-arm64`, base image with tzdata; `.dockerignore`
- [x] 1.7 CI workflow: `dotnet restore/build/test`, migrations check, resource-key completeness check, Playwright, arm64 image build
- [x] 1.8 Document `dotnet run` / `dotnet test` / `dotnet ef` / CLI commands in `README.md`

## 2. Data model

- [x] 2.1 Define entities: `User` (Email, PasswordHash?, IsAdmin, DisabledAt?), `Profile`, `RegistrationLink`, `Session`, `MealGroup`, `MealEntry`, `Tag`, `MealEntryTag`
- [x] 2.2 Write `AppDbContext` configs: unique `User.Email` / `RegistrationLink.TokenHash` / `Session.TokenHash` / `MealGroup.Key` / `Tag(UserId,Name)`, `MealEntryTag` composite key, portion `PortionSize` XOR `PortionGrams` check constraint
- [x] 2.3 Register `AppDbContext` + `IDbContextFactory<AppDbContext>` with SQLite; enable WAL
- [x] 2.4 Initial migration; `Migrate()` at startup in Development
- [x] 2.5 Seed the `MealGroup` catalog (`breakfast`, `lunch`, `dinner`, `snack`, `other`) in a migration/seeder

## 3. Design system

- [x] 3.1 Color/type/spacing/radius tokens as CSS custom properties for light and `[data-theme="dark"]`
- [x] 3.2 Pre-paint inline theme script (system preference + `localStorage` override → `data-theme` on `<html>`)
- [x] 3.3 `ThemeProvider` + interactive theme toggle
- [x] 3.4 `FpButton`, `FpInput`, `FpTextarea`, `FpSelect`, `FpChip`, `FpCard` with focus states and ARIA
- [x] 3.5 `AppShell` layout (header nav, language + theme toggles, sign-out), responsive to 320px with nav collapse < 768px
- [x] 3.6 bUnit tests: keyboard activation, label association, visible focus ring
- [x] 3.7 Verify WCAG AA contrast for all token pairings in both themes

## 4. Localization

- [x] 4.1 Add `RequestLocalizationOptions` for `ca`/`es`/`en`, default `es`
- [x] 4.2 Custom `RequestCultureProvider` that reads the authenticated user's `Profile.Language` first
- [x] 4.3 Create `.resx` resource sets (es base, ca, en) and wire `IStringLocalizer`
- [x] 4.4 Route all UI strings (including validation messages and meal-group display names by `Key`) through localization
- [x] 4.5 Locale-aware date/number/weekday formatting helpers
- [x] 4.6 `/profile/language` POST endpoint + language switcher in `AppShell`
- [x] 4.7 Test: language resolution order (profile > Accept-Language > es); resource-key completeness across ca/es/en

## 5. Auth (token-link activation + password)

- [x] 5.1 Password hashing helper (Argon2id via `Konscious.Security.Cryptography`, or `PasswordHasher<T>`); `RegistrationService`: create link (random token, store hash, 30-day expiry, return URL once), list, revoke
- [x] 5.2 `Foodprint.Cli` verbs: `invite create <email> [--expires <date>]`, `invite list`, `invite revoke <id>`, `user disable|enable <email>`, `mealgroup add|retire <key>`, `db migrate`, `db admin-link`
- [x] 5.3 Admin bootstrap on startup: ensure `User` for `Foodprint:AdminEmail` with `IsAdmin`; if no `PasswordHash`, create + log a `RegistrationLink`
- [x] 5.4 `GET/POST /activate/{token}`: validate active unused link; form collects display name + password (≥10); create `User`(if new) + `PasswordHash` + default `Profile`; set `UsedAt`; create `Session` + `fp_session` cookie (HttpOnly, Secure, SameSite=Lax, 30-day rolling); redirect home
- [x] 5.5 Neutral "link no longer valid" page for expired/revoked/used/unknown tokens
- [x] 5.6 `/register` form gated by `Foodprint:AllowSelfRegistration` — creates a link and shows the URL once; neutral response if email already has a password
- [x] 5.7 `POST /auth/sign-in`: verify email + password hash, reject if disabled, generic failure message; create `Session`
- [x] 5.8 `SessionAuthenticationHandler` (scheme `Foodprint`): resolve cookie each request, reject if session missing/expired or user disabled; rolling renewal; expose `IsAdmin`
- [x] 5.9 Fallback `[Authorize]` policy + `AdminOnly` policy; unauthenticated pages → sign-in, JSON → 401
- [x] 5.10 `POST /profile/password` (current + new ≥10) → replace hash, invalidate other sessions; `POST /auth/sign-out`
- [x] 5.11 Rate limiting: 10 / IP / 15 min across activation + sign-in; 5 / email / 15 min for password sign-in
- [x] 5.12 xUnit tests: activation (first/weak-password/expired/revoked/used), sign-in (correct/wrong/unknown/disabled), rate limits, cookie attributes, session expiry, password change invalidates other sessions, admin-only guard

## 6. User profile

- [x] 6.1 `ProfileService`: get/update display name (1–80), `TimeZoneId` (validate via `TimeZoneInfo`), `Language` (ca/es/en)
- [x] 6.2 Profile page + form (`InteractiveServer`), reachable from `AppShell`
- [x] 6.3 `CurrentUser` scoped accessor exposing the profile to pages/services
- [x] 6.4 xUnit tests: defaults on creation, invalid time zone rejected, language change takes effect

## 7. Meal logging

- [x] 7.1 Entry validation: name 1–120, `EatenAt` not >24h future (past unrestricted), notes ≤1000
- [x] 7.2 Portion validation: `PortionSize` in set XOR `PortionGrams` 1–5000; reject both/neither-invalid
- [x] 7.3 Meal-group validation: optional, must be an active `MealGroup` id
- [x] 7.4 Tag normalization (trim+lowercase, dedupe, ≤10, 1–30 chars) with per-user `Tag` upsert
- [x] 7.5 `MealEntryService.Create` + "add entry" form (defaults `EatenAt` to server now; portion mode toggle; group selector lists active groups only)
- [x] 7.6 `MealEntryService.Update` + edit form; sets `UpdatedAt`
- [x] 7.7 `MealEntryService.Delete` + `<dialog>` confirmation; permanent
- [x] 7.8 Scope every query by `UserId`; not-found for unowned/missing ids
- [x] 7.9 xUnit tests: portion XOR, grams range, meal-group check, tag normalization, ownership isolation

## 8. Diary views

- [x] 8.1 `DayRange(DateOnly, TimeZoneInfo)` helper; unit-test DST boundaries
- [x] 8.2 Day view page: entries for a calendar day in the profile time zone, time-ascending, prev/next nav, defaults to today
- [x] 8.3 Day view empty state with add-entry prompt
- [x] 8.4 History view page: reverse-chron list of days-with-entries, count + name preview, 20/page pagination
- [x] 8.5 History → day navigation and history empty state
- [x] 8.6 xUnit tests: day grouping across zones, history pagination

## 9. Weekly summary

- [x] 9.1 `SummaryService`: load the 7-day window (ending today, profile time zone) once
- [x] 9.2 Entries-per-day including zero days
- [x] 9.3 Top-5 tags (count desc, name asc) with deterministic tie-break
- [x] 9.4 Current streak (consecutive days ending today with ≥1 entry)
- [x] 9.5 Summary dashboard page with empty states
- [x] 9.6 xUnit tests: per-day counts, tag ranking, streak (active + broken)

## 10. End-to-end & deploy

- [x] 10.1 Playwright: activate a CLI-created link (set password), set language, log an entry (grams + group), edit, delete
- [x] 10.2 Playwright: sign out, sign back in with email + password; entry appears in day view, history, and weekly summary
- [x] 10.3 Wire navigation between all pages in `AppShell`; localized page titles
- [x] 10.4 Finalize `README.md`: setup, configuration, CLI, dev/test commands
- [x] 10.5 Compose/run config for void-server (`foodprint.northernarchive.com`): volumes (DB + DP keys), forwarded headers, `PublicBaseUrl`, `AdminEmail`, image tag; document proxy attachment, admin activation-link retrieval, and backup (file copy + volume snapshot)
