## ADDED Requirements

### Requirement: Team scope is supported by the unified EmailSettings aggregate
The Email module's `EmailSettings` aggregate (defined in `email-settings`) SHALL accept `Scope=Team` rows whose `ScopeId` is a `TeamId`. The same fields (SMTP host, port, from-address, authentication mode, credentials), the same `Version` optimistic concurrency token, and the same `email` schema apply to team-scoped rows as to event-scoped rows. There SHALL be no separate `TeamEmailSettings` table or aggregate type.

#### Scenario: Team-scoped row stored in the unified table
- **WHEN** an organizer creates email settings for team "acme"
- **THEN** the row is persisted in `email.email_settings` with `scope='team'` and `scope_id` referencing the "acme" team id, alongside any event-scoped rows in the same table

#### Scenario: At most one team-scoped row per team
- **WHEN** an organizer attempts to create a second team-scoped settings row for the same team
- **THEN** the request is rejected with an "already exists" error (enforced by the `(scope, scope_id)` unique index)

---

### Requirement: Team-scoped settings act as the fallback in effective-settings resolution
When effective email settings are resolved for an event (per `email-settings`), the team-scoped row for the event's owning team SHALL be used if and only if no event-scoped row exists for the event. There SHALL be no per-field merging across scopes — when an event-scoped row exists, the team-scoped row is ignored entirely, even if the event-scoped row is invalid.

#### Scenario: Team settings used when event has none
- **GIVEN** team "acme" has valid team-scoped settings AND event "devconf-2026" (owned by "acme") has no event-scoped row
- **WHEN** the send-email command handler resolves effective settings for "devconf-2026"
- **THEN** the team-scoped row is returned

#### Scenario: Team settings ignored when event has its own
- **GIVEN** team "acme" has valid team-scoped settings AND event "devconf-2026" (owned by "acme") has its own event-scoped row
- **WHEN** the send-email command handler resolves effective settings for "devconf-2026"
- **THEN** the event-scoped row is returned and the team-scoped row is not consulted

#### Scenario: Deleting team settings does not affect event-scoped rows
- **GIVEN** team "acme" has team-scoped settings AND event "devconf-2026" (owned by "acme") has its own event-scoped row
- **WHEN** the team-scoped row is deleted
- **THEN** event-scoped resolution for "devconf-2026" continues to succeed using the event-scoped row

---

### Requirement: Team-scoped admin endpoints share the EmailSettings slice family
Admin endpoints for team-scoped settings SHALL be exposed under the team admin route family (mirroring the existing `EmailTemplate` admin pattern, e.g. `/admin/teams/{teamSlug}/email-settings`) and SHALL share their command/handler implementation with the event-scoped admin endpoints via a single `(Scope, ScopeId)`-parameterised slice family. Authorization SHALL require team membership on the team identified by `{teamSlug}`. The masking, optional-secret-preservation, and optimistic-concurrency behavior described in `email-settings` SHALL apply equally to team-scoped requests.

#### Scenario: Create team-scoped settings via team admin endpoint
- **WHEN** an authenticated team member POSTs valid settings to `/admin/teams/acme/email-settings`
- **THEN** a row with `(scope='team', scope_id=<acme team id>)` is created and the response masks the password

#### Scenario: Non-member rejected on team admin endpoint
- **WHEN** a user who is not a member of team "acme" calls any team-scoped settings admin endpoint for "acme"
- **THEN** the request is denied with a 403 response

#### Scenario: One slice family handles both scopes
- **WHEN** code search inspects the create/update/delete handlers for email settings
- **THEN** the same handler types serve both the team-scoped and event-scoped admin endpoints, parameterised by `(Scope, ScopeId)`
