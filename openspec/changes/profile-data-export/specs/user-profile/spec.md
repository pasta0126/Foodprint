## MODIFIED Requirements

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
