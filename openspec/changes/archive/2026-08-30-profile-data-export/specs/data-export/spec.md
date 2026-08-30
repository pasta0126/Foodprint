## Purpose

Lets the owner take their meal log out of the app for a chosen date range as a
single Markdown file that reads well on its own and is meant to be handed to an AI
assistant for analysis.

## ADDED Requirements

### Requirement: Export the meal log for a date range

An authenticated user SHALL be able to download their own meal log as one
Markdown file. The request SHALL accept an optional start date and an optional
end date, interpreted as calendar days in the user's profile time zone and
inclusive of both ends. A missing start date SHALL mean "from the first entry
ever"; a missing end date SHALL mean "through today". When the start date is
after the end date, or a date is unparseable, the system SHALL fall back to those
defaults rather than fail. The export SHALL contain only the requesting user's
entries.

The response SHALL be delivered as a file download (`Content-Disposition:
attachment`) with a `.md` filename that reflects the resolved range and a
Markdown content type.

#### Scenario: Download a range

- **WHEN** the user requests an export from 2026-08-01 to 2026-08-15
- **THEN** the response is a Markdown file attachment covering entries whose local date is 2026-08-01 through 2026-08-15 inclusive

#### Scenario: Open-ended range

- **WHEN** the user requests an export with no dates
- **THEN** the file covers every entry the user has ever logged, up to today

#### Scenario: Reversed or invalid dates

- **WHEN** the user requests an export with a start date after the end date
- **THEN** the system exports the full history through today instead of returning an error

#### Scenario: Only the owner's data

- **WHEN** a user requests an export
- **THEN** the file contains none of any other user's entries

#### Scenario: Unauthenticated request

- **WHEN** an unauthenticated client requests the export endpoint
- **THEN** the request is rejected and no data is returned

### Requirement: Export file contents and structure

The Markdown file SHALL be self-describing and written in the profile's active
language. It SHALL contain, in order:

1. **A header** identifying the file as a Foodprint meal-log export for AI
   analysis, with the generation timestamp, the profile time zone, the language,
   the resolved date range, and the total number of meals in the file.
2. **A legend** explaining the named portion sizes against a standard plate
   (small ≈ a third of a plate or a small bowl; medium ≈ between a third and two
   thirds; large ≈ a full plate; very large ≈ a heaped or oversized plate), that
   a portion may instead be given in grams, and the label for each meal group.
3. **An analysis section** with: the total meals and the number of distinct days
   with at least one entry in the range; meals per day; the count of entries per
   meal group; tag frequency with counts; the current run of consecutive days
   with an entry ending today; and the list of dates within the range that have
   no entry.
4. **An entry table** with one row per entry — local date, local time, name, meal
   group, portion (localized size name, or "N g"), tags, notes — ordered by
   eaten-at ascending.

Any Markdown-significant characters in user-supplied text (names, tags, notes)
SHALL be escaped so the table and surrounding structure stay intact.

#### Scenario: Header and legend present

- **WHEN** any export is generated
- **THEN** the file begins with the identifying header and includes the portion-size and meal-group legend

#### Scenario: Aggregates reflect the data

- **WHEN** the range contains 10 entries across 4 distinct days, 6 of them tagged `lunch`
- **THEN** the analysis section reports 10 meals, 4 days with entries, and `lunch` at a count of 6

#### Scenario: Missing days are listed

- **WHEN** the range spans 7 days and the user logged nothing on 2 of them
- **THEN** the analysis section lists those 2 dates as having no entry

#### Scenario: Entry rows

- **WHEN** an entry is "Pa amb tomàquet", portion large, group breakfast, tags `home`, no notes
- **THEN** the table has a row with that name, the localized "large" label, the localized breakfast label, `home`, and an empty notes cell

#### Scenario: Special characters are escaped

- **WHEN** an entry name contains a `|` character
- **THEN** the exported table renders as a valid Markdown table with that character escaped

#### Scenario: Empty range

- **WHEN** the resolved range contains no entries
- **THEN** the file still has the header and legend, the analysis section shows zero meals, and the entry table is empty or replaced by an explicit "no entries" note
