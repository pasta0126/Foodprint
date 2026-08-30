## MODIFIED Requirements

### Requirement: Core UI primitives

The system SHALL provide reusable Button, Input, Textarea, Select, Tag/Chip, Card,
Icon, and app layout-shell components. The app layout-shell is a header with the
primary navigation and a navigation identity control (see "Navigation identity
control"); it SHALL NOT contain the language switcher, the theme toggle, or a
sign-out control — those live on the profile page (see `user-profile`).
Interactive primitives SHALL expose visible focus states and correct ARIA
roles/labels, and SHALL be operable by keyboard alone.

#### Scenario: Keyboard operation

- **WHEN** the user tabs to a Button and presses Enter or Space
- **THEN** the button activates and shows a visible focus ring

#### Scenario: Labeled inputs

- **WHEN** an Input is rendered
- **THEN** it is associated with a programmatic label

#### Scenario: Header has no scattered account chrome

- **WHEN** the app layout-shell header is rendered for a signed-in user
- **THEN** it shows primary navigation and the identity control only
- **AND** language, theme, and sign-out controls are not present in the header

### Requirement: Responsive layout

The system SHALL render usable layouts from 320px viewport width upward, with the
primary navigation and the navigation identity control collapsing to a
mobile-appropriate form below 768px and no horizontal page scrolling. Forms and
cards SHALL reflow to a single column on narrow viewports, and tap targets SHALL
be at least 44px in the smallest dimension.

#### Scenario: Narrow viewport

- **WHEN** the app is viewed at 320px wide
- **THEN** all content, the navigation, and the identity control are reachable without horizontal scroll

#### Scenario: Meal form on a phone

- **WHEN** the meal-entry form is viewed at 360px wide
- **THEN** its fields stack in a single column with no clipped or overlapping controls

## ADDED Requirements

### Requirement: Navigation identity control

The app layout-shell SHALL present the signed-in user's identity as a control on
the trailing edge of the header: a circular avatar showing the first letter of
the user's display name (or, if absent, their email), on a background color
deterministically derived from a stable user identifier so the same user always
gets the same color, with text/background contrast meeting WCAG AA. The user's
display name or email SHALL be shown alongside the avatar on viewports wide enough
to fit it. Activating the control SHALL reveal a menu with at least "Profile"
(navigates to the profile page) and "Sign out" (performs sign-out). The menu
SHALL be operable by keyboard and SHALL NOT require an interactive server circuit.

#### Scenario: Deterministic avatar

- **WHEN** the same user loads the app on two occasions
- **THEN** the avatar shows the same initial and the same background color both times

#### Scenario: Opening the menu

- **WHEN** the user activates the identity control
- **THEN** a menu with "Profile" and "Sign out" appears

#### Scenario: Menu to profile

- **WHEN** the user selects "Profile" from the identity menu
- **THEN** the app navigates to the profile page

#### Scenario: Keyboard access

- **WHEN** the user focuses the identity control with the keyboard and activates it, then tabs
- **THEN** focus moves through the menu items and each shows a visible focus ring

### Requirement: Icon set

The system SHALL provide a small set of inline-SVG icons used consistently across
the app for navigation destinations, entry actions (add, edit, delete, save),
meal groups, and portion sizes. Icons SHALL render without any client-side script
dependency. A decorative icon accompanying a visible text label SHALL be hidden
from assistive technology; an icon that is the only content of a control SHALL
have an accessible name.

#### Scenario: Icon with a text label

- **WHEN** a button shows an icon next to its text label
- **THEN** the icon is marked decorative (e.g. aria-hidden) and the control's name comes from the text

#### Scenario: Icon-only control

- **WHEN** a control's only visible content is an icon
- **THEN** the control exposes an accessible name describing its action

#### Scenario: No script needed

- **WHEN** a page renders with JavaScript disabled
- **THEN** all icons still display
