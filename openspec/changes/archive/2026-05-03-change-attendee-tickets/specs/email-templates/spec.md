## MODIFIED Requirements

### Requirement: Template rendering uses Scriban with parameters from the triggering event

Templates SHALL be rendered with the Scriban templating engine. The renderer SHALL import the triggering event's parameter object as Scriban global variables (e.g. `{{ event_name }}`, `{{ first_name }}`, `{{ register_link }}`). Rendering SHALL produce three strings: rendered subject, rendered text body, rendered html body.

When the calling code (the bulk-email composer) supplies an ad-hoc `Subject`, `TextBody`, or `HtmlBody`, the renderer SHALL use those strings instead of the corresponding template field. Ad-hoc strings SHALL be rendered through Scriban with the same parameter set as the resolved template would have been.

The `ticket` template type SHALL receive a `ticket_types` parameter containing the list of ticket type names the attendee is registered for. This parameter SHALL be supplied by both the initial-registration email handler (`AttendeeRegisteredIntegrationEventHandler`) and the new ticket-change email handler (`AttendeeTicketsChangedIntegrationEventHandler`). The built-in default `ticket` templates (HTML and text) SHALL display the ticket type list. Custom `ticket` templates that omit `{{ ticket_types }}` are unaffected — Scriban silently ignores unused variables.

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

#### Scenario: ticket_types variable lists registered ticket names in confirmation email

- **GIVEN** a registration for "alice@example.com" holding ticket types "Early Bird" and "Workshop"
- **WHEN** a `ticket` confirmation email is rendered (either for initial registration or ticket change)
- **THEN** the rendered output contains "Early Bird" and "Workshop"

#### Scenario: ticket_types is empty list when no catalog exists (coupon-only registration)

- **GIVEN** a registration created via a coupon for an event with no ticket catalog (no ticket type snapshots)
- **WHEN** the `ticket` confirmation email is rendered
- **THEN** rendering succeeds; the `{{ ticket_types }}` block renders as empty or is hidden by the template guard
