## Purpose

Email + password sign-in for a small set of users, where the password is first set
by opening a one-time token link (no email is sent by the app), and one configured
account is the administrator.

## ADDED Requirements

### Requirement: Administrator account

The system SHALL treat the email in configuration (`Foodprint:AdminEmail`, e.g.
`pasta0126@gmail.com`) as the administrator. On startup the system SHALL ensure an
account exists for that email; if it has no password yet, the system SHALL make an
activation token link available to the operator (printed by the CLI / startup
logs). The administrator SHALL be able to create, list, and revoke registration
links and to manage the meal-group catalog; non-admin users SHALL NOT.

#### Scenario: Admin bootstrap

- **WHEN** the app starts for the first time with `Foodprint:AdminEmail` set
- **THEN** an admin account exists with no password
- **AND** an activation token link for it is available to the operator

#### Scenario: Admin-only actions

- **WHEN** a non-admin user attempts an admin action (create/revoke a link, edit meal groups)
- **THEN** the system refuses with "not authorized"

### Requirement: Registration links

The system SHALL let the administrator create a registration link for an email
address (via CLI, and optionally a guarded admin endpoint). When
`Foodprint:AllowSelfRegistration` is enabled, a public register form SHALL also
create one for a submitted email and show the link once on screen. Each link SHALL
carry an opaque high-entropy token (≥256 bits) stored only as a hash, SHALL be
single-use for activation, SHALL expire 30 days after creation by default, and
SHALL be revocable.

#### Scenario: Admin creates a link

- **WHEN** the administrator runs `invite create alex@example.com`
- **THEN** a registration record is stored with a hashed token and a 30-day expiry
- **AND** the full link URL is returned exactly once

#### Scenario: Self-registration disabled

- **WHEN** self-registration is disabled and someone opens the register form
- **THEN** the form is unavailable and they are told to request access from the administrator

#### Scenario: Duplicate email

- **WHEN** a registration link is requested for an email that already has an active account with a password
- **THEN** no new link is created and the response is the same neutral confirmation

### Requirement: Activating an account via a link

The system SHALL, when a valid, unexpired, unrevoked, unused link is opened, let
the person set a display name (1–80 chars) and a password (≥10 characters), then
create the account (if new) with a default profile, mark the link used, and start
an authenticated session. Opening an invalid link SHALL show a neutral "this link
is no longer valid" page and create nothing.

#### Scenario: First activation

- **WHEN** a person opens their registration link and submits a name and a valid password
- **THEN** their account and default profile are created
- **AND** the link cannot be reused
- **AND** a session is established and they land on the diary home

#### Scenario: Weak password

- **WHEN** the submitted password is shorter than 10 characters
- **THEN** activation is rejected with a validation message and nothing is saved

#### Scenario: Expired or revoked link

- **WHEN** a person opens a link that is expired, revoked, or already used
- **THEN** no account or session is created and they see the neutral "no longer valid" page

### Requirement: Password sign-in

The system SHALL authenticate a returning user by email and password, comparing
against a salted, slow password hash (e.g. Argon2id or PBKDF2). On success it SHALL
establish a session; on failure it SHALL respond with a generic "email or password
is incorrect" message that does not reveal whether the email exists.

#### Scenario: Correct credentials

- **WHEN** an activated user submits their correct email and password
- **THEN** a session is established and they reach the diary home

#### Scenario: Wrong password

- **WHEN** the password does not match
- **THEN** the system returns a generic failure message and creates no session

#### Scenario: Unknown email

- **WHEN** the email has no account
- **THEN** the system returns the same generic failure message

### Requirement: Sign-in rate limiting

The system SHALL limit authentication attempts (link redemption and password
sign-in combined) to at most 10 per IP address per 15 minutes, and password
attempts to at most 5 per email per 15 minutes, rejecting further attempts in the
window.

#### Scenario: Too many password attempts for one email

- **WHEN** a 6th password attempt is made for the same email within 15 minutes
- **THEN** the system rejects it with a "try again later" response

### Requirement: Changing and resetting a password

An authenticated user SHALL be able to change their password by supplying the
current one and a new one (≥10 characters). Because the app sends no email, a
forgotten password SHALL be recovered by the administrator issuing a fresh
registration/reset link, which lets the user set a new password.

#### Scenario: Change password

- **WHEN** a signed-in user submits the correct current password and a valid new password
- **THEN** the password hash is replaced
- **AND** the user's other sessions are invalidated

#### Scenario: Admin-assisted reset

- **WHEN** the administrator issues a reset link for a user who forgot their password
- **THEN** opening it lets that user set a new password without knowing the old one

### Requirement: Session lifetime

The system SHALL store the session in an HTTP-only, `Secure`, `SameSite=Lax`
cookie holding a token that is persisted only as a hash. A session SHALL remain
valid for 30 days of inactivity and SHALL be renewed on authenticated requests.

#### Scenario: Session cookie attributes

- **WHEN** a session is created
- **THEN** its cookie is HTTP-only, Secure, and SameSite=Lax and is not readable from JavaScript

#### Scenario: Idle expiry

- **WHEN** 30 days pass with no authenticated request on a session
- **THEN** that session is no longer accepted and the user must sign in again

### Requirement: Protected routes

The system SHALL require a valid session for every diary, entry, history, summary,
and profile route and for all write operations. Unauthenticated page requests
SHALL redirect to the sign-in page; data requests SHALL return 401.

#### Scenario: Unauthenticated access

- **WHEN** a request without a valid session hits a protected page
- **THEN** the system redirects to the sign-in page

### Requirement: Account disabling

The system SHALL let the administrator disable a user account, which SHALL
immediately invalidate that user's sessions and block further sign-in until
re-enabled.

#### Scenario: Admin disables an account

- **WHEN** the administrator disables a user
- **THEN** that user's next request is rejected and they cannot sign in

### Requirement: Sign out

The system SHALL let an authenticated user end their current session, clearing the
cookie and invalidating the server-side session.

#### Scenario: User signs out

- **WHEN** an authenticated user activates "Sign out"
- **THEN** the session is invalidated server-side and the cookie is removed
