# Email Settings Specification

## Purpose

The Email module owns per-event email server settings (SMTP host, port, from-address, authentication mode, and credentials). It persists secrets encrypted with ASP.NET Data Protection, exposes admin endpoints for organizers to manage the settings, and publishes a facade that other modules (notably Registrations) can consult to determine whether email is configured for a given event.
## Requirements
### Requirement: Secret fields are encrypted at rest using ASP.NET Data Protection
The Email module SHALL encrypt secret fields (SMTP password and any future API tokens or full connection strings) on every `EmailSettings` row — regardless of scope — using the ASP.NET Core Data Protection API before persisting them. Decryption SHALL only happen inside the Email module's infrastructure. Read APIs SHALL never expose the plaintext secret to other modules or to API responses.

#### Scenario: Password is encrypted in the database
- **WHEN** an event-scoped or team-scoped settings row is saved with password "p@ssw0rd"
- **THEN** the password column in `email.email_settings` does not contain the literal string "p@ssw0rd"

#### Scenario: Admin GET masks the password
- **WHEN** an organizer reads settings at either scope via the admin endpoint
- **THEN** the response contains metadata (host, port, from-address, auth mode, hasPassword=true) but does not include the plaintext password

#### Scenario: Cross-purpose decryption is rejected
- **WHEN** code outside the Email module attempts to decrypt the protected blob using a different Data Protection purpose string
- **THEN** decryption fails

### Requirement: Organizers can update email settings via admin endpoints
The Email module SHALL expose admin HTTP endpoints to read, create, update, and delete settings at both team and event scope, sharing one slice family. Updates SHALL accept the current `Version` for optimistic concurrency. Updates that omit a secret field SHALL preserve the existing stored value. Endpoints SHALL be authorized via team membership on the team that owns the scope (the team itself for team-scope, the event's owning team for event-scope).

#### Scenario: Update from-address only — event scope
- **WHEN** an organizer submits an update to event-scoped settings with only `fromAddress` changed and the correct `Version`
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Update from-address only — team scope
- **WHEN** an organizer submits an update to team-scoped settings with only `fromAddress` changed and the correct `Version`
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Reject update with stale version
- **WHEN** an organizer submits an update with a `Version` older than the stored value
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the relevant team attempts to update settings at either scope
- **THEN** the request is denied with a 403 response

---

### Requirement: Email module exposes a facade for cross-module configuration checks
The Email module's Contracts project SHALL expose an `IEventEmailFacade` interface with a method that reports whether email is configured for a given event. The facade implementation SHALL return true if and only if effective email settings exist for the event AND those settings pass the domain `IsValid` check (all required fields populated). Effective settings SHALL be resolved as: (1) the event-scoped `EmailSettings` row for `(Scope=Event, ScopeId=eventId)` if present; otherwise (2) the team-scoped `EmailSettings` row for `(Scope=Team, ScopeId=teamId)` of the event's owning team if present; otherwise none. There SHALL be no per-field merging across the two scopes — when event-scoped settings exist, team-scoped settings are ignored entirely. The facade SHALL NOT perform an SMTP connectivity probe.

#### Scenario: Reports configured when event-scoped settings exist and are valid
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose event-scoped `EmailSettings` row exists with all required fields populated
- **THEN** the facade returns true

#### Scenario: Reports configured when only team-scoped settings exist and are valid
- **WHEN** `IsEmailConfiguredAsync` is called for an event with no event-scoped row but whose owning team has a valid team-scoped row
- **THEN** the facade returns true

#### Scenario: Event-scoped fully overrides team-scoped
- **WHEN** an event has both event-scoped settings (invalid: missing from-address) and team-scoped settings (fully valid)
- **THEN** the facade returns false because event-scoped settings, when present, are used in full and they are invalid

#### Scenario: Reports not configured when neither scope has settings
- **WHEN** `IsEmailConfiguredAsync` is called for an event with no settings row in either scope
- **THEN** the facade returns false

#### Scenario: Reports not configured when required fields are missing in the chosen scope
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose chosen-scope settings row is missing the from-address
- **THEN** the facade returns false

---

### Requirement: Email module CRUD endpoints register in all hosts
Admin endpoints and the `IEventEmailFacade` implementation SHALL be registered in any host that loads the Email module assembly (API host, Worker host, Migrations host). Email-sending capability (future) MAY be gated behind `HostCapability.Email`, but settings management and the configuration-status facade are metadata operations and SHALL NOT be capability-gated.

#### Scenario: Facade resolvable in API host
- **WHEN** the API host requests `IEventEmailFacade` from DI
- **THEN** an implementation is resolved without requiring `HostCapability.Email`

### Requirement: Email module owns email server settings as a single scope-keyed aggregate
The system SHALL provide an Email module (`Admitto.Module.Email`) that owns email server settings as a single `EmailSettings` aggregate keyed by `(Scope, ScopeId)` where `Scope ∈ {Team, Event}`. Each settings record SHALL belong to exactly one scope (a specific team OR a specific event). Settings SHALL include at minimum: SMTP host, SMTP port, from-address, authentication mode (`none`, `basic`), and credentials when applicable. The aggregate SHALL carry a `Version` token for optimistic concurrency and SHALL be persisted in a dedicated `email` database schema. A unique index on `(scope, scope_id)` SHALL enforce at most one settings row per scope per scopeId.

#### Scenario: Create event-scoped email settings
- **WHEN** an organizer creates email settings for event "devconf-2026" with host "smtp.acme.org", port 587, from-address "events@acme.org", auth "basic", username "noreply", password "secret"
- **THEN** an `EmailSettings` aggregate is persisted in the `email` schema with `Scope=Event` and `ScopeId` referencing "devconf-2026"

#### Scenario: Create team-scoped email settings
- **WHEN** an organizer creates email settings for team "acme" with the same fields
- **THEN** an `EmailSettings` aggregate is persisted with `Scope=Team` and `ScopeId` referencing the "acme" team id

#### Scenario: At most one settings record per event scope
- **WHEN** an organizer attempts to create a second settings row for `(Scope=Event, ScopeId=devconf-2026)`
- **THEN** the request is rejected with an "already exists" error

#### Scenario: At most one settings record per team scope
- **WHEN** an organizer attempts to create a second settings row for `(Scope=Team, ScopeId=acme)`
- **THEN** the request is rejected with an "already exists" error

#### Scenario: Existing event-scoped data is preserved through the storage change
- **WHEN** the EF migration runs against a database that previously contained rows in `event_email_settings`
- **THEN** every prior row appears in the new `email_settings` table as `(scope='event', scope_id=<original ticketed_event_id>, …)` with all other fields preserved

---

### Requirement: Email module exposes effective settings to its own send path
The Email module SHALL provide an internal contract (not exposed to other modules) that returns the **effective** `EmailSettings` for an event — the resolved combination of event-scoped and team-scoped settings per the precedence rules above — including the decrypted credentials needed to open an SMTP connection. This contract SHALL only be available inside the Email module assembly and SHALL only be called by the email-sending command handler.

#### Scenario: Send path resolves event-scoped over team-scoped
- **WHEN** the send-email command handler resolves effective settings for an event with both scopes present
- **THEN** the returned `EffectiveEmailSettings` carries the event-scoped host/port/from/credentials and ignores the team-scoped row

#### Scenario: Send path resolves team-scoped when event-scoped absent
- **WHEN** the send-email command handler resolves effective settings for an event with only team-scoped settings
- **THEN** the returned `EffectiveEmailSettings` carries the team-scoped host/port/from/credentials

#### Scenario: Send path returns null when neither scope has settings
- **WHEN** the send-email command handler resolves effective settings for an event with no settings in either scope
- **THEN** the contract returns null and the handler records a Failed log row with reason "email not configured" (per `email-sending`)

---

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

---

