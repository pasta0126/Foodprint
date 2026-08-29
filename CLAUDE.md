# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Foodprint is a small personal food diary. Invited users log meals, browse history,
and see a weekly habit summary. Trilingual UI (ca/es/en). See `README.md`.

Stack: .NET 10, ASP.NET Core Blazor Web App (**static SSR** — forms post and
re-render; the only interactive component is the theme toggle), EF Core + SQLite
(dev and prod), no email (invite-link + password auth).

## Commands

```bash
dotnet build
dotnet test                                                # everything
dotnet test --filter FullyQualifiedName~SummaryServiceTests # one class
dotnet test --filter FullyQualifiedName~MealJourneyE2E      # Playwright journey
pwsh tests/Foodprint.Tests/bin/Debug/net10.0/playwright.ps1 install chromium  # once, for e2e
dotnet run --project src/Foodprint.Web                      # dev server
dotnet run --project src/Foodprint.Cli -- <verb>            # admin CLI
dotnet dotnet-ef migrations add <Name> --project src/Foodprint.Core --startup-project src/Foodprint.Cli
```

## Architecture

- **`Foodprint.Core`** holds everything with behaviour: `Domain/` entities,
  `Data/AppDbContext` (+ migrations, meal-group seed), and one service per area
  under `Auth/`, `Profiles/`, `Meals/`. Services take `AppDbContext` and are
  registered by `AddFoodprintCore`. The Web app and the CLI both depend on this.
- **`Foodprint.Web`** is thin: Razor pages/components in `Components/`, a custom
  cookie auth handler (`Auth/SessionAuthHandler`, scheme `Foodprint`), a
  `CurrentUser` scoped accessor, minimal-API endpoints for sign-out / language /
  entry-delete, and localization wiring (`Localization/`, resx in `Resources/`).
- **Auth model**: `RegistrationLink` (one-time, 30-day) → user sets password →
  `Session` (cookie `fp_session`, 30-day rolling). `AdminBootstrapper` ensures the
  `Foodprint:AdminEmail` account and prints its activation link at startup.
- **Time zones**: meal times are stored UTC (server clock). Every calendar-day
  bucket (day view, history, weekly window) goes through `Meals/DayRange` using the
  user's `Profile.TimeZoneId`. Don't bucket dates any other way.
- **Localization**: all user-facing strings come from `IStringLocalizer<SharedResource>`
  (`Resources/SharedResource*.resx`, neutral file = Spanish). `ResourceCompletenessTests`
  fails the build if `ca`/`en` drift from the neutral key set — add new keys to all three.

## Conventions

- Static SSR: form components use `[SupplyParameterFromForm]` + `EditForm`; handlers
  redirect with `NavigationManager`. Don't reach for `@rendermode InteractiveServer`
  unless a feature genuinely needs a live circuit.
- Every query in a service is scoped by `userId`; cross-user access returns
  not-found, and there are tests that assert it.
- `TreatWarningsAsErrors` is on for `src/`. Conventional commits.

## OpenSpec

Planned via OpenSpec. The MVP change is `openspec/changes/foodprint-mvp/`
(implemented). Future work: `/opsx:propose "<idea>"` → `/opsx:apply` → `/opsx:archive`.
