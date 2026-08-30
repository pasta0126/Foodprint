## 1. Shared streak helper

- [ ] 1.1 Add `MealStreak.Current(IReadOnlySet<DateOnly> daysWithEntries, DateOnly today)` (pure) in `Foodprint.Core/Meals/`
- [ ] 1.2 Refactor `SummaryService.ComputeStreakAsync` to use it; keep behaviour identical
- [ ] 1.3 Unit test `MealStreak.Current` (active streak, broken today, empty, gap)

## 2. Export service (Core)

- [ ] 2.1 `Foodprint.Core/Export/MealExportStrings.cs` — record with section/label/column strings + `SizeLabel`, `SizeLegend`, `GroupLabel` delegates
- [ ] 2.2 `Foodprint.Core/Export/MealExportService.cs` — `BuildAsync(userId, DateOnly? from, DateOnly? to, TimeZoneInfo zone, MealExportStrings, ct)`: resolve range (never errors; reversed/invalid → full history through today), query owner entries in the UTC window with group + tags
- [ ] 2.3 Compute aggregates: total meals, distinct days-with-entries, meals-per-day (days with entries only), per-meal-group counts, tag frequency (count desc, name asc, all tags), streak (via `MealStreak`, lookback bounded), missing days in `[reportedFrom..resolvedTo]` (list when span ≤ 92 days, else count + first/last)
- [ ] 2.4 `MealExportMarkdown.Render(model, strings)` internal static — header bullet list, legend, analysis tables, entry table; `MdCell` escaping (`\`, `|`, CR/LF→space, trim); dates `yyyy-MM-dd`, times `HH:mm`
- [ ] 2.5 Return `MealExport(string FileName, string Markdown)`; filename `foodprint-<from|all>-<to>.md`
- [ ] 2.6 Register `MealExportService` in `AddFoodprintCore`

## 3. Export service tests

- [ ] 3.1 Range resolution: explicit range, open `from`, open `to`, reversed → full history, unparseable handled by the endpoint (see 5.3)
- [ ] 3.2 Aggregates: 10 entries / 4 days / 6×`lunch` → correct totals & tag count; missing-days list for a 7-day range with 2 empty days
- [ ] 3.3 Markdown render: header + legend present; entry row maps name/size label/group label/tags/notes; `|` in a name stays a valid table; empty range → header + legend + zero meals + no-entries note
- [ ] 3.4 Ownership: another user's entries never appear in the file

## 4. Localization

- [ ] 4.1 Add `Profile.Export`, `Profile.Export.Help`, `Export.From`, `Export.To`, `Export.Download` to `SharedResource.resx` (es) + `.ca` + `.en`
- [ ] 4.2 Add `Export.Md.*` fixed-text keys (title, intro, generated/timezone/language/range/total labels, legend headings, analysis headings, table column headers, `NoGroup`, `NoEntries`, `MissingDaysCount`) to all three
- [ ] 4.3 `dotnet test --filter FullyQualifiedName~ResourceCompletenessTests` green

## 5. Web endpoint

- [ ] 5.1 `Components/.../ExportEndpoints.cs` — `MapExportEndpoints`: `GET /profile/export` (`from`, `to` query), `.RequireAuthorization()`, resolve zone from `ProfileService`, build `MealExportStrings` from `IStringLocalizer<SharedResource>`, return `Results.File(bytes, "text/markdown; charset=utf-8", fileName)`
- [ ] 5.2 Wire `app.MapExportEndpoints()` in `Program.cs`
- [ ] 5.3 Parse `from`/`to` with `DateOnly.TryParseExact("yyyy-MM-dd")`; null on failure → service defaults
- [ ] 5.4 Endpoint tests (WebApplicationFactory): 302/401 when unauthenticated; authed → `Content-Disposition: attachment` + `.md` name + `text/markdown`; body contains the header; a second user's entry is absent

## 6. Profile UI

- [ ] 6.1 Add an "Export data" `<FpCard>` to `ProfilePage.razor`: `<form method="get" action="/profile/export">` with two `<input type="date">` (`from` = today−30, `to` = today, computed in the profile zone) and a submit button
- [ ] 6.2 Short helper text (`Profile.Export.Help`) explaining it produces a Markdown file for AI analysis

## 7. Verification

- [ ] 7.1 Add a step to `MealJourneyE2E`: from `/profile`, submit the export form and assert a `.md` download (or that the endpoint returns 200 + markdown for the session)
- [ ] 7.2 `dotnet build` clean (TreatWarningsAsErrors) and `dotnet test` green
- [ ] 7.3 Manual check: download for a real range, confirm the file opens and pastes cleanly into a chat (needs the running app)
- [ ] 7.4 `openspec validate profile-data-export --strict`
