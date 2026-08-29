## Purpose

Presents logged meal entries back to the owner: one day at a time, and as a
scrollable history of past days.

## ADDED Requirements

### Requirement: Day view

The system SHALL provide a view of all entries for a single calendar day in the
user's profile time zone (see `user-profile`), ordered by eaten-at time ascending. The view SHALL default
to today and SHALL allow moving to the previous or next day. Each entry SHALL show
its name, time, portion, tags, and a notes indicator.

#### Scenario: Today with entries

- **WHEN** the user opens the diary home and has logged 3 entries today
- **THEN** all 3 are listed in time order with their fields

#### Scenario: Empty day

- **WHEN** the user navigates to a day with no entries
- **THEN** the view shows an empty state with a prompt to add an entry

#### Scenario: Day navigation

- **WHEN** the user activates "previous day"
- **THEN** the view shows the prior calendar day's entries and updates the visible date

### Requirement: History view

The system SHALL provide a reverse-chronological list of days that have at least
one entry, each day showing its date, entry count, and a preview of entry names.
The list SHALL be paginated at 20 days per page.

#### Scenario: Browsing history

- **WHEN** the user opens the history view with entries spanning 45 days
- **THEN** the first page shows the 20 most recent days with entries
- **AND** a control loads the next page

#### Scenario: Jump to a day

- **WHEN** the user selects a day in the history list
- **THEN** the day view opens for that date

#### Scenario: No history

- **WHEN** the user has never logged an entry
- **THEN** the history view shows an empty state
