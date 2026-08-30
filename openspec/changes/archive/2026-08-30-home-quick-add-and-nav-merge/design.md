## Context

See `proposal.md` — Why. Constraints that shape the approach:

- Static SSR everywhere except the profile theme control script. Forms are
  `EditForm` + `[SupplyParameterFromForm]`; handlers redirect (PRG) via
  `NavigationManager`. No interactive circuit is wanted here.
- `MealEntryForm.razor` is already the shared create/edit form (portion required,
  time-of-day meal-group suggestion). `MealEntryFormModel` maps to/from
  `MealEntryInput` / `MealEntryView`.
- `Day.razor` currently owns both `/` and `/day/{date}`; `History.razor` owns
  `/history` and renders `HistoryDay` summaries linking to `/day`; `Summary.razor`
  owns `/summary`. `DiaryService` has `GetDayAsync` and `GetHistoryAsync`
  (summary rows); `SummaryService.GetAsync` returns `WeeklySummary`.
- Tags use `Tag` (per-user, normalised) + `MealEntryTag` join, attached by
  `MealEntryService.AttachTagsAsync`.
- Routing/auth: `_Imports.razor` has a fallback `[Authorize]`; minimal-API
  endpoints (`MealEndpoints`) handle non-form POSTs and `Results.LocalRedirect`.
- EF Core + SQLite, migrations run at startup. One migration exists
  (`InitialCreate`).

## Goals / Non-Goals

**Goals**

- One home (`/`): quick-add cards + entry form on top, seven-day summary below.
- `/history`: every day's entries rendered inline, paginated by day.
- Favorites: a form checkbox creates/updates them; cards on the home pre-fill the
  form; inline delete.
- Extract `WeeklySummaryView`, `DiaryDaySection`, `QuickAddCards` components and
  reuse `MealEntryForm`.
- Old routes (`/summary`, `/day/*`, `/entries/new`) redirect to `/`.

**Non-Goals**

- No change to weekly-summary computation (`SummaryService` untouched) or to
  portion/meal-group/tag rules.
- No week navigation on the summary (still "last 7 days").
- No favorites management page or profile section; no reordering/renaming beyond
  what re-saving from the form does.
- No one-tap logging from a card (card only pre-fills).
- No schema change to existing tables.

## Decisions

### Data model: `MealFavorite` with a serialized tag list

New entity in `Foodprint.Core/Domain`:

```
MealFavorite { Guid Id, Guid UserId, string Name, string NameNormalized,
               string? PortionSize, int? PortionGrams, int? MealGroupId,
               string TagsCsv, DateTime CreatedAt, DateTime UpdatedAt }
```

- Tags stored as a normalised, comma-joined string (`TagsCsv`), not a join to
  `Tag`. Rationale: favorites are lightweight templates, never queried by tag;
  a join would couple favorite lifecycle to per-user `Tag` rows for no benefit.
  Reuse `MealEntryRules.NormalizeTags` for the list ⇄ csv conversion.
- `NameNormalized` = `Name.Trim().ToLowerInvariant()`, persisted so dedup is a
  plain indexed lookup.
- Same portion CHECK constraint as `MealEntry`
  (`PortionSize IS NULL OR PortionGrams IS NULL`). Portion is optional on a
  favorite (a template may omit it and the user picks on submit) — but see
  "prefill" below.
- Index `(UserId, NameNormalized, MealGroupId)`, non-unique (SQLite treats NULL
  MealGroupId as distinct, so uniqueness can't be enforced cleanly; the service
  owns dedup). FK `MealGroupId` → `MealGroups` `ON DELETE SET NULL`, matching
  `MealEntry`.
- Migration `AddMealFavorites` adds only this table.

Alternative — `MealFavoriteTag` join reusing `Tag`: rejected, extra table and
`Tag` GC concerns for zero query benefit.

### `MealFavoriteService` (Core/Meals)

```
ListGroupedAsync(userId)      -> IReadOnlyList<FavoriteGroup>   // ordered by meal-group SortOrder, "no group" last
SaveAsync(userId, FavoriteDraft) -> MealFavorite                 // create or update-in-place by (user, NameNormalized, MealGroupId)
GetAsync(userId, favId)       -> MealFavoriteView?               // for prefill
DeleteAsync(userId, favId)    -> bool
```

- `FavoriteDraft` = name + portion (size xor grams) + mealGroupId + tags, built
  from a just-saved entry's values.
- All queries `Where(f => f.UserId == userId)`; cross-user `GetAsync`/`DeleteAsync`
  return null/false (tests assert it), mirroring `MealEntryService`.
- Registered by `AddFoodprintCore`.

### Favorite creation piggybacks on entry create

`MealEntryFormModel` gains `bool SaveFavorite`. The **page** handlers own the
side effect, not `MealEntryService` (keeps the entry service single-purpose):

- `Home.Submit`: `Entries.CreateAsync(...)`; on success, if `Model.SaveFavorite`,
  `Favorites.SaveAsync(me.Id, draft)` built from the submitted model; then
  redirect to `/`. A favorite failure is swallowed (logged) — the entry is
  already saved (spec: saving still succeeds).
- `EditEntry.Submit`: same optional call after a successful update. The checkbox
  is in the shared form, so edit gets it for free.

### Prefill from a favorite: GET with a query param

Card = `<a class="fp-quickcard" href="/?from={favId}#log">`. `Home` reads
`[SupplyParameterFromQuery] Guid? From`; on GET, if `From` resolves to one of the
user's favorites, seed `MealEntryFormModel` from it (name, portion, meal group,
tags) with `EatenAtLocal = now`. On an unknown/foreign id, ignore and show a
blank form. `#log` anchors to the form.

- Also support `/?date=yyyy-MM-dd` (from a history "add for this day" link) to
  seed just the date — keeps the removed `/entries/new?date=` affordance.
- Alternative — per-card POST that re-renders with a pre-filled `SupplyParameterFromForm`
  model: more moving parts (antiforgery, form nesting) for no gain; GET is
  idempotent and shareable.

### Delete a favorite: minimal-API endpoint

`MapPost("/favorites/{id:guid}/delete")` in a new `FavoriteEndpoints` (same shape
as the entry-delete endpoint) → `Favorites.DeleteAsync` → `LocalRedirect("/")`.
Card delete control is a tiny `<form method="post">` with just an icon button —
no blocking dialog (consistent with "preferable sin diálogo bloqueante"); the
action is cheap and reversible by re-saving.

### History: entries grouped by day, inline

New `DiaryService.GetHistoryDaysAsync(userId, zone, page)` →
`IReadOnlyList<DiaryDay>` + `HasMore`:

1. Pull `(EatenAt)` for the user, bucket to local dates, distinct, sort desc,
   page (20/page) — same shape as today's `GetHistoryAsync`.
2. For the page's date range, one query loads the entries (with `MealGroup` +
   tags) and group in memory into `DiaryDay`s.

`GetHistoryAsync` (summary rows) and `HistoryDay`/`HistoryPage` are removed;
`GetDayAsync` stays (still used? — only `Day.razor` used it; if nothing else
does, remove it too and its `DiaryDay` stays as the history/section shape).

### Components

- `WeeklySummaryView.razor` — parameter `WeeklySummary Model`; the streak card,
  per-day bars and top-tags card lifted verbatim from `Summary.razor`.
- `DiaryDaySection.razor` — parameters `DiaryDay Day`, `TimeZoneInfo Zone`;
  renders the date header + `MealEntryCard` list (+ an "add for this day" link).
- `QuickAddCards.razor` — parameter `IReadOnlyList<FavoriteGroup> Groups`;
  per group: label + `Icon Name="group-{key}"`, then the cards; each card shows
  name, a portion glyph/points, tags, an `<a>` to `/?from=` and a delete
  `<form>`.
- `Home.razor` (`@page "/"`) composes `QuickAddCards` + `MealEntryForm` +
  `WeeklySummaryView`; has the page `<h1>` for `FocusOnNavigate`.
- `History.razor` loops `DiaryDaySection` + the existing pager.

### Old-route redirects

URL-rewrite/redirect in `Program.cs` before routing: `/summary` → `/`,
`/day/{date}` → `/`, `/entries/new` → `/` (preserving `?date=` →
`/?date=`). A small `app.MapGet(pattern, () => Results.LocalRedirect("/"))` per
pattern with `.RequireAuthorization()` is enough and keeps it visible.

## Risks / Trade-offs

- **History pages can get long** (a day with many entries × 20 days) → acceptable
  at personal scale; pagination stays at 20 days and the bucketing query is the
  same cost as today's summary list.
- **Dedup relies on the service, not a DB constraint** (NULL MealGroupId) → a
  race could double-insert; single-user personal app, negligible. `SaveAsync`
  does the lookup+write in one `SaveChangesAsync`.
- **Favorite portion may be absent** if a template is saved from an entry and
  later the size set changes — not possible today (portion is required on
  entries), so every favorite created via the form carries a valid portion. The
  column stays nullable for forward-safety.
- **`from`/`date` GET params on `/`** are user-controllable → resolved strictly
  against the acting user's own favorites; unknown ids fall back to a blank form,
  no error surface.
- **E2E rework** — the journey's day/summary/new-entry steps all move; the
  navigation assertions change. One focused pass.
- **Removing `GetDayAsync`/`HistoryDay`** ripples into any test referencing them
  → update `DiaryServiceTests`.

## Migration Plan

1. Add migration `AddMealFavorites` (new table only). Runs at startup on deploy;
   nothing to backfill.
2. Deploy. Old bookmarks to `/summary`, `/day/*`, `/entries/new` redirect to `/`.
3. Rollback: revert the deploy; the unused `MealFavorites` table is harmless if
   left, or drop it with a down-migration.

## Open Questions

- Exact card visual (how portion is shown on a compact card — glyph only, or
  glyph + short label) — cosmetic, settled during implementation with the
  existing `Icon` set.
