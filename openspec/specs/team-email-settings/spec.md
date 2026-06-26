# team-email-settings Specification

## Purpose

Team email settings are the only SMTP settings source for event-owned application emails. They are represented by the unified Email module `EmailSettings` aggregate and include simple branding values used by built-in email templates.

## Requirements

### Requirement: Team scope is supported by the unified EmailSettings aggregate

The Email module's `EmailSettings` aggregate SHALL support team-level rows with `TeamId` set. The same fields (SMTP host, port, from-address, authentication mode, credentials), the same `Version` optimistic concurrency token, the same branding fields, and the same `email` schema apply to all email settings. There SHALL be no event-scoped settings row and no separate `TeamEmailSettings` table or aggregate type.

#### Scenario: Team-scoped row stored in the email settings table

- **WHEN** an organizer creates email settings for team "acme"
- **THEN** the row is persisted in `email.email_settings` with `team_id` referencing the "acme" team id and no event scope

#### Scenario: At most one team-scoped row per team

- **WHEN** an organizer attempts to create a second team-scoped settings row for the same team
- **THEN** the request is rejected with an "already exists" error (enforced by the team-scope unique index)

---

### Requirement: Team-scoped settings act as the fallback in effective-settings resolution

Team-scoped settings SHALL be the only email settings used for event-owned application emails. When effective email settings are resolved for an event, the team-scoped row for the event's owning team SHALL be used. There SHALL be no event-scoped override and no per-field merging across scopes.

#### Scenario: Team settings used for event email

- **GIVEN** team "acme" has valid team-scoped settings AND event "devconf-2026" is owned by "acme"
- **WHEN** the send-email command handler resolves effective settings for "devconf-2026"
- **THEN** the team-scoped row is returned

#### Scenario: No event override exists

- **GIVEN** team "acme" has valid team-scoped settings
- **WHEN** the send-email command handler resolves effective settings for any event owned by "acme"
- **THEN** no event-scoped settings row is consulted

#### Scenario: Deleting team settings disables event email for the team

- **GIVEN** team "acme" has team-scoped settings
- **WHEN** the team-scoped row is deleted
- **THEN** email configuration checks for events owned by "acme" return not configured

---

### Requirement: Team-scoped admin endpoints share the EmailSettings slice family

Admin endpoints for settings SHALL be exposed under the team admin route family (`/admin/teams/{teamSlug}/email-settings`) only. Authorization SHALL require team membership on the team identified by `{teamSlug}`. The masking, optional-secret-preservation, branding, and optimistic-concurrency behavior described in `email-settings` SHALL apply to team-scoped requests.

#### Scenario: Create team-scoped settings via team admin endpoint

- **WHEN** an authenticated team member POSTs or PUTs valid settings to `/admin/teams/acme/email-settings`
- **THEN** a row for the "acme" team is created or updated and the response masks the password

#### Scenario: Non-member rejected on team admin endpoint

- **WHEN** a user who is not a member of team "acme" calls any team-scoped settings admin endpoint for "acme"
- **THEN** the request is denied with a 403 response

#### Scenario: Event-scoped endpoints removed

- **WHEN** code search inspects the Email settings endpoints
- **THEN** there are no `/admin/teams/{teamSlug}/events/{eventSlug}/email-settings` endpoints
