## Why

Foodprint has no application code yet — only a README and license. To turn it into
a usable food diary we need a first end-to-end slice: an invited person can sign
in, log what they ate, review their history, and see a simple weekly summary. This
change establishes the project scaffold and the MVP feature set so later work has
a foundation to build on.

## What Changes

- Scaffold a .NET 10 ASP.NET Core Blazor Web App with Entity Framework Core
  (SQLite), xUnit, bUnit and Playwright.
- Add email + password auth with no email sending: the administrator
  (`pasta0126@gmail.com`) or an optional self-register form produces a one-time
  token link (30-day expiry, revocable); opening it lets the person set a display
  name and password; afterwards they sign in normally with email + password.
  Admin can issue reset links, disable accounts, and manage the meal-group catalog.
- Add per-user profiles: display name, IANA time zone, and UI language, editable by
  the user; used for day/week bucketing and locale.
- Add UI localization in Catalan, Spanish and English from the MVP, with the
  language chosen from the profile (falling back to the request and then Spanish).
- Add meal logging: create a meal entry with name, date/time (server clock),
  portion as a named size **or** a quantity in grams, an optional meal group from
  a backend-managed catalog, optional notes and free-form tags.
- Add entry management: view a single day's entries, edit an entry, delete an entry.
- Add history view: browse past days with entries, paginated, newest first.
- Add a weekly summary dashboard: entries per day, most-used tags, current logging
  streak for the last 7 days, in the user's time zone.
- Add a design system: color tokens, typography scale, and shared UI primitives
  (button, input, card, layout shell) used across all pages.

## Capabilities

### New Capabilities

- `auth`: token-link account activation, email + password sign-in, app-managed
  cookie sessions, password change/reset, an admin account, account disabling.
- `user-profile`: per-user display name, time zone, and UI language, with defaults
  and validation.
- `localization`: Catalan/Spanish/English UI strings and locale-aware date and
  number formatting, with language resolution and switching.
- `meal-logging`: create, read, update, and delete meal diary entries with their
  fields (including portion-by-size-or-grams and meal group) and validation rules.
- `diary-views`: day view and paginated history view over meal entries.
- `weekly-summary`: aggregate view of the last 7 days — counts, top tags, streak.
- `design-system`: visual language and shared UI primitives for the web app.

### Modified Capabilities

<!-- None — this is the first change. -->

## Impact

- New codebase: a .NET solution — `Foodprint.sln`, a Blazor Web App project, an
  `AppDbContext` with EF Core migrations, and xUnit/bUnit/Playwright test projects.
- New dependencies: `Microsoft.EntityFrameworkCore.Sqlite`, EF Core tools,
  `xunit`, `bunit`, `Microsoft.Playwright`; localization via resx +
  `IStringLocalizer`.
- New runtime requirements: a SQLite database file (persistent volume) and a
  persistent Data Protection key path; a catalog seed for meal groups; no SMTP.
- Deployment: a `linux-arm64` Docker image on void-server (Raspberry Pi, Debian
  13) at `foodprint.northernarchive.com`, behind the existing reverse proxy (same
  pattern as the other void-server containers) — the app honors forwarded headers
  and reads the connection string, Data Protection key path, `Foodprint:AdminEmail`,
  the public base URL, and time-zone/culture configuration from the environment.
