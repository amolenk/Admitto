## ADDED Requirements

### Requirement: Email templates are configurable per team and per event
The Email module SHALL persist `EmailTemplate` records scoped to either a team or a specific ticketed event. Each template SHALL carry a `Type` (e.g. `ticket`), a `Subject`, a `TextBody`, and an `HtmlBody`. A team SHALL have at most one template per `Type`; an event SHALL have at most one template per `Type`.

#### Scenario: Create a team-scoped template
- **WHEN** an organizer creates a `ticket` template for team "acme" with subject "Welcome to {{ event_name }}", a text body, and an html body
- **THEN** an `EmailTemplate` is persisted in the `email` schema with scope=team, scopeId=acmeTeamId, type="ticket"

#### Scenario: Create an event-scoped template
- **WHEN** an organizer creates a `ticket` template for event "devconf-2026" on team "acme"
- **THEN** an `EmailTemplate` is persisted with scope=event, scopeId=devconfEventId, type="ticket"

#### Scenario: At most one template per scope per type
- **WHEN** an organizer creates a second `ticket` template for the same event
- **THEN** the request is rejected with an "already exists" error

---

### Requirement: Template lookup precedence is event > team > built-in default
When the Email module needs a template of type `T` for event `E` on team `Team(E)`, it SHALL resolve in this order: (1) the event-scoped template for `(scope=event, scopeId=E, type=T)` if present; otherwise (2) the team-scoped template for `(scope=team, scopeId=Team(E), type=T)` if present; otherwise (3) the built-in default template for type `T` shipped as an embedded resource. The lookup SHALL be a single resolution pass — no field-level merging across scopes.

#### Scenario: Event-scoped template wins over team-scoped
- **WHEN** both a team-scoped `ticket` template and an event-scoped `ticket` template exist for the event in question
- **THEN** the event-scoped template's subject, text body, and html body are used

#### Scenario: Team-scoped template used when no event-scoped exists
- **WHEN** no event-scoped `ticket` template exists but the owning team has one
- **THEN** the team-scoped template is used

#### Scenario: Built-in default used when neither exists
- **WHEN** neither an event-scoped nor a team-scoped `ticket` template exists
- **THEN** the built-in default `ticket` template (embedded resource) is used

#### Scenario: Unknown template type
- **WHEN** the system requests a template of type `unknown-type` and no team- or event-scoped template exists
- **THEN** template resolution fails with a "template not supported" error rather than silently substituting another type

---

### Requirement: Template rendering uses Scriban with parameters from the triggering event
Templates SHALL be rendered with the Scriban templating engine. The renderer SHALL import the triggering event's parameter object as Scriban global variables (e.g. `{{ event_name }}`, `{{ first_name }}`, `{{ register_link }}`). Rendering SHALL produce three strings: rendered subject, rendered text body, rendered html body.

#### Scenario: Variables are substituted
- **WHEN** a template subject is "Your {{ event_name }} Ticket" and the parameters provide `event_name = "DevConf"`
- **THEN** the rendered subject is "Your DevConf Ticket"

#### Scenario: Parse error surfaces as a render failure
- **WHEN** a template body contains an unparseable Scriban expression
- **THEN** rendering throws a deterministic error that callers can catch and record (see `email-sending`)

---

### Requirement: Admin endpoints manage team-scoped and event-scoped templates
The Email module SHALL expose admin HTTP endpoints to read, upsert, and delete templates at both team and event scope. Endpoints SHALL be authorized via membership on the team that owns the scope. Updates SHALL accept the current `Version` for optimistic concurrency.

#### Scenario: Upsert team-scoped template
- **WHEN** an organizer of team "acme" sends an upsert for a `ticket` team-scoped template with subject "Welcome"
- **THEN** the template is created or updated, and a subsequent GET returns the new content

#### Scenario: Delete event-scoped template falls back to team or default
- **WHEN** an organizer deletes the event-scoped `ticket` template for event "devconf-2026" while a team-scoped one still exists
- **THEN** template resolution for event "devconf-2026" subsequently returns the team-scoped template

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the owning team attempts to upsert a template
- **THEN** the request is denied with a 403 response
