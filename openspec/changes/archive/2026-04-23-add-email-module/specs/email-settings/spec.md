## ADDED Requirements

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

## MODIFIED Requirements

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


## REMOVED Requirements

### Requirement: Email module owns per-event email server settings
**Reason**: Replaced by the unified scope-keyed `EmailSettings` aggregate which supports both team-scope and event-scope settings under a single aggregate type.
