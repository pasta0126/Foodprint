## 1. Domain: fourth portion size

- [x] 1.1 Add `VeryLarge = "very-large"` to `PortionSizes` and include it in `All` (`src/Foodprint.Core/Domain/Constants.cs`)
- [x] 1.2 Grep for `"large"`/`PortionSizes`/`Meal.Size.` across `src/` and update every exhaustive switch or mapping (portion display, form model `ToInput`, any summary code)
- [x] 1.3 Add unit tests: `very-large` passes `MealEntryRules.Validate`, an unknown size is rejected

## 2. Localization keys

- [x] 2.1 Add `Meal.Size.very-large` label + per-size plate-reference description keys (`Meal.Size.small.Desc` … `very-large.Desc`) to `SharedResource.resx` (es), `.ca`, `.en`
- [x] 2.2 Add identity-menu keys (`Nav.Account`, `Nav.Profile` reuse, `Nav.SignOut` reuse), profile theme keys (`Theme.Label`, `Theme.System`, `Theme.Light`, `Theme.Dark`), and icon aria-label keys
- [x] 2.3 Run `dotnet test --filter FullyQualifiedName~ResourceCompletenessTests` and resolve any drift

## 3. Icon component

- [x] 3.1 Create `Components/Shared/Icon.razor` with `Name`/`Title` params, inline `<svg>` from a static path-data dictionary, `aria-hidden` vs `role="img"`+`<title>`
- [x] 3.2 Populate the icon set: nav (today, history, summary), actions (add, edit, delete, save, back), meal groups, four portion-size plate glyphs
- [x] 3.3 bUnit test: decorative mode emits `aria-hidden`, titled mode emits `role="img"` + `<title>`

## 4. Deterministic avatar + identity control

- [x] 4.1 Add `AvatarColor(string key)` helper + a design-token palette of ~12 AA-contrast background colors; unit test determinism and palette membership
- [x] 4.2 Create an `IdentityBadge` markup (avatar span with initial + color, name/email beside it, hidden below a breakpoint)
- [x] 4.3 Build the header identity control as `<details><summary>` (badge) with a panel containing `<a href="/profile">` and the sign-out `<form>`; keyboard-operable, no render mode
- [x] 4.4 bUnit test: same user → same initial + color; menu contains Profile link and sign-out form

## 5. App shell rework

- [x] 5.1 In `AppShell.razor` remove the `/profile` `NavLink`, the `LanguageSwitcher`, the `ThemeToggle`, and the header sign-out form
- [x] 5.2 Add the identity control to the trailing edge of the header; add icons to the remaining nav links
- [x] 5.3 Responsive CSS: header wraps/stacks below 768px, nav `<details>` toggle restyled, identity name hides on narrow, no horizontal scroll at 320px
- [x] 5.4 bUnit/e2e assertion: header renders nav + identity control only; no language/theme/sign-out controls in the header

## 6. Profile page as account hub

- [x] 6.1 Add a theme control to `ProfilePage.razor` (three script-wired options calling `fpTheme.set`, no interactive component)
- [x] 6.2 Add a visible "Sign out" control (posts to `/auth/sign-out`) to the profile page
- [x] 6.3 Keep language selector; verify save still force-reloads so the language claim rebuilds
- [x] 6.4 Remove `Components/Shared/ThemeToggle.razor` (now unused) and any leftover references
- [x] 6.5 bUnit test: profile page exposes name, tz, language, theme, and sign-out

## 7. Portion input UX

- [x] 7.1 Rework `MealEntryForm.razor` portion section: "no portion" + four size options as a segmented/radio row, each with plate glyph + localized description
- [x] 7.2 Move grams into a collapsed `<details>` ("enter exact grams instead")
- [x] 7.3 Update `MealEntryFormModel` so server-side `ToInput` derives XOR: grams (when the disclosure has a value) else selected size; keep the `PortionBothProvided` safety net
- [x] 7.4 Show the chosen size's description/glyph on `MealEntryCard.razor`
- [x] 7.5 bUnit test: selecting a size then entering grams saves grams and clears size; both-empty saves no portion

## 8. Time-of-day meal-group suggestion

- [x] 8.1 Add pure helper `MealGroupSuggestion.ForLocalTime(TimeOnly, IReadOnlyList<MealGroupOption>) : int?` with the documented bands + fallback
- [x] 8.2 Unit tests: 08:30→breakfast, 13:00→lunch, 20:30→dinner, 16:00→snack, missing catalog member→fallback/null
- [x] 8.3 Call it in `NewEntry.razor` when building the initial form model, converting "now" to local time via the profile time zone
- [x] 8.4 Confirm `EditEntry.razor` does not apply any suggestion
- [x] 8.5 bUnit/e2e: new-entry form pre-selects the time-appropriate group and it can be overridden before save

## 9. Verification

- [x] 9.1 Update `MealJourneyE2E` and any journey that selects the old navbar profile link / header sign-out, or assumes the 3-size set
- [x] 9.2 `dotnet build` clean (TreatWarningsAsErrors) and `dotnet test` green
- [ ] 9.3 Manual pass at 320/375/768/1024px: header, new-entry form, day view, profile page (needs a browser session — not done autonomously)
- [x] 9.4 `openspec validate diary-ux-rework --strict`
