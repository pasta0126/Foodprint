## Purpose

Makes the whole interface available in Catalan, Spanish and English, and formats
dates and numbers according to the active locale.

## ADDED Requirements

### Requirement: Supported languages

The system SHALL support exactly three UI languages from the MVP: Catalan (`ca`),
Spanish (`es`), and English (`en`). Spanish SHALL be the ultimate fallback.

#### Scenario: Complete coverage

- **WHEN** any user-facing screen is rendered in any supported language
- **THEN** every visible string is translated, with no raw keys or placeholders shown

### Requirement: Language resolution

The system SHALL choose the active language per request in this order: the
authenticated user's profile language; otherwise the best match from the request's
`Accept-Language` header among the supported languages; otherwise `es`.

#### Scenario: Anonymous visitor with a header

- **WHEN** an unauthenticated visitor with `Accept-Language: ca` opens the sign-in page
- **THEN** the page renders in Catalan

#### Scenario: Authenticated user overrides header

- **WHEN** a user whose profile language is `en` requests a page with `Accept-Language: ca`
- **THEN** the page renders in English

#### Scenario: Unsupported preference

- **WHEN** a visitor sends `Accept-Language: fr`
- **THEN** the page renders in Spanish

### Requirement: Switching language

The system SHALL let an authenticated user switch language from the UI, and the
choice SHALL persist to their profile (see `user-profile`).

#### Scenario: Switch persists across sessions

- **WHEN** a user switches to Catalan and later signs in again
- **THEN** the app is still in Catalan

### Requirement: Locale-aware formatting

The system SHALL format dates, times, weekday names, and numbers using the active
language's locale conventions.

#### Scenario: Date formatting differs by locale

- **WHEN** the same entry date is shown to an `en` user and an `es` user
- **THEN** each sees the date formatted per their locale's conventions
