## 1. Domain + data: MealFavorite

- [x] 1.1 Add `MealFavorite` entity to `Foodprint.Core/Domain/Entities.cs` (`Id`, `UserId`, `Name`, `NameNormalized`, `PortionSize?`, `PortionGrams?`, `MealGroupId?`, `TagsCsv`, `CreatedAt`, `UpdatedAt`); nav to `User`
- [x] 1.2 Configure it in `AppDbContext` (DbSet, max lengths, `(UserId, NameNormalized, MealGroupId)` index, FK `MealGroupId` → `MealGroups` ON DELETE SET NULL, portion CHECK constraint) and add cascade from `User`
- [x] 1.3 `dotnet dotnet-ef migrations add AddMealFavorites --project src/Foodprint.Core --startup-project src/Foodprint.Cli`; verify only the new table is in the migration
- [x] 1.4 Add `MealFavorites` collection to `User` if needed for cascade; build

## 2. MealFavoriteService

- [x] 2.1 Create `Foodprint.Core/Meals/MealFavoriteService.cs`: `FavoriteDraft`, `MealFavoriteView`, `FavoriteGroup` records
- [x] 2.2 `SaveAsync(userId, FavoriteDraft)` — create, or update portion + tags in place when a favorite with the same `(userId, NameNormalized, MealGroupId)` exists
- [x] 2.3 `ListGroupedAsync(userId)` — favorites grouped by meal group, groups ordered by `MealGroup.SortOrder`, "no group" bucket last
- [x] 2.4 `GetAsync(userId, favId)` and `DeleteAsync(userId, favId)` — both scoped by `userId`, cross-user returns null / false
- [x] 2.5 Register in `AddFoodprintCore`
- [x] 2.6 Unit tests: dedup updates in place, different meal group is distinct, grouping/order, ownership isolation on Get/Delete

## 3. Diary service: history days with entries

- [x] 3.1 Add `DiaryService.GetHistoryDaysAsync(userId, zone, page)` → `(IReadOnlyList<DiaryDay> Days, bool HasMore)`, 20 days/page, entries ordered by time asc within each day
- [x] 3.2 Remove `GetHistoryAsync`, `HistoryDay`, `HistoryPage`; remove `GetDayAsync` if nothing else uses it (keep `DiaryDay`)
- [x] 3.3 Update `DiaryServiceTests` for the new method; drop tests for removed members

## 4. Reusable components

- [x] 4.1 `Components/Meals/WeeklySummaryView.razor` — `[Parameter] WeeklySummary Model`; streak card + per-day bars + top-tags card moved verbatim from `Summary.razor`
- [x] 4.2 `Components/Meals/DiaryDaySection.razor` — `[Parameter] DiaryDay Day`, `TimeZoneInfo Zone`; date header + `MealEntryCard` list + "add for this day" link (`/?date=`)
- [x] 4.3 `Components/Meals/QuickAddCards.razor` — `[Parameter] IReadOnlyList<FavoriteGroup> Groups`; per group: localized label + `Icon Name="group-{key}"`; each card: name, portion glyph, tags, `<a href="/?from={id}#log">`, delete `<form method="post" action="/favorites/{id}/delete">`
- [x] 4.4 bUnit: `QuickAddCards` groups by meal group and renders a delete form per card; `DiaryDaySection` lists entries in time order

## 5. Meal-entry form: favorite checkbox + prefill

- [x] 5.1 `MealEntryFormModel` gains `bool SaveFavorite`; helper to build a `FavoriteDraft` from the model
- [x] 5.2 `MealEntryForm.razor` renders a "save to favorites" checkbox (localized `Meal.SaveFavorite`)
- [x] 5.3 `MealEntryFormModel.FromFavorite(MealFavoriteView, TimeZoneInfo, DateTime nowLocal)` factory for prefill
- [x] 5.4 bUnit: checkbox binds; `FromFavorite` maps name/portion/group/tags and sets eaten-at to now

## 6. Home view

- [x] 6.1 Create `Components/Pages/Home.razor` at `@page "/"`: `<h1>`, `QuickAddCards`, `MealEntryForm` (anchor `#log`), `WeeklySummaryView`
- [x] 6.2 `[SupplyParameterFromQuery] Guid? From` and `string? Date`: on GET seed the form model from the favorite (own only) and/or the date; unknown/foreign `from` → blank form
- [x] 6.3 `Submit`: `Entries.CreateAsync`; on success, if `Model.SaveFavorite` call `Favorites.SaveAsync` (swallow + log favorite failure); redirect to `/`
- [x] 6.4 Load `Favorites.ListGroupedAsync` and `Summaries.GetAsync` for the view
- [x] 6.5 Delete `Day.razor` (frees `/` and `/day/{date}`)

## 7. History view

- [x] 7.1 Rework `History.razor` to loop `DiaryDaySection` over `GetHistoryDaysAsync`, keep the `?page=` pager and empty state

## 8. Weekly page removal + edit form

- [x] 8.1 Delete `Summary.razor`; `EditEntry.razor` calls `Favorites.SaveAsync` after a successful update when `Model.SaveFavorite`
- [x] 8.2 `FavoriteEndpoints.MapFavoriteEndpoints` with `POST /favorites/{id:guid}/delete` → `DeleteAsync` → `LocalRedirect("/")`; wire in `Program.cs`
- [x] 8.3 Old-route redirects in `Program.cs`: `GET /summary`, `/day/{date}`, `/entries/new` → `LocalRedirect("/")` (map `?date` through), `.RequireAuthorization()`

## 9. Navigation + localization

- [x] 9.1 `AppShell.razor` nav → two items: Home (`/`, icon `today`) and History (`/history`, icon `history`); drop the weekly link; relabel `Nav.Today` → `Nav.Home` (or reuse) across ca/es/en
- [x] 9.2 New resx keys in `SharedResource.resx` + `.ca` + `.en`: `Meal.SaveFavorite`, `Home.Favorites`, `Home.QuickAdd`, `Favorite.Delete`, history/day-section labels, any empty states; remove now-unused keys (`Day.*`, `Summary.*` that no longer render, `History.Count`/preview if dropped)
- [x] 9.3 `dotnet test --filter FullyQualifiedName~ResourceCompletenessTests` green

## 10. Verification

- [x] 10.1 Update `MealJourneyE2E`: create from the home form; assert entry shows in `/history` under today; no `/summary` / `/day` / `/entries/new` (redirect to `/`); tick "save to favorites", then use the card to pre-fill and log a second entry; delete the favorite
- [x] 10.2 `dotnet build` clean (TreatWarningsAsErrors) and `dotnet test` green
- [x] 10.3 Manual pass: home dashboard (summary | form | cards), history — verified in-browser at desktop widths; single-column below 60rem by construction
- [x] 10.4 `openspec validate home-quick-add-and-nav-merge --strict`
