## MODIFIED Requirements

### Requirement: Portion

An entry's portion is optional. When provided, it SHALL be expressed in exactly
one of two ways: a named size from the fixed set (`small`, `medium`, `large`,
`very-large`), or a quantity in grams as a positive integer between 1 and 5000.
The system SHALL reject an entry that specifies both, or a grams value outside
that range.

Named sizes are defined against a standard flat dinner plate as the shared
reference, so the amount can be estimated by eye:

- `small` — about a third of a plate, or a small bowl
- `medium` — between a third and two thirds of a plate
- `large` — a full plate
- `very-large` — a heaped or oversized plate

The named size is the primary way to record a portion; grams is a secondary,
more-precise option. Which representation the UI presents first is an interface
concern (see `design-system`), but the stored value SHALL always be exactly one
of the two or neither.

#### Scenario: Named size

- **WHEN** the user selects portion size `medium`
- **THEN** the entry is saved with a named portion and no grams value

#### Scenario: Fourth named size

- **WHEN** the user selects portion size `very-large`
- **THEN** the entry is saved with the named portion `very-large` and no grams value

#### Scenario: Grams

- **WHEN** the user enters a portion of `250` grams
- **THEN** the entry is saved with a grams portion and no named size

#### Scenario: Both provided

- **WHEN** the user submits both a named size and a grams value
- **THEN** the system rejects the entry with a validation message

#### Scenario: Out-of-range grams

- **WHEN** the user submits `0` or `6000` grams
- **THEN** the system rejects the entry with a validation message

#### Scenario: Unknown named size

- **WHEN** a submitted entry specifies a named size that is not in the fixed set
- **THEN** the system rejects the entry with a validation message

### Requirement: Meal group

An entry MAY reference exactly one meal group chosen from a system-managed catalog
(for example breakfast, lunch, dinner, snack). The catalog is a closed set: its
members SHALL only be created, renamed, or retired through the backend (a database
seed/migration or an admin API), never through the meal-logging UI. The system
SHALL reject an entry whose group is not an active catalog member.

When a user starts creating a new entry, the system SHALL pre-select a suggested
meal group based on the eaten-at time of day, interpreted in the user's profile
time zone. The suggestion maps typical meal hours to catalog groups (for example
morning → breakfast, midday → lunch, evening → dinner, otherwise snack) and falls
back to no selection when no active catalog member matches. The suggestion is only
a default: the user SHALL be able to change or clear it before saving, and the
system SHALL NOT re-apply a suggestion when editing an existing entry.

#### Scenario: Valid group

- **WHEN** the user picks "lunch" from the group list while logging an entry
- **THEN** the entry is saved referencing that catalog group

#### Scenario: Suggested group on creation

- **WHEN** the user opens the new-entry form at 08:30 in their profile time zone and "breakfast" is an active catalog member
- **THEN** the meal group field is pre-selected to "breakfast"

#### Scenario: Suggestion is overridable

- **WHEN** the form has pre-selected "breakfast" and the user changes it to "snack" and saves
- **THEN** the entry is saved referencing "snack"

#### Scenario: No suggestion when editing

- **WHEN** the user edits an existing entry that has no meal group
- **THEN** the meal group field stays empty and no suggestion is applied

#### Scenario: Unknown group

- **WHEN** a submitted entry references a group id that is not an active catalog member
- **THEN** the system rejects the entry with a validation message

#### Scenario: Catalog is read-only in the app

- **WHEN** a user looks for a way to add or edit meal groups in the UI
- **THEN** no such control exists; the group selector only lists existing active groups
