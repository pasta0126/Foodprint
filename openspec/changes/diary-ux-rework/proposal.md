## Why

Logging a meal is meant to be the fast, low-friction core of Foodprint, but the
current UI slows it down: the portion picker shows three modes and two inputs at
once, "small/medium/large" has no shared reference so people hesitate, the meal
group must be picked from scratch every time, and the chrome (language, theme,
sign-out, profile) is scattered across a navbar that does not adapt well to small
screens and gives little visual guidance. This change reworks the diary UX so the
common path is obvious and quick on a phone.

## What Changes

- **Navigation identity control**: remove the "Profile" nav link. Add a
  Google-style identity control on the right of the header — a circular avatar
  with a deterministic background color derived from the user and the initial of
  their display name (or email), shown next to their name/email. Activating it
  opens a small menu with "Profile" and "Sign out" (static-SSR friendly, no live
  circuit).
- **Profile page becomes the account hub**: language selector (already there),
  theme preference, and sign-out all live on `/profile`. The header no longer
  renders the language switcher, the theme toggle, or a sign-out form.
- **Portion input simplified**: plate-referenced named size is the primary,
  default way to record a portion; grams becomes a secondary ("more precise")
  option that is collapsed by default. The form makes clear it is one or the
  other, never both.
- **Portion size semantics + a fourth level** — **BREAKING** (named-size set
  changes): sizes are redefined against a standard flat plate as the reference,
  and a fourth size is added:
  - `small` — about a third of a plate (or a small bowl)
  - `medium` — between a third and two thirds of a plate
  - `large` — a full plate
  - `very-large` — a heaped/large plate
  Localized descriptions (ca/es/en) and a small per-size plate glyph make
  eyeballing the amount fast.
- **Meal group suggested by time**: when creating an entry, the meal group
  defaults to a suggestion based on the eaten-at time of day (breakfast / lunch /
  dinner / snack). The user can override it, and editing an existing entry does
  not re-suggest.
- **Icons throughout**: a small inline-SVG icon set (no JS dependency) applied to
  nav items, entry actions (add / edit / delete / save), meal groups, and portion
  sizes so each control is identifiable at a glance.
- **Responsive pass**: header, forms, and cards are tightened to work well from
  320px up, with the identity control and navigation collapsing cleanly on
  mobile.

## Capabilities

### New Capabilities

_None. All changes modify existing capabilities._

### Modified Capabilities

- `meal-logging`: the named portion set gains `very-large` and the sizes are
  defined against a plate reference; the meal group gets a time-of-day default
  suggestion on creation.
- `design-system`: the app layout-shell requirement changes — primary nav drops
  the profile link and gains a deterministic-color avatar identity control with a
  menu; theme toggle and sign-out move out of the header. A new UI primitive
  covers the icon set. The responsive-layout requirement is tightened for the new
  header.
- `user-profile`: the profile page is specified as the place a user changes their
  theme preference and signs out, in addition to name / time zone / language.

## Impact

- **Domain / data**: `PortionSizes` gains `very-large`; validation, `MealEntryInput`,
  and any exhaustive switches update. Existing rows are unaffected (no rename of
  current values). No schema change unless a migration is wanted for a check
  constraint — none required.
- **Core services**: `MealEntryService` / form model gain time-of-day → meal-group
  suggestion logic (new pure helper, unit-tested).
- **Web**: `AppShell.razor` reworked (identity control + menu, drop tools);
  `ProfilePage.razor` gains theme + sign-out; new `Icon` component and
  `IdentityBadge` / avatar-color helper; `MealEntryForm.razor` portion UX;
  `MealEntryCard.razor` and nav pick up icons; CSS/design-token updates for
  responsiveness.
- **Localization**: new/changed resx keys for portion-size descriptions, the
  identity menu, theme labels on the profile page, and icon aria-labels — added to
  all three of `SharedResource.resx` (es), `.ca`, `.en` (enforced by
  `ResourceCompletenessTests`).
- **Tests**: `MealEntryRules` / suggestion unit tests; bUnit tests for the new
  header and portion form; update any e2e/journey assertions that select the old
  navbar profile link or sign-out button, or that assume the `large` set.
