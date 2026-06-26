## ADDED Requirements

### Requirement: Transactional email templates are built-in and themed
The Email module SHALL render transactional emails from code-owned built-in templates. Built-in transactional templates SHALL NOT be persisted as `EmailTemplate` rows and SHALL NOT be editable by organizers. Rendering SHALL apply the owning team's email branding values: accent color and font-family string.

#### Scenario: Confirmation email uses built-in content and team branding
- **WHEN** an attendee registers for event "DevConf" owned by team "acme"
- **THEN** the confirmation email is rendered from the built-in `ticket` content with team "acme" branding values applied

#### Scenario: Organizer cannot edit transactional copy
- **WHEN** an organizer opens email settings in the Admin UI
- **THEN** no transactional template subject/body editor is available

#### Scenario: Font string is applied to transactional HTML
- **WHEN** team "acme" has selected font `Inter`
- **THEN** the built-in transactional HTML uses the configured `Inter` font-family value

## MODIFIED Requirements

### Requirement: Template rendering uses Scriban with parameters from the triggering event
Built-in transactional templates and custom bulk email job content SHALL be rendered with the Scriban templating engine. The renderer SHALL import the triggering event's parameter object as Scriban global variables (e.g. `{{ event_name }}`, `{{ first_name }}`, `{{ register_link }}`). Rendering SHALL produce three strings: rendered subject, rendered text body, and rendered html body.

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

## REMOVED Requirements

### Requirement: Email templates are configurable per team and per event
**Reason**: Transactional email copy is now code-owned and themed through simple team branding. Editable per-team/per-event transactional templates are no longer required and create avoidable complexity.
**Migration**: Remove persisted `EmailTemplate` rows and template management APIs. Existing custom transactional template content is not migrated.

#### Scenario: Configurable transactional templates removed
- **WHEN** the change is implemented
- **THEN** organizers can no longer create team-scoped or event-scoped transactional templates

### Requirement: Template lookup precedence is event > team > built-in default
**Reason**: There are no event-scoped or team-scoped transactional template overrides. Transactional email always uses built-in content.
**Migration**: Replace template lookup with built-in template selection by email type plus team branding.

#### Scenario: Built-in content is always selected for transactional email
- **WHEN** the Email module renders a transactional email type
- **THEN** it selects the built-in template for that type without checking event or team template rows

### Requirement: Admin endpoints manage team-scoped and event-scoped templates
**Reason**: Transactional templates are no longer admin-editable.
**Migration**: Remove template CRUD endpoints and generated SDK functions; remove callers from the Admin UI.

#### Scenario: Template CRUD endpoints removed
- **WHEN** an admin client attempts to use a transactional template CRUD endpoint
- **THEN** no such endpoint is available in the API contract

### Requirement: Admin endpoint returns the resolved (effective) template for preview
**Reason**: There is no editable effective template to preview outside code-owned rendering tests.
**Migration**: Remove preview endpoints and Admin UI preview panels.

#### Scenario: Template preview endpoints removed
- **WHEN** the API contract is generated
- **THEN** transactional template preview endpoints are absent

### Requirement: Admin endpoint sends a rendered test email for a template type
**Reason**: Template-specific test sending is tied to removed editable templates. SMTP diagnostics remain available through team email settings test send.
**Migration**: Remove template test-send endpoints and use the team email-settings diagnostic send for SMTP verification.

#### Scenario: Template test-send endpoints removed
- **WHEN** the API contract is generated
- **THEN** transactional template test-send endpoints are absent
