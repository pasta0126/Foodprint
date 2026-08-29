## Purpose

Defines the shared visual language and UI primitives so every Foodprint page looks
like one product and stays accessible in light and dark modes.

## ADDED Requirements

### Requirement: Design tokens

The system SHALL define a single source of design tokens covering color (surface,
text, primary, border, success, danger), a typographic scale, spacing, and border
radius. All components SHALL consume these tokens (e.g. via CSS custom properties)
rather than hard-coded values.

#### Scenario: Token reuse

- **WHEN** a component needs a background or text color
- **THEN** it references a named token, not a raw hex value

### Requirement: Light and dark themes

The system SHALL support light and dark themes driven by the operating-system
preference, with an in-app override that persists per browser. All text/background
pairings SHALL meet WCAG AA contrast (4.5:1 for body text, 3:1 for large text) in
both themes.

#### Scenario: Follows system preference

- **WHEN** a first-time visitor has their OS set to dark mode
- **THEN** the app renders in the dark theme

#### Scenario: Manual override persists

- **WHEN** the user switches the in-app theme toggle to light and reloads
- **THEN** the app stays in the light theme

### Requirement: Core UI primitives

The system SHALL provide reusable Button, Input, Textarea, Select, Tag/Chip, Card,
and app layout-shell (header with navigation and sign-out) components. Interactive
primitives SHALL expose visible focus states and correct ARIA roles/labels, and
SHALL be operable by keyboard alone.

#### Scenario: Keyboard operation

- **WHEN** the user tabs to a Button and presses Enter or Space
- **THEN** the button activates and shows a visible focus ring

#### Scenario: Labeled inputs

- **WHEN** an Input is rendered
- **THEN** it is associated with a programmatic label

### Requirement: Responsive layout

The system SHALL render usable layouts from 320px viewport width upward, with the
primary navigation collapsing to a mobile-appropriate form below 768px and no
horizontal page scrolling.

#### Scenario: Narrow viewport

- **WHEN** the app is viewed at 320px wide
- **THEN** all content and navigation are reachable without horizontal scroll
