## MODIFIED Requirements

### Requirement: Create a meal entry

An authenticated user SHALL be able to create a meal entry. The system SHALL
require a non-empty name (1–120 characters), an eaten-at date/time (defaulting to
the current server time), and a portion (see "Portion"). The system SHALL accept
an optional meal group (see "Meal group"), optional notes (up to 1000
characters), and an optional list of tags. The eaten-at time SHALL NOT be more
than 24 hours in the future; there is no limit on how far in the past it may be.

The create form MAY be pre-filled from a saved favorite (see `meal-favorites`),
and MAY offer a "save to favorites" option that, when enabled, also persists the
entry's name, portion, meal group and tags as a favorite. Neither affects the
validation rules above.

#### Scenario: Minimal valid entry

- **WHEN** the user submits an entry with a name and a portion size
- **THEN** the entry is saved with eaten-at defaulted to the current time
- **AND** it appears in the history view's day section for that day

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

#### Scenario: Save as favorite alongside the entry

- **WHEN** the user submits a valid entry with the "save to favorites" option enabled
- **THEN** the entry is created and a favorite is created or updated per `meal-favorites`
