## Why

Logging a meal still takes too many taps: the home is a single day you have to
navigate, "Today" and "History" duplicate each other, the weekly summary is a
page nobody visits, and every entry is typed from scratch even though the same
meals repeat constantly. Folding everything into one home — quick-add on top,
habit summary below — and letting repeated meals become one-tap cards makes the
common path fast.

## What Changes

- **Navigation merged to two destinations** — **BREAKING** (routes removed):
  - `/` (home) becomes the diary home: a quick-add block (favorite cards +
    the meal-entry form) on top, the seven-day summary below.
  - `/history` shows entries **grouped by day, inline** — each day a section with
    its date and its entry cards (reverse-chronological by day, ascending by time
    within a day), paginated ~20 days per page.
  - Removed: `/summary` (folded into home), `/day/{date}` and the standalone
    day view with prev/next navigation (folded into `/history`), `/entries/new`
    (the home form is the only create surface; it already has a date/time field
    for past entries). Editing stays at `/entries/{id}/edit`; delete unchanged.
  - Navbar: **Home · History** + the identity control. The weekly nav item is
    gone.
- **Meal favorites** (new capability): a "Save to favorites" checkbox on the
  entry form. Ticking it on save creates — or updates, when one already exists
  with the same normalised name and meal group — a favorite holding the name,
  portion, meal group and tags (not the time, not the notes).
- **Quick-add cards**: favorites render on the home as cards **grouped by meal
  group** (breakfast / lunch / dinner / snack / other), each group with its
  localized label and icon. Tapping a card pre-fills the home entry form (name,
  portion, meal group, tags; eaten-at = now) for the user to review and submit.
  Each card has an inline control to delete that favorite.
- **Component extraction** (no behavior change): the day section (date header +
  entry-card list) and the seven-day summary view (streak, per-day bars, top
  tags) become reusable components so the home and history can compose them.

## Capabilities

### New Capabilities

- `meal-favorites`: per-user saved meal templates, created from the entry form,
  surfaced as quick-add cards grouped by meal group, and used to pre-fill a new
  entry.

### Modified Capabilities

- `diary-views`: the "Day view" requirement is replaced by a "Home view"
  (quick-add block + seven-day summary, no day navigation); the "History view"
  requirement changes to render entries grouped by day inline instead of a
  summary list that links to a separate day page.
- `meal-logging`: the "Create a meal entry" requirement gains the optional
  "save as favorite" action and the ability to start from a favorite's values
  (see `meal-favorites`).

## Impact

- **Domain / data**: new `MealFavorite` entity (`Id`, `UserId`, `Name`,
  `PortionSize?`, `PortionGrams?`, `MealGroupId?`, tags) + join for tags or a
  serialized tag list — decided in design; EF Core migration adds the table(s).
  No change to existing tables or rows.
- **Core**: new `MealFavoriteService` in `Foodprint.Core/Meals/` (list grouped,
  save-or-update with dedup, delete), all queries scoped by `userId`. Extend the
  meal-entry create path to optionally persist a favorite.
- **Web**: `Day.razor` and `Summary.razor` deleted; `NewEntry.razor` deleted.
  New `Home.razor` at `/` composing `QuickAddCards`, `MealEntryForm`,
  `WeeklySummaryView`. `History.razor` reworked to use a new `DiaryDaySection`
  component. `MealEntryForm` gains the favorite checkbox and accepts prefill
  values. New minimal-API endpoints for favorite delete (and prefill if a POST
  mechanism is chosen). Nav in `AppShell.razor` trimmed to two items. Old routes
  `/summary`, `/day/*`, `/entries/new` → redirect to `/`.
- **Localization**: new resx keys (favorites, "save to favorites", card group
  headers, empty states, nav relabel) in all three of `SharedResource.resx`
  (es), `.ca`, `.en` — enforced by `ResourceCompletenessTests`.
- **Tests**: unit tests for `MealFavoriteService` (dedup, isolation, grouping);
  bUnit for `QuickAddCards` and `DiaryDaySection`; `MealJourneyE2E` updated
  (no `/summary`, `/day`, `/entries/new`; create from the home; save a favorite
  and reuse its card).
