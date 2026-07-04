## ADDED Requirements

### Requirement: Admin endpoint returns the resolved (effective) template for preview
The Email module SHALL expose a `GET /admin/teams/{teamSlug}/email-templates/{type}/preview` endpoint (and its event-scoped equivalent at `/admin/teams/{teamSlug}/events/{eventSlug}/email-templates/{type}/preview`) that resolves the effective template for the given type and scope using the precedence rules defined in the main `email-templates` spec, then renders it with a fixed set of sample placeholder values (see rendering spec). The response SHALL return the rendered subject, text body, and HTML body as a JSON object. The endpoint SHALL NOT send any email.

#### Scenario: Preview team-scoped custom template
- **WHEN** an organizer requests a preview of the `ticket` template for team "acme" and a custom team-scoped template exists
- **THEN** the response contains the rendered subject, text body, and HTML body from that custom template with sample variables substituted

#### Scenario: Preview falls back to built-in default when no custom template exists
- **WHEN** an organizer requests a preview of the `ticket` template for team "acme" and no custom team-scoped template exists
- **THEN** the response contains the rendered subject, text body, and HTML body from the built-in default `ticket` template with sample variables substituted

#### Scenario: Preview event-scoped template wins over team-scoped
- **WHEN** an organizer requests a preview of the `ticket` template for event "devconf-2026" on team "acme" and both an event-scoped and a team-scoped custom template exist
- **THEN** the response contains the rendered output from the event-scoped template

#### Scenario: Unknown template type returns 400
- **WHEN** an organizer requests a preview for type `unknown-type`
- **THEN** the endpoint returns a 400 error indicating the template type is not supported

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the owning team requests a preview
- **THEN** the endpoint returns 403

---

### Requirement: Admin endpoint sends a rendered test email for a template type
The Email module SHALL expose a `POST /admin/teams/{teamSlug}/email-templates/{type}/test-send` endpoint (and its event-scoped equivalent) that resolves the effective template for the given type, renders it with sample placeholder values, and dispatches the rendered email to a caller-supplied recipient address using the email settings resolved for the scope. The endpoint SHALL return 200 on success. If no email settings are configured for the scope, the endpoint SHALL return a business-rule error.

#### Scenario: Send test email using team-scoped effective template
- **WHEN** an organizer posts `{ "recipient": "bob@example.com" }` to the team-scoped test-send endpoint for the `ticket` type and the team has email settings configured
- **THEN** one email is sent to "bob@example.com" with the rendered content of the resolved `ticket` template

#### Scenario: Test send fails when no email settings configured
- **WHEN** an organizer posts to the test-send endpoint and the team has no email settings configured
- **THEN** the endpoint returns a 422 error with code `email_settings.not_configured`

#### Scenario: Non-team-member denied test send
- **WHEN** a user who is not a member of the owning team attempts a test send
- **THEN** the endpoint returns 403
