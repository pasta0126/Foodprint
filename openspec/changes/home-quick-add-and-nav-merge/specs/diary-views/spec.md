## ADDED Requirements

### Requirement: Home view

The system SHALL present a single home view at the app root that combines, in
this order:

1. a quick-add block — the meal-favorite cards (see `meal-favorites`) followed by
   the meal-entry form (see `meal-logging`), so a new entry can be logged without
   leaving the page; and
2. the seven-day summary (see `weekly-summary`) — streak, per-day counts and top
   tags.

The home view SHALL NOT provide single-day browsing or previous/next-day
navigation. Submitting the form SHALL create the entry and return the user to the
home view.

#### Scenario: Home shows quick-add above the summary

- **WHEN** a signed-in user opens the app root
- **THEN** the favorite cards and the entry form appear above the seven-day summary

#### Scenario: Logging from the home

- **WHEN** the user fills the home entry form and submits a valid entry
- **THEN** the entry is saved and the home view re-renders showing the updated summary

#### Scenario: No day navigation

- **WHEN** the user is on the home view
- **THEN** there is no control to move to a previous or next single day

## MODIFIED Requirements

### Requirement: History view

The system SHALL provide a reverse-chronological view of the days that have at
least one entry. Each day SHALL be rendered as a section showing its date and all
of that day's entries in full (name, time, portion, tags, notes indicator),
ordered by eaten-at time ascending, in the user's profile time zone. The list
SHALL be paginated at 20 days per page. There SHALL NOT be a separate per-day
page.

#### Scenario: Browsing history

- **WHEN** the user opens the history view with entries spanning 45 days
- **THEN** the first page shows the 20 most recent days with entries, each expanded to its entries
- **AND** a control loads the next page

#### Scenario: Entries within a day

- **WHEN** a day in the list has three entries
- **THEN** all three are shown under that day's date in time order

#### Scenario: Jump to a day

- **WHEN** the user wants to see a specific day's entries
- **THEN** they find that day's section in the history list; there is no separate day page to open

#### Scenario: No history

- **WHEN** the user has never logged an entry
- **THEN** the history view shows an empty state

## REMOVED Requirements

### Requirement: Day view

**Reason**: Single-day browsing is replaced by the combined home view (no day
navigation) and the history view (which now shows every day's entries inline).

**Migration**: Requests to the old day routes (`/day/{date}`) redirect to the
home view. Today's entries are visible on the history view's first day section;
the home view is for logging, not browsing a specific day.
