## ADDED Requirements

### Requirement: Organizers can send a diagnostic test email via the saved settings of either scope

The Email module SHALL expose admin HTTP endpoints that send a diagnostic email using the saved settings of a single scope, so organizers can verify SMTP credentials before relying on them for real sends. The endpoints SHALL exist at both team scope (`POST /admin/teams/{teamSlug}/email-settings/test`) and event scope (`POST /admin/teams/{teamSlug}/events/{eventSlug}/email-settings/test`), and SHALL share the same `(Scope, ScopeId)`-parameterised slice family used by the read/upsert/delete endpoints.

The request body SHALL carry a single `recipient` field (an email address). The endpoint SHALL load only the email-settings row matching the requested scope and SHALL NOT fall back to the team scope when the event-scope row is missing — the test reflects exactly the settings that the requested scope would use today. The diagnostic send SHALL be performed synchronously through `IEmailSender` (no outbox, no Quartz job) so the caller receives the success or failure result on the same HTTP response. The send SHALL NOT write any row to `email_log`, because the diagnostic is not real correspondence.

The endpoint SHALL be authorized via team membership on the team that owns the scope, with the same `Organizer` role required by the sibling settings endpoints.

#### Scenario: Diagnostic send succeeds at team scope
- **GIVEN** team "acme" has saved valid team-scoped email settings AND an organizer is a member of "acme"
- **WHEN** the organizer issues `POST /admin/teams/acme/email-settings/test` with body `{"recipient": "ops@acme.org"}`
- **THEN** the Email module reads the team-scoped settings row, decrypts the SMTP password, and asks `IEmailSender` to send a fixed-content diagnostic message to "ops@acme.org" using those settings
- **AND** the response is `200 OK`
- **AND** no row is written to `email_log`

#### Scenario: Diagnostic send succeeds at event scope without consulting the team scope
- **GIVEN** team "acme" has both team-scoped and event-scoped email settings AND event "devconf-2026" (owned by "acme") has its own event-scoped row
- **WHEN** an organizer issues `POST /admin/teams/acme/events/devconf-2026/email-settings/test` with a valid recipient
- **THEN** the diagnostic message is sent using the event-scoped settings
- **AND** the team-scoped settings are not read

#### Scenario: Diagnostic send fails at event scope when no event-scoped row exists
- **GIVEN** team "acme" has team-scoped settings AND event "devconf-2026" has no event-scoped row
- **WHEN** an organizer issues `POST /admin/teams/acme/events/devconf-2026/email-settings/test` with a valid recipient
- **THEN** the request is rejected with a business-rule error stating that email settings have not been configured for this scope
- **AND** the team-scoped row is not used as a fallback

#### Scenario: Settings are present but invalid
- **GIVEN** the saved settings for the requested scope have `AuthMode=Basic` but no stored password
- **WHEN** an organizer issues a test request for that scope with a valid recipient
- **THEN** the request is rejected with a business-rule error indicating that the saved settings are incomplete
- **AND** no SMTP connection is attempted

#### Scenario: SMTP transport fails
- **GIVEN** the saved settings for the requested scope have a wrong password
- **WHEN** an organizer issues a test request for that scope with a valid recipient
- **THEN** the SMTP authentication failure is wrapped into a business-rule error whose message includes the underlying transport error
- **AND** the response status indicates a client-visible failure
- **AND** no row is written to `email_log`

#### Scenario: Recipient validation
- **WHEN** an organizer issues a test request with a `recipient` field that is missing or not a syntactically valid email address
- **THEN** the request is rejected by the endpoint validator with a `400 Bad Request`
- **AND** no settings row is loaded and no SMTP connection is attempted

#### Scenario: Authorization
- **WHEN** a user who is not an Organizer (or higher) of the team that owns the scope issues a test request at either scope
- **THEN** the request is denied with a `403` response
- **AND** no diagnostic email is sent
