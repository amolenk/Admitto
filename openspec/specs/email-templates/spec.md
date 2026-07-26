# email-templates Specification

## Purpose

The Email module renders transactional and custom bulk email content with Scriban. Transactional templates are code-owned built-in resources themed with team accent color; organizers do not manage persisted transactional template overrides.

## Requirements

### Requirement: Transactional email templates are built-in and themed

The Email module SHALL render transactional emails from code-owned built-in templates. Built-in transactional templates SHALL NOT be persisted as `EmailTemplate` rows and SHALL NOT be editable by organizers. Rendering SHALL apply the owning team's accent color, falling back to the system default accent color when the team has no explicit value.

#### Scenario: Confirmation email uses built-in content and team branding

- **WHEN** an attendee registers for event "DevConf" owned by team "acme"
- **THEN** the confirmation email is rendered from the built-in `ticket` content with team "acme" accent color applied

#### Scenario: Organizer cannot edit transactional copy

- **WHEN** an organizer opens event email pages in the Admin UI
- **THEN** no transactional template subject/body editor is available

#### Scenario: Accent color is applied to transactional HTML

- **WHEN** team "acme" has accent color `#0f766e`
- **THEN** the built-in transactional HTML uses `#0f766e` for accent-colored template elements

---

### Requirement: Template rendering uses Scriban with parameters from the triggering event

Built-in transactional templates and custom bulk email job content SHALL be rendered with the Scriban templating engine. The renderer SHALL import the triggering event's parameter object as Scriban global variables (e.g. `{{ event_name }}`, `{{ first_name }}`, `{{ register_link }}`). Rendering SHALL produce three strings: rendered subject, rendered text body, and rendered html body.

The canonical branding parameter SHALL be `accent_color`, supplied once by the send pipeline from the resolved effective email settings. Template parameter objects assembled by transactional event handlers SHALL NOT carry their own accent color, and no duplicate `team_accent_color` alias SHALL be exposed. The `font_family` parameter SHALL likewise be supplied by the send pipeline from a fixed system constant, not from team or event data. Registration QR-code links SHALL use the canonical `qrcode_link` parameter for both transactional and bulk rendering.

Custom bulk email jobs SHALL supply complete job-owned `Subject`, `TextBody`, and `HtmlBody`; those fields SHALL be rendered through Scriban with the same recipient/event parameter set. Transactional email callers SHALL render code-owned built-in templates only.

The `ticket` built-in template SHALL receive a `ticket_types` parameter containing the list of ticket type names the attendee is registered for. This parameter SHALL be supplied by both the initial-registration email handler (`AttendeeRegisteredIntegrationEventHandler`) and the ticket-change email handler (`AttendeeTicketsChangedIntegrationEventHandler`). The built-in default `ticket` templates (HTML and text) SHALL display the ticket type list.

#### Scenario: Variables are substituted

- **WHEN** a built-in template subject is "Your {{ event_name }} Ticket" and the parameters provide `event_name = "DevConf"`
- **THEN** the rendered subject is "Your DevConf Ticket"

#### Scenario: Parse error surfaces as a render failure

- **WHEN** built-in template content or custom bulk job content contains an unparseable Scriban expression
- **THEN** rendering throws a deterministic error that callers can catch and record (see `email-sending`)

#### Scenario: Custom bulk fields are rendered from job content

- **WHEN** a custom bulk job carries `Subject="Schedule update for {{ event_name }}"`, `TextBody`, and `HtmlBody`
- **THEN** the rendered email uses those job-owned fields and does not load a stored template

#### Scenario: ticket_types variable lists registered ticket names in confirmation email

- **GIVEN** a registration for "alice@example.com" holding ticket types "Early Bird" and "Workshop"
- **WHEN** a `ticket` confirmation email is rendered for either initial registration or ticket change
- **THEN** the rendered output contains "Early Bird" and "Workshop"

#### Scenario: ticket_types is empty list when no catalog exists

- **GIVEN** a registration created via a coupon for an event with no ticket catalog and no ticket type snapshots
- **WHEN** the `ticket` confirmation email is rendered
- **THEN** rendering succeeds and the `{{ ticket_types }}` block renders as empty or is hidden by the template guard
