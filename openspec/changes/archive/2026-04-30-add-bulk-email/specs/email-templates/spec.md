## MODIFIED Requirements

### Requirement: Email templates are configurable per team and per event

The Email module SHALL persist `EmailTemplate` records scoped to either a team or a specific ticketed event. Each template SHALL carry a `Type`, a `Subject`, a `TextBody`, and an `HtmlBody`. A team SHALL have at most one template per `Type`; an event SHALL have at most one template per `Type`.

The supported `Type` values SHALL be: `ticket` (single registration confirmation), `cancellation` (single cancellation confirmation), `reconfirm` (recurring reconfirm-attendance prompt), and `bulk-custom` (catch-all type used when ad-hoc subject/body fully overrides the resolved template; see `bulk-email` capability).

#### Scenario: Create a team-scoped template
- **WHEN** an organizer creates a `ticket` template for team "acme" with subject "Welcome to {{ event_name }}", a text body, and an html body
- **THEN** an `EmailTemplate` is persisted in the `email` schema with scope=team, scopeId=acmeTeamId, type="ticket"

#### Scenario: Create an event-scoped template
- **WHEN** an organizer creates a `ticket` template for event "devconf-2026" on team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=event, scopeId=devconfEventId, type="ticket"

#### Scenario: At most one template per scope per type
- **WHEN** an organizer creates a second `ticket` template for the same event
- **THEN** the request is rejected with an "already exists" error

#### Scenario: Create a reconfirm template
- **WHEN** an organizer creates a `reconfirm` team-scoped template for team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=team, type="reconfirm" and is used by the reconfirm scheduler for any of the team's events lacking an event-scoped override

#### Scenario: bulk-custom type cannot be persisted as a template
- **WHEN** an organizer attempts to create or upsert a template with `type="bulk-custom"`
- **THEN** the request is rejected with a validation error stating that `bulk-custom` is reserved for ad-hoc bulk-email content carried on the job and not for stored templates

---

### Requirement: Template rendering uses Scriban with parameters from the triggering event

Templates SHALL be rendered with the Scriban templating engine. The renderer SHALL import the triggering event's parameter object as Scriban global variables (e.g. `{{ event_name }}`, `{{ first_name }}`, `{{ register_link }}`). Rendering SHALL produce three strings: rendered subject, rendered text body, rendered html body.

When the calling code (the bulk-email composer) supplies an ad-hoc `Subject`, `TextBody`, or `HtmlBody`, the renderer SHALL use those strings instead of the corresponding template field. Ad-hoc strings SHALL be rendered through Scriban with the same parameter set as the resolved template would have been.

#### Scenario: Variables are substituted
- **WHEN** a template subject is "Your {{ event_name }} Ticket" and the parameters provide `event_name = "DevConf"`
- **THEN** the rendered subject is "Your DevConf Ticket"

#### Scenario: Parse error surfaces as a render failure
- **WHEN** a template body contains an unparseable Scriban expression
- **THEN** rendering throws a deterministic error that callers can catch and record (see `email-sending`)

#### Scenario: Ad-hoc subject overrides template subject
- **WHEN** the composer is given an ad-hoc subject "Schedule update for {{ event_name }}" and a resolved template whose subject is "Your {{ event_name }} Ticket"
- **THEN** the rendered subject is "Schedule update for DevConf" — the ad-hoc string was rendered, the template subject was ignored

#### Scenario: Partial ad-hoc override falls back to template for missing fields
- **WHEN** the composer supplies only an ad-hoc subject, and the resolved template provides text+html bodies
- **THEN** the rendered text body and html body come from the template, rendered with the same parameters
