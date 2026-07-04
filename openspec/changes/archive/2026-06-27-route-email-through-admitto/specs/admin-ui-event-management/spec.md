## REMOVED Requirements

### Requirement: Open-registration action is gated by email configuration in the UI
**Reason**: Organizer-managed email configuration is removed and registration behavior is no longer gated by team/event SMTP settings.
**Migration**: Remove email-configuration checks, disabled-state hints, and links to the event Email tab from the registration UI.

### Requirement: Email tab manages event email server settings
**Reason**: Event-scoped SMTP settings are removed.
**Migration**: Remove the event Email tab or replace it with non-SMTP event/public-link settings if needed by navigation.

### Requirement: Event Email tab exposes a Send-test-email action with a recipient picker
**Reason**: Organizer SMTP diagnostic testing is removed.
**Migration**: Remove the send-test-email action, event email-settings proxy calls, and generated SDK references.

## ADDED Requirements

### Requirement: Event create and edit forms manage public slug
The Admin UI SHALL include a public slug field in event creation and event general/details editing. The field SHALL be required, SHALL use the backend slug validation rules, and SHALL surface backend duplicate-slug errors inline or as a form-level validation error.

#### Scenario: Create form submits public slug
- **WHEN** an organizer creates event "Azure Fest 2026" and enters public slug `azure-fest-2026`
- **THEN** the UI submits `publicSlug: "azure-fest-2026"` with the event creation request

#### Scenario: Edit form shows current public slug
- **WHEN** an organizer opens the event General page for an event whose public slug is `azure-fest-2026`
- **THEN** the public slug field is pre-filled with `azure-fest-2026`

#### Scenario: Duplicate slug error is shown
- **WHEN** the backend rejects a submitted public slug because it is already in use
- **THEN** the UI shows the duplicate-slug error to the organizer and does not report the save as successful

### Requirement: Admin UI can apply team accent color as a scoped visual accent
When team detail data includes an accent color, the Admin UI MAY expose it through a scoped CSS variable for selected-team UI affordances. This SHALL be limited to small accents and SHALL NOT require a full design-system retheme.

#### Scenario: Selected team accent variable is available
- **WHEN** the dashboard renders for selected team "acme" with accent color `#0f766e`
- **THEN** team-scoped UI can read a CSS variable or equivalent value containing `#0f766e`
