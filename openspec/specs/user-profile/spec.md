# user-profile Specification

## Purpose

Holds the per-user settings the rest of the app depends on — display name, time
zone, and interface language — with sensible defaults and validation.

## Requirements

### Requirement: Profile fields and defaults

Every user SHALL have a profile with a display name (1–80 characters), an IANA
time-zone id, and a UI language (one of `ca`, `es`, `en`). On account creation the
display name SHALL come from the account-activation form, the time zone SHALL
default to `Europe/Madrid`, and the language SHALL default to the language resolved
for the activation request (see `localization`), falling back to `es`.

#### Scenario: Defaults on creation

- **WHEN** a user account is activated with the display name "Alex"
- **THEN** the profile has display name "Alex", time zone `Europe/Madrid`, and a language of `ca`, `es`, or `en`

### Requirement: Editing the profile

An authenticated user SHALL be able to view and change their own display name,
time zone, and language. Invalid values (empty name, unknown time-zone id,
unsupported language) SHALL be rejected with a validation message and no change
SHALL be saved.

#### Scenario: Change time zone

- **WHEN** the user sets their time zone to `America/New_York` and saves
- **THEN** the profile persists the new zone
- **AND** day, history, and weekly-summary views immediately bucket by it

#### Scenario: Invalid time zone

- **WHEN** the user submits a time-zone id that is not a known IANA zone
- **THEN** the system rejects the change and the stored profile is unchanged

#### Scenario: Change language

- **WHEN** the user switches their language to `en` and saves
- **THEN** the profile persists `en`
- **AND** subsequent pages render in English

### Requirement: Time zone is the bucketing authority

All calendar-day grouping in the app (day view, history, weekly summary) SHALL use
the acting user's profile time zone. Meal timestamps SHALL continue to be stored
from the server clock in UTC regardless of profile.

#### Scenario: Entry near midnight

- **WHEN** a user in `Pacific/Auckland` logs an entry at a UTC instant that is 11pm local
- **THEN** the entry is grouped into that local calendar day, not the UTC day

### Requirement: Profile page is the account hub

The profile page SHALL be the single place where a signed-in user manages their
account: in addition to display name, time zone, and language, it SHALL let the
user set their theme preference (system / light / dark), export their meal log
(see `data-export`), and sign out. The theme preference SHALL take effect
immediately and persist per the design-system theme rules. Sign-out from the
profile page SHALL end the current session and return the user to the signed-out
entry point.

#### Scenario: Change theme from the profile page

- **WHEN** the user selects the "dark" theme option on the profile page
- **THEN** the app switches to the dark theme
- **AND** the choice persists on reload per the design-system theme rules

#### Scenario: Export from the profile page

- **WHEN** the user picks a date range in the export zone and activates download
- **THEN** the browser downloads their meal-log Markdown file for that range

#### Scenario: Sign out from the profile page

- **WHEN** the user activates "Sign out" on the profile page
- **THEN** the session ends and the user lands on the signed-out entry point

#### Scenario: One account hub

- **WHEN** a signed-in user looks for where to change language, theme, export their data, or sign out
- **THEN** all of them are on the profile page, reachable from the header identity control
