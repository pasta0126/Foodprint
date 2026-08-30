## Context

See `proposal.md` — Why. Constraints:

- Static SSR; downloads come from minimal-API endpoints (`MealEndpoints`,
  `FavoriteEndpoints`, `AuthEndpoints`) that return `Results.*` and rely on
  `.RequireAuthorization()` + `CurrentUser`.
- `Foodprint.Core` has no dependency on the web localization stack; services take
  primitives. Localized display strings today are resx keys resolved in the web
  layer (`IStringLocalizer<SharedResource>`), active culture set by
  `UseRequestLocalization` from the profile-language claim.
- `DayRange` converts a local `DateOnly` ↔ a half-open UTC window (DST-aware).
  `SummaryService` already computes a "consecutive days ending today" streak with
  a private helper.
- Entries: `MealEntry` (name, `EatenAt` UTC, `PortionSize` xor `PortionGrams`,
  `MealGroupId`, notes) + tags via `MealEntryTag`/`Tag`. `MealEntryService.ToView`
  is the standard projection.
- resx completeness across es/ca/en is build-enforced.

## Goals / Non-Goals

**Goals**

- One `GET /profile/export` endpoint → a single Markdown file download, scoped to
  the caller, for an optional inclusive local-date range.
- Markdown rendered in Core so it is unit-testable; localized text passed in.
- Analysis-oriented content: header, plate legend, aggregates, full entry table.
- Export zone on the profile page (plain GET form, no JS).

**Non-Goals**

- No JSON/CSV/other formats, no import, no scheduled or emailed exports.
- No new stored data, no schema change.
- Meals-per-day and missing-days are not exhaustively listed for very long ranges
  (see Decisions) — the file stays paste-able.

## Decisions

### Render Markdown in Core, inject the localized strings

`Foodprint.Core/Export/`:

- `MealExportStrings` — a plain record carrying every localized string the render
  needs: section headings, header field labels, table column headers, the
  "no group" / "no entries" fillers, plus maps `SizeLabel(string)`,
  `SizeLegend(string)` and `GroupLabel(string)` (delegates or dictionaries). Built
  in the web layer from `IStringLocalizer<SharedResource>` (reusing existing
  `Meal.Size.*`, `Meal.Size.*.Desc`, `MealGroup.*` keys); constructed with fixed
  English text in unit tests.
- `MealExportService(AppDbContext db, TimeProvider clock)`:
  - `Task<MealExport> BuildAsync(Guid userId, DateOnly? from, DateOnly? to, TimeZoneInfo zone, MealExportStrings s, CancellationToken)`
  - resolves the range (see below), queries the owner's entries in the UTC
    window (with group + tags), computes aggregates, and returns
    `MealExport(string FileName, string Markdown)`.
  - The pure render is a separate internal static (`MealExportMarkdown.Render(...)`)
    taking the resolved model + strings, so tests can hit it directly.
- Registered in `AddFoodprintCore`.

Rejected — service returns structured data, web renders Markdown: pushes the
fiddly part (escaping, table layout) out of the tested project.

### Range resolution (never errors)

Endpoint parses `from`/`to` as `yyyy-MM-dd` (invalid → null).

- `today = DayRange.LocalDate(clock.UtcNow, zone)`
- `resolvedTo = to ?? today`
- `resolvedFrom = from` (may stay null = "from the first entry")
- if `resolvedFrom` is not null and `> resolvedTo` → set both to null/today (full
  history through today)
- UTC window: `startUtc = resolvedFrom is null ? DateTime.MinValue : DayRange.For(resolvedFrom, zone).StartUtc`;
  `endUtc = DayRange.For(resolvedTo, zone).EndUtc`
- For "missing days" and the filename, an open `from` is reported as the first
  entry's local date (or, with no entries, equal to `resolvedTo`).

Filename: `foodprint-<from>-<to>.md` with `<from>` = `all` when open, else
`yyyy-MM-dd`.

### Aggregates

- **total meals** = row count; **days with entries** = distinct local dates.
- **meals per day**: only dates that have ≥1 entry → `date | count`, ascending.
- **meal-group distribution**: group by `MealGroupKey` (null bucket = "no group"),
  `label | count`, in catalog order then the null bucket.
- **tag frequency**: every tag in the range, `#tag | count`, count desc then tag
  asc (matches `weekly-summary` ordering); not truncated — analysis wants all.
- **streak**: consecutive local days with an entry ending `today`, independent of
  the export range. Extract `MealStreak.Current(IReadOnlySet<DateOnly> days, DateOnly today)`
  (pure) and reuse it from `SummaryService` too (small refactor + its own test).
  The export queries a bounded lookback (same 400-day cap as `SummaryService`)
  unioned with the range's dates.
- **missing days**: dates in `[reportedFrom .. resolvedTo]` with no entry. Listed
  explicitly only when that span ≤ 92 days; for a longer span the file gives the
  count and the first/last missing date instead. No entries at all → section
  omitted.

### Markdown shape and escaping

- Headings with `##`, key/value header as a bullet list, aggregates as small
  Markdown tables, entries as one big table:
  `| Date | Time | Name | Group | Portion | Tags | Notes |`.
- `MdCell(string?)`: `null/empty → ""`; else replace `\` → `\\`, `|` → `\|`,
  collapse any CR/LF to a single space, trim. Applied to name, tags joined by
  `, `, and notes. Portion cell is `SizeLabel(size)` or `"{grams} g"`.
- Times/dates formatted with invariant `yyyy-MM-dd` / `HH:mm` for
  machine-friendliness (the legend states the timezone); weekday names etc. are
  not needed by an AI.

### Web wiring

- `ExportEndpoints.MapExportEndpoints`:
  `app.MapGet("/profile/export", async (string? from, string? to, CurrentUser me, ProfileService profiles, MealExportService export, IStringLocalizer<SharedResource> L, CancellationToken ct) => { ... return Results.File(bytes, "text/markdown; charset=utf-8", fileName); }).RequireAuthorization();`
  — resolves the zone from the profile, builds `MealExportStrings` from `L`.
- Wired in `Program.cs` next to the other `Map*Endpoints()`.
- `ProfilePage.razor`: a new `<FpCard>` "Export data" with a
  `<form method="get" action="/profile/export">` — two `<input type="date">`
  (`from` prefilled today−30, `to` prefilled today, computed in the profile zone)
  and a submit button. Extract to `ExportZone.razor` only if it needs its own
  logic; a static form likely does not.

### Localization

New keys (es/ca/en): `Profile.Export`, `Profile.Export.Help`, `Export.From`,
`Export.To`, `Export.Download`, and the Markdown fixed text
`Export.Md.*` (title, intro line, field labels, section headings, table column
headers, `NoGroup`, `NoEntries`, `MissingDaysCount`). Portion-size names/legends
and meal-group labels reuse existing `Meal.Size.*`, `Meal.Size.*.Desc`,
`MealGroup.*`.

## Risks / Trade-offs

- **Very large export** (years of data) → big file; mitigated by not listing
  per-day/missing-day rows past a 92-day span, and tags/entries are inherently
  bounded by how much the user logged. The entry table itself is not capped — a
  heavy logger could produce a multi-thousand-row table; acceptable for a
  personal tool and still valid Markdown.
- **Streak is "as of today", not range-relative** → intentional and stated in the
  file; avoids confusing a historical export with current behaviour.
- **`SummaryService` refactor** to share `MealStreak` → covered by existing
  summary tests plus a new unit test; behaviour unchanged.
- **Markdown in the profile's language** means an English-speaking AI session
  gets a Catalan file if the profile is Catalan. Accepted — the file states its
  language and modern assistants handle it; matching the user's own language is
  the more predictable choice.

## Migration Plan

None. Read-only endpoint; deploy and it is available. Rollback = revert.

## Open Questions

None blocking. The exact wording of the legend/intro lines is settled during resx
authoring and does not affect the structure or tests.
