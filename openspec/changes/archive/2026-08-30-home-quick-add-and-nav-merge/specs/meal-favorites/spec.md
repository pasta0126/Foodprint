## Purpose

Lets the owner turn meals they log repeatedly into saved templates ("favorites")
that appear as one-tap cards to pre-fill a new entry, so common meals don't have
to be retyped.

## ADDED Requirements

### Requirement: Saving a favorite from the entry form

The meal-entry form SHALL offer a "save to favorites" option. When an entry is
saved with that option enabled, the system SHALL persist a favorite for the
acting user holding the entry's name, its portion (named size XOR grams, carried
over exactly), its meal group (if any) and its tags. The favorite SHALL NOT carry
the eaten-at time or the notes. Saving the entry SHALL still succeed even if the
favorite cannot be created; the option has no effect when left disabled.

#### Scenario: Save an entry and a favorite together

- **WHEN** the user submits a valid entry named "Greek yogurt", portion `small`, meal group "breakfast", tag `quick`, with "save to favorites" enabled
- **THEN** the entry is created
- **AND** a favorite exists for that user with name "Greek yogurt", portion `small`, meal group "breakfast", tag `quick`, and no time or notes

#### Scenario: Option left off

- **WHEN** the user submits a valid entry with "save to favorites" disabled
- **THEN** the entry is created and no favorite is added

### Requirement: One favorite per name and meal group

The system SHALL identify a favorite by the acting user, its normalised name
(trimmed, case-insensitive) and its meal group. When "save to favorites" is used
and a favorite with the same identity already exists, the system SHALL update
that favorite's portion and tags in place rather than create a duplicate.

#### Scenario: Re-saving updates in place

- **WHEN** the user has a favorite "Greek yogurt" / breakfast / portion `small` and later saves an entry "greek yogurt" / breakfast / portion `medium` with "save to favorites" enabled
- **THEN** the user still has exactly one "Greek yogurt" / breakfast favorite
- **AND** its portion is now `medium`

#### Scenario: Different meal group is a different favorite

- **WHEN** the user saves favorites "Toast" / breakfast and "Toast" / snack
- **THEN** both favorites exist independently

### Requirement: Quick-add cards grouped by meal group

The home view SHALL render the acting user's favorites as cards grouped by meal
group, each group shown under its localized meal-group label and icon, in the
meal-group catalog's display order, with favorites having no meal group shown in
their own group. When the user has no favorites, no card area is shown.

#### Scenario: Grouped display

- **WHEN** the user has favorites in "breakfast" and "lunch"
- **THEN** the home shows a "breakfast" group and a "lunch" group, each listing its favorites

#### Scenario: No favorites

- **WHEN** the user has never saved a favorite
- **THEN** the home shows the entry form with no card area

### Requirement: Using a favorite to pre-fill an entry

Selecting a quick-add card SHALL populate the home entry form with that
favorite's name, portion, meal group and tags, with the eaten-at time set to the
current time. The user SHALL be able to change any field before submitting, and
submitting SHALL create an ordinary entry. Selecting a card SHALL NOT by itself
create an entry.

#### Scenario: Card pre-fills the form

- **WHEN** the user taps the "Greek yogurt" card
- **THEN** the entry form shows name "Greek yogurt", portion `small`, meal group "breakfast", tag `quick`, and eaten-at at the current time
- **AND** no entry is created until the user submits

#### Scenario: Adjust before submitting

- **WHEN** the user taps a card and changes the portion, then submits
- **THEN** the created entry has the changed portion

### Requirement: Deleting a favorite

Each quick-add card SHALL provide an inline control to delete that favorite. The
deletion SHALL be permanent and SHALL remove the card. It SHALL NOT affect any
entries already logged from that favorite.

#### Scenario: Remove a card

- **WHEN** the user activates the delete control on the "Greek yogurt" card
- **THEN** the favorite is gone and the card no longer appears
- **AND** previously logged "Greek yogurt" entries are unchanged

### Requirement: Ownership isolation

The system SHALL scope every favorite read and write to the acting user's own
favorites. A request to use or delete a favorite id that belongs to another user
or does not exist SHALL have no effect and SHALL be reported as not found.

#### Scenario: Cross-user access

- **WHEN** a user attempts to delete a favorite id they do not own
- **THEN** nothing is deleted and the system responds with "not found"
