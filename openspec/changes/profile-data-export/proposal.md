## Why

The diary accumulates useful signal about what and when someone eats, but there
is no way to get it out. The owner should be able to hand their log to an AI
assistant and ask it to spot patterns — meal timing, portion drift, gaps,
recurring foods. A single self-contained Markdown file that pastes straight into
a chat is the most direct way to enable that.

## What Changes

- **New "Export data" zone on the profile page**: a From / To date range
  (pre-filled to the last 30 days, both ends optional) and a Download button.
- **New download endpoint** `GET /profile/export` (auth-required, scoped to the
  signed-in user) that returns a Markdown file of that user's meal log for the
  range, as an attachment named `foodprint-<from>-<to>.md`.
- **The Markdown file is written for AI analysis** and contains, in the profile's
  active language:
  1. a header — what the file is, generation timestamp, profile time zone,
     language, the exported range, total meal count;
  2. a legend — what each portion size means against a plate, that grams are the
     alternative, and the meal-group labels;
  3. an analysis section — total meals and days-with-entries in the range, meals
     per day, meal-group distribution, tag frequency, the current logging streak
     (as of today), and which days in the range have no entry;
  4. a table of every entry in the range (date, local time, name, meal group,
     portion, tags, notes), oldest first, with Markdown special characters
     escaped.

## Capabilities

### New Capabilities

- `data-export`: lets the owner download their own meal log for a date range as a
  single Markdown file suitable for AI analysis.

### Modified Capabilities

- `user-profile`: the "Profile page is the account hub" requirement gains data
  export as one of the things managed from the profile page.

## Impact

- **Core**: new `MealExportService` (`Foodprint.Core/Export/`) — reads the user's
  entries in the UTC window derived from the local date range (reuses
  `DayRange`), computes the aggregates (reusing the streak logic shape from
  `SummaryService`), and renders the Markdown. Localized strings (size/group
  labels, section headings) are supplied to it so the render stays unit-testable
  and Core keeps no dependency on the web localization stack.
- **Web**: new `ExportEndpoints` mapping `GET /profile/export`, wired in
  `Program.cs`; the export zone added to `ProfilePage.razor` (extracted to a
  small component if it earns it). Localizer resolves the active culture as usual
  via `UseRequestLocalization`.
- **Localization**: new resx keys in all three of `SharedResource.resx` (es),
  `.ca`, `.en` for the profile zone and for the Markdown's fixed section text —
  enforced by `ResourceCompletenessTests`.
- **Data / schema**: none. Read-only feature, no migration.
- **Tests**: unit tests for the Markdown render (header, legend, aggregates,
  entry table, pipe escaping, empty range) and the aggregate/no-entry-days
  computation; endpoint tests (auth required, `Content-Disposition`, cross-user
  isolation); a short profile-export step added to `MealJourneyE2E`.
