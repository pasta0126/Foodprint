# weekly-summary Specification

## Purpose

Gives the owner a lightweight sense of their logging habits over the last seven
days without any nutrition or calorie modelling.

## Requirements

### Requirement: Seven-day window

The system SHALL compute the summary over the 7 calendar days ending today,
inclusive, in the user's profile time zone (see `user-profile`).

#### Scenario: Window boundaries

- **WHEN** the summary is viewed on a Wednesday
- **THEN** it covers the previous Thursday through the current Wednesday

### Requirement: Entries per day

The summary SHALL show the entry count for each of the 7 days, including days with
zero entries.

#### Scenario: Sparse week

- **WHEN** the user logged entries on only 2 of the last 7 days
- **THEN** the summary shows all 7 days, 5 of them at zero

### Requirement: Top tags

The summary SHALL list the up-to-5 most frequently used tags in the window with
their counts, ordered by count descending then tag name ascending. Ties beyond the
5th place SHALL be omitted deterministically by that ordering.

#### Scenario: Tag ranking

- **WHEN** tags in the window are lunch×6, home×6, snack×3, work×1
- **THEN** the summary lists home (6), lunch (6), snack (3), work (1)

#### Scenario: No tags

- **WHEN** no entries in the window have tags
- **THEN** the top-tags section shows an empty state

### Requirement: Logging streak

The summary SHALL show the current streak: the number of consecutive days ending
today on which at least one entry was logged.

#### Scenario: Active streak

- **WHEN** the user logged at least one entry today, yesterday, and the day before, but not 4 days ago
- **THEN** the streak is 3

#### Scenario: Broken streak

- **WHEN** the user logged nothing today
- **THEN** the streak is 0
