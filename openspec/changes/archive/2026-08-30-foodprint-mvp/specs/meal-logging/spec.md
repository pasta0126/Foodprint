## Purpose

Lets the owner record what they ate as structured diary entries, and change or
remove those entries later.

## ADDED Requirements

### Requirement: Create a meal entry

An authenticated user SHALL be able to create a meal entry. The system SHALL
require a non-empty name (1–120 characters) and an eaten-at date/time, defaulting
the date/time to the current server time. The system SHALL accept an optional
portion (see "Portion"), an optional meal group (see "Meal group"), optional notes
(up to 1000 characters), and an optional list of tags. The eaten-at time SHALL NOT
be more than 24 hours in the future; there is no limit on how far in the past it
may be.

#### Scenario: Minimal valid entry

- **WHEN** the user submits an entry with only a name
- **THEN** the entry is saved with eaten-at defaulted to the current time
- **AND** it appears in that day's view

#### Scenario: Full entry

- **WHEN** the user submits a name, past date/time, portion `large`, a meal group, notes, and two tags
- **THEN** all fields are persisted and shown on the entry

#### Scenario: Missing name

- **WHEN** the user submits an entry with an empty or whitespace-only name
- **THEN** the system rejects it with a validation message
- **AND** nothing is saved

#### Scenario: Far-future time

- **WHEN** the user submits an eaten-at time more than 24 hours ahead
- **THEN** the system rejects it with a validation message

### Requirement: Portion

An entry's portion is optional. When provided, it SHALL be expressed in exactly one
of two ways: a named size from the fixed set (`small`, `medium`, `large`), or a
quantity in grams as a positive integer between 1 and 5000. The system SHALL reject
an entry that specifies both, or a grams value outside that range.

#### Scenario: Named size

- **WHEN** the user selects portion size `medium`
- **THEN** the entry is saved with a named portion and no grams value

#### Scenario: Grams

- **WHEN** the user enters a portion of `250` grams
- **THEN** the entry is saved with a grams portion and no named size

#### Scenario: Both provided

- **WHEN** the user submits both a named size and a grams value
- **THEN** the system rejects the entry with a validation message

#### Scenario: Out-of-range grams

- **WHEN** the user submits `0` or `6000` grams
- **THEN** the system rejects the entry with a validation message

### Requirement: Meal group

An entry MAY reference exactly one meal group chosen from a system-managed catalog
(for example breakfast, lunch, dinner, snack). The catalog is a closed set: its
members SHALL only be created, renamed, or retired through the backend (a database
seed/migration or an admin API), never through the meal-logging UI. The system
SHALL reject an entry whose group is not an active catalog member.

#### Scenario: Valid group

- **WHEN** the user picks "lunch" from the group list while logging an entry
- **THEN** the entry is saved referencing that catalog group

#### Scenario: Unknown group

- **WHEN** a submitted entry references a group id that is not an active catalog member
- **THEN** the system rejects the entry with a validation message

#### Scenario: Catalog is read-only in the app

- **WHEN** a user looks for a way to add or edit meal groups in the UI
- **THEN** no such control exists; the group selector only lists existing active groups

### Requirement: Tags

The system SHALL normalize each tag by trimming whitespace and l-casing it, SHALL
drop empty tags and duplicates within an entry, and SHALL allow at most 10 tags
per entry, each 1–30 characters.

#### Scenario: Tag normalization

- **WHEN** the user submits tags `"  Lunch "`, `"lunch"`, and `"HOME"`
- **THEN** the entry is saved with tags `lunch` and `home`

#### Scenario: Too many tags

- **WHEN** the user submits 11 or more tags
- **THEN** the system rejects the entry with a validation message

### Requirement: Edit a meal entry

An authenticated user SHALL be able to edit any field of an existing entry they
own. The same validation rules as creation SHALL apply. The system SHALL record an
updated-at timestamp on every change.

#### Scenario: Edit succeeds

- **WHEN** the user changes the name and portion of an existing entry
- **THEN** the changes persist
- **AND** the updated-at timestamp advances

#### Scenario: Edit fails validation

- **WHEN** the user clears the name of an existing entry and saves
- **THEN** the system rejects the change and the stored entry is unchanged

### Requirement: Delete a meal entry

An authenticated user SHALL be able to delete an entry they own. Deletion SHALL
require an explicit confirmation and SHALL be permanent.

#### Scenario: Delete with confirmation

- **WHEN** the user confirms deletion of an entry
- **THEN** the entry is removed and no longer appears in any view

#### Scenario: Delete cancelled

- **WHEN** the user opens the delete confirmation and cancels
- **THEN** the entry remains unchanged

### Requirement: Ownership isolation

The system SHALL scope every read and write to the authenticated user's own
entries. A request for an entry id that belongs to another user or does not exist
SHALL return "not found".

#### Scenario: Cross-user access

- **WHEN** a user requests an entry id they do not own
- **THEN** the system responds with "not found"
