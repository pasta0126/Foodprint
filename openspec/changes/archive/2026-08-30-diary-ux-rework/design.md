## Context

See `proposal.md` — Why. Current state that shapes the approach:

- The header (`AppShell.razor`) hard-codes four nav links plus a tools cluster:
  `LanguageSwitcher`, `ThemeToggle` (`@rendermode InteractiveServer`, backed by a
  `fpTheme` JS helper writing `localStorage` + a root attribute), and a sign-out
  `<form method="post">`.
- Portion is `PortionSize` (string, from `PortionSizes.All`) XOR `PortionGrams`
  (int 1..5000), validated in `MealEntryRules.Validate`. The form
  (`MealEntryForm.razor`) uses a `PortionMode` radio group (`None`/`Size`/`Grams`)
  with both the size `<select>` and the grams `<input>` always visible.
- Meal groups come from a seeded closed catalog (`MealGroupKeys.Seed`), surfaced
  as `MealGroupOption` via `MealGroupService.ActiveAsync()`.
- Static SSR is the rule; `ThemeToggle` is the one interactive component.
- All user-facing strings are `IStringLocalizer<SharedResource>` keys, present in
  es (neutral) + ca + en, enforced by `ResourceCompletenessTests`.

## Goals / Non-Goals

**Goals:**

- One header affordance for identity → menu → Profile / Sign out.
- Profile page as the account hub (name, tz, language, theme, sign-out).
- Portion capture that leads with a plate-referenced named size and keeps grams
  as a secondary option, with a fourth size `very-large`.
- Time-of-day meal-group suggestion on create only.
- A reusable inline-SVG `Icon` component and consistent iconography.
- Keep everything working under static SSR; keep the responsive floor at 320px.

**Non-Goals:**

- No change to how meal timestamps are stored (still server-clock UTC).
- No schema migration for portion (no DB check constraint added; `very-large` is
  just a new allowed string).
- No renaming of existing portion values; no data backfill.
- No new client framework or icon-library dependency.
- No redesign of history / weekly-summary beyond picking up shared icons and the
  responsive pass.

## Decisions

### Identity menu without a live circuit

Use a native `<details><summary>` disclosure in the header for the identity
control: `<summary>` renders the avatar + name, the open panel holds `<a
href="/profile">` and the existing sign-out `<form method="post"
action="/auth/sign-out">`. Pure HTML/CSS, keyboard-operable, no `@rendermode`.

- Alternative — interactive dropdown component: rejected, pulls a circuit into the
  header for a trivial menu and contradicts the static-SSR convention.
- Alternative — navigate straight to `/profile` on avatar click with no menu:
  loses the quick sign-out the user asked for.
- Accessibility note: `<details>` toggling is well supported; add
  `aria-label` on `<summary>`, close-on-outside-click is a nice-to-have via a
  tiny inline script but not required for correctness.

### Deterministic avatar color

A pure helper `AvatarColor(string key)` (in `Foodprint.Web`, unit-tested):
hash the stable key (user id `Guid` string, lower-cased) → pick from a fixed
palette of ~12 AA-contrast-against-white background colors defined as design
tokens. Initial = first rune of `DisplayName` (trimmed), else first rune of
email, upper-cased. Rendered as a styled `<span>` — no image request.

- Alternative — HSL from hash: harder to guarantee AA contrast for the overlaid
  initial; a curated token palette is safer and on-brand.

### Theme preference on the profile page

Keep the `fpTheme` JS helper and its `system/light/dark` model. Replace the
header `ThemeToggle` with a theme control on the profile page. Simplest path that
preserves static SSR: render three radio-style options as a small interactive
island (`@rendermode InteractiveServer`) reusing the `fpTheme.set` call, OR keep
it script-only — three `<button>`/`<label>` elements wired by a 3-line inline
script calling `fpTheme.set`. Choose the **script-wired** version so the profile
page stays fully static SSR and there is zero interactive component in the app.
The `fpTheme` bootstrap script (runs before paint) is unchanged.

- Alternative — persist theme in `Profile` and render server-side: makes theme a
  cross-device account setting, but it is currently per-browser by spec
  (`design-system` — "persists per browser") and would need a migration + a
  no-FOUC story on first paint. Out of scope; keep per-browser.

### Portion UX: progressive disclosure, still XOR

Model stays `PortionSize` XOR `PortionGrams`. Form becomes:

- A segmented control / radio row of the four sizes (each with a small plate
  glyph and the localized description as helper text), plus a "no portion"
  choice.
- A collapsed "Enter exact grams instead" disclosure (`<details>`); opening it
  and typing a value clears the selected size on submit.
- `PortionMode` is derived server-side from which field is non-empty (grams wins
  only if the grams disclosure was used and has a value), so the existing
  `PortionBothProvided` guard still applies as a safety net.

Add `PortionSizes.VeryLarge = "very-large"` to `All`; update
`MealEntryRules.Validate` (already generic via `PortionSizes.IsValid`), any
exhaustive `switch` on size (portion display in `MealEntryCard`, summary if any),
and the `MealEntryFormModel.ToInput` mapping.

### Time-of-day meal-group suggestion

New pure helper `MealGroupSuggestion.ForLocalTime(TimeOnly localTime,
IReadOnlyList<MealGroupOption> active) : int?` returning the matching active
group id or null. Fixed bands (local time): 05:00–10:59 breakfast, 11:00–15:59
lunch, 19:00–23:59 dinner, else snack; fall back through
`snack → null` when a band's key is not an active catalog member. Called only
by `NewEntry.razor` when building the initial form model, using
`CurrentUser` profile tz to convert "now" to local time. `EditEntry.razor` never
calls it.

- Alternative — configurable bands per user: over-engineered for a personal app;
  revisit only if asked.

### Icon component

`Icon.razor` (`Foodprint.Web/Components/Shared`): `[Parameter] Name`,
`[Parameter] string? Title`. Renders an inline `<svg>` from a static
`Dictionary<string, string>` of path data (16/20/24 viewBox, `currentColor`
fill/stroke). `aria-hidden="true"` when `Title` is null, else `role="img"` +
`<title>`. Icon set: nav (today, history, summary), actions (add, edit, delete,
save, back), meal groups (breakfast, lunch, dinner, snack, other), portion sizes
(four plate-fill glyphs). Source: hand-picked from an MIT/CC0 set (e.g. Lucide
path data), copied in — no package reference.

### Responsive pass

CSS-only, in the existing stylesheet / design tokens. Header switches to a
two-row / wrap layout below 768px; the `<details>` nav already exists
(`fp-nav-toggle`) — keep it, restyle. Forms: single-column grid, 44px min tap
targets, portion segmented control wraps. No markup framework change.

## Risks / Trade-offs

- **`very-large` is a new enum-like value across code + resx + tests** → grep for
  every use of `PortionSizes`/`"large"` string literals and the resx key pattern
  `Meal.Size.*`; add `Meal.Size.very-large` and description keys to all three
  resx files or `ResourceCompletenessTests` fails the build.
- **`<details>`-based menu has no outside-click close by default** → acceptable;
  optionally add a tiny inline script. Not a blocker; keyboard + click-again
  close work.
- **Removing the header sign-out changes muscle memory / existing e2e** → update
  `MealJourneyE2E` and any journey selecting the old navbar profile link or
  sign-out button; the sign-out endpoint itself is unchanged.
- **Script-wired theme control** → if JS is disabled the profile theme control is
  inert (same as today's `ThemeToggle`, which is interactive-only). Acceptable;
  the pre-paint bootstrap still honors a previously stored choice.
- **Suggestion bands are opinionated** → they are only a default and fully
  overridable; low risk.

## Migration Plan

1. Ship code; no DB migration required. `very-large` entries can only be created
   after deploy, so no backfill.
2. Rollback: revert the deploy. Any `very-large` rows written in the meantime
   would fail `PortionSizes.IsValid` on edit under the old code — acceptable given
   the tiny user base and short exposure; note in the PR.
3. No config or infra changes.

## Open Questions

- Exact plate-fraction wording per language — refine with the user during resx
  authoring; does not affect specs or task breakdown.
- Final icon source/style — Lucide assumed; swap is mechanical.
