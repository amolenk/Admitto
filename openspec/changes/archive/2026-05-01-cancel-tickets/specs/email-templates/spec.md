## MODIFIED Requirements

### Requirement: Email templates are configurable per team and per event
The Email module SHALL persist `EmailTemplate` records scoped to either a team or a specific ticketed event. Each template SHALL carry a `Type`, a `Subject`, a `TextBody`, and an `HtmlBody`. A team SHALL have at most one template per `Type`; an event SHALL have at most one template per `Type`.

The supported `Type` values SHALL be: `ticket` (registration confirmation), `cancellation` (attendee-request cancellation), `visa-letter-denied` (visa denial cancellation), `ticket-types-removed` (system/admin cancellation due to removed ticket types), `reconfirm` (reconfirm-attendance prompt), and `bulk-custom` (ad-hoc bulk email content; cannot be stored as a template).

#### Scenario: Create a team-scoped template
- **WHEN** an organizer creates a `ticket` template for team "acme" with subject "Welcome to {{ event_name }}", a text body, and an html body
- **THEN** an `EmailTemplate` is persisted in the `email` schema with scope=team, scopeId=acmeTeamId, type="ticket"

#### Scenario: Create a cancellation template
- **WHEN** an organizer creates a `cancellation` template for team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=team, type="cancellation" and is used for attendee-request cancellations for any of the team's events lacking an event-scoped override

#### Scenario: Create a visa-letter-denied template
- **WHEN** an organizer creates a `visa-letter-denied` template for team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=team, type="visa-letter-denied"

#### Scenario: Create a ticket-types-removed template
- **WHEN** an organizer creates a `ticket-types-removed` template for team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=team, type="ticket-types-removed"

#### Scenario: At most one template per scope per type
- **WHEN** an organizer creates a second `ticket` template for the same event
- **THEN** the request is rejected with an "already exists" error

#### Scenario: bulk-custom type cannot be persisted as a template
- **WHEN** an organizer attempts to create or upsert a template with `type="bulk-custom"`
- **THEN** the request is rejected with a validation error
