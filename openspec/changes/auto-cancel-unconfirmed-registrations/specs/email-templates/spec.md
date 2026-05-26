## MODIFIED Requirements

### Requirement: Email templates are configurable per team and per event

The Email module SHALL persist `EmailTemplate` records scoped to either a team or a specific ticketed event. Each template SHALL carry a `Type`, a `Subject`, a `TextBody`, and an `HtmlBody`. A team SHALL have at most one template per `Type`; an event SHALL have at most one template per `Type`.

The supported `Type` values SHALL be: `ticket` (single registration confirmation), `cancellation` (attendee-request cancellation), `visa-letter-denied` (visa denial cancellation), `ticket-types-removed` (system/admin cancellation due to removed ticket types), `reconfirm` (recurring reconfirm-attendance prompt), `reconfirm-cancelled` (notification sent when a registration is auto-cancelled after exhausting reconfirm attempts), and `bulk-custom` (catch-all type used when ad-hoc subject/body fully overrides the resolved template; see `bulk-email` capability).

#### Scenario: Create a team-scoped template

- **WHEN** an organizer creates a `ticket` template for team "acme" with subject "Welcome to {{ event_name }}", a text body, and an html body
- **THEN** an `EmailTemplate` is persisted in the `email` schema with scope=team, scopeId=acmeTeamId, type="ticket"

#### Scenario: Create an event-scoped template

- **WHEN** an organizer creates a `ticket` template for event "devconf-2026" on team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=event, scopeId=devconfEventId, type="ticket"

#### Scenario: At most one template per scope per type

- **WHEN** an organizer creates a second `ticket` template for the same event
- **THEN** the request is rejected with an "already exists" error

#### Scenario: Create a reconfirm-cancelled template

- **WHEN** an organizer creates a `reconfirm-cancelled` template for team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=team, type="reconfirm-cancelled" and is used as the auto-cancel notification for any of the team's events lacking an event-scoped override

#### Scenario: bulk-custom type cannot be persisted as a template

- **WHEN** an organizer attempts to create or upsert a template with `type="bulk-custom"`
- **THEN** the request is rejected with a validation error stating that `bulk-custom` is reserved for ad-hoc bulk-email content carried on the job and not for stored templates
