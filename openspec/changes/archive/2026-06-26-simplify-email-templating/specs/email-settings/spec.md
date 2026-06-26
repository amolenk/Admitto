## MODIFIED Requirements

### Requirement: Secret fields are encrypted at rest using ASP.NET Data Protection
The Email module SHALL encrypt secret fields (SMTP password and any future API tokens or full connection strings) on every team-scoped `EmailSettings` row using the ASP.NET Core Data Protection API before persisting them. Decryption SHALL only happen inside the Email module's infrastructure. Read APIs SHALL never expose the plaintext secret to other modules or to API responses.

#### Scenario: Password is encrypted in the database
- **WHEN** a team-scoped settings row is saved with password "p@ssw0rd"
- **THEN** the password column in `email.email_settings` does not contain the literal string "p@ssw0rd"

#### Scenario: Admin GET masks the password
- **WHEN** an organizer reads team email settings via the admin endpoint
- **THEN** the response contains metadata (host, port, from-address, auth mode, hasPassword=true) but does not include the plaintext password

#### Scenario: Cross-purpose decryption is rejected
- **WHEN** code outside the Email module attempts to decrypt the protected blob using a different Data Protection purpose string
- **THEN** decryption fails

### Requirement: Organizers can update email settings via admin endpoints
The Email module SHALL expose admin HTTP endpoints to read, create, update, and delete team-scoped settings only. Updates SHALL accept the current `Version` for optimistic concurrency. Updates that omit a secret field SHALL preserve the existing stored value. Endpoints SHALL be authorized via team membership on the team.

#### Scenario: Update from-address only
- **WHEN** an organizer submits an update to team-scoped settings with only `fromAddress` changed and the correct `Version`
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Reject update with stale version
- **WHEN** an organizer submits an update with a `Version` older than the stored value
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the team attempts to update settings
- **THEN** the request is denied with a 403 response

### Requirement: Email module exposes a facade for cross-module configuration checks
The Email module's Contracts namespace SHALL expose an `IEventEmailFacade` interface with a method that reports whether email is configured for a given event. The facade implementation SHALL return true if and only if the owning team's email settings exist and those settings pass the domain `IsValid` check (all required fields populated). The facade SHALL NOT perform an SMTP connectivity probe and SHALL NOT consult event-scoped settings.

#### Scenario: Reports configured when team-scoped settings exist and are valid
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose owning team has valid team-scoped `EmailSettings`
- **THEN** the facade returns true

#### Scenario: Reports not configured when team has no settings
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose owning team has no settings row
- **THEN** the facade returns false

#### Scenario: Reports not configured when required fields are missing
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose owning team's settings row is missing the from-address
- **THEN** the facade returns false

### Requirement: Email module owns email server settings as a scoped aggregate
The system SHALL provide an Email module that owns email server settings as a single team-scoped `EmailSettings` aggregate. Each settings record SHALL belong to exactly one team and SHALL NOT be scoped to a specific ticketed event. Settings SHALL include at minimum: SMTP host, SMTP port, from-address, authentication mode (`none`, `basic`), and credentials when applicable. The aggregate SHALL carry a `Version` token for optimistic concurrency and SHALL be persisted in the `email` database schema. A unique index on `team_id` SHALL enforce at most one settings row per team.

#### Scenario: Create team-scoped email settings
- **WHEN** an organizer creates email settings for team "acme" with host "smtp.acme.org", port 587, from-address "events@acme.org", auth "basic", username "noreply", password "secret"
- **THEN** an `EmailSettings` aggregate is persisted with `TeamId` referencing the "acme" team id

#### Scenario: At most one settings record per team
- **WHEN** an organizer attempts to create a second settings row for `TeamId=acme`
- **THEN** the request is rejected with an "already exists" error

#### Scenario: Event-scoped settings are unsupported
- **WHEN** an organizer or client attempts to create settings for a specific ticketed event
- **THEN** no event-scoped settings endpoint or command is available

### Requirement: Email module exposes effective settings to its own send path
The Email module SHALL provide an internal contract (not exposed to other modules) that returns the team-scoped `EmailSettings` for an event's owning team, including the decrypted credentials needed to open an SMTP connection. This contract SHALL only be available inside the Email module assembly and SHALL only be called by email-sending paths.

#### Scenario: Send path resolves team settings for an event
- **WHEN** the send-email command handler resolves settings for event "devconf-2026" owned by team "acme"
- **THEN** the returned `EffectiveEmailSettings` carries the team-scoped host/port/from/credentials for "acme"

#### Scenario: Send path returns null when team has no settings
- **WHEN** the send-email command handler resolves settings for an event whose owning team has no settings row
- **THEN** the contract returns null and the handler records a Failed log row with reason "email not configured" (per `email-sending`)

### Requirement: Organizers can send a diagnostic test email via the saved settings of either scope
The Email module SHALL expose an admin HTTP endpoint that sends a diagnostic email using the saved team settings, so organizers can verify SMTP credentials before relying on them for real sends. The endpoint SHALL exist at team scope (`POST /admin/teams/{teamSlug}/email-settings/test`) only.

The request body SHALL carry a single `recipient` field (an email address). The diagnostic send SHALL be performed synchronously through `IEmailSender` (no outbox, no Quartz job) so the caller receives the success or failure result on the same HTTP response. The send SHALL NOT write any row to `email_log`, because the diagnostic is not real correspondence.

#### Scenario: Diagnostic send succeeds at team scope
- **GIVEN** team "acme" has saved valid team-scoped email settings AND an organizer is a member of "acme"
- **WHEN** the organizer issues `POST /admin/teams/acme/email-settings/test` with body `{"recipient": "ops@acme.org"}`
- **THEN** the Email module reads the team-scoped settings row, decrypts the SMTP password, and asks `IEmailSender` to send a fixed-content diagnostic message to "ops@acme.org" using those settings
- **AND** the response is `200 OK`
- **AND** no row is written to `email_log`

#### Scenario: Event-scope diagnostic endpoint is unavailable
- **WHEN** an organizer attempts to send a diagnostic email for a specific event scope
- **THEN** no event-scoped diagnostic endpoint is available

#### Scenario: Settings are present but invalid
- **GIVEN** the saved team settings have `AuthMode=Basic` but no stored password
- **WHEN** an organizer issues a test request with a valid recipient
- **THEN** the request is rejected with a business-rule error indicating that the saved settings are incomplete
- **AND** no SMTP connection is attempted

#### Scenario: SMTP transport fails
- **GIVEN** the saved team settings have a wrong password
- **WHEN** an organizer issues a test request with a valid recipient
- **THEN** the SMTP authentication failure is wrapped into a business-rule error whose message includes the underlying transport error
- **AND** the response status indicates a client-visible failure
- **AND** no row is written to `email_log`

#### Scenario: Recipient validation
- **WHEN** an organizer issues a test request with a `recipient` field that is missing or not a syntactically valid email address
- **THEN** the request is rejected by the endpoint validator with a `400 Bad Request`
- **AND** no settings row is loaded and no SMTP connection is attempted

#### Scenario: Authorization
- **WHEN** a user who is not an Organizer (or higher) of the team issues a test request
- **THEN** the request is denied with a `403` response
- **AND** no diagnostic email is sent

## ADDED Requirements

### Requirement: Email branding is configurable per team
The Email module SHALL store simple team-scoped branding used by built-in transactional emails. Branding SHALL include an accent color and a font-family string. The API SHALL accept the font-family value as a string and SHALL NOT validate whether it names an actual font or an email-safe font. When branding has not been configured, the module SHALL use default accent color and default font-family values.

#### Scenario: Team branding is saved with email settings
- **WHEN** an organizer saves team email settings with accent color `#2563eb` and font `Inter`
- **THEN** the Email module persists those branding values for the team

#### Scenario: Arbitrary font string is stored
- **WHEN** an API client saves team email settings with font `Inter, Arial, sans-serif`
- **THEN** the Email module persists that font string without checking whether `Inter` is installed or email-safe

#### Scenario: Defaults are used when branding is absent
- **WHEN** a transactional email is rendered for a team with no explicit branding values
- **THEN** the renderer uses the default accent color and default font-family string
