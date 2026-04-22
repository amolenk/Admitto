# Email Settings Specification

## Purpose

The Email module owns per-event email server settings (SMTP host, port, from-address, authentication mode, and credentials). It persists secrets encrypted with ASP.NET Data Protection, exposes admin endpoints for organizers to manage the settings, and publishes a facade that other modules (notably Registrations) can consult to determine whether email is configured for a given event.

## Requirements

### Requirement: Email module owns per-event email server settings
The system SHALL provide an Email module (`Admitto.Module.Email`) that owns email server settings on a per-ticketed-event basis. Each settings record SHALL belong to exactly one event, identified by the event's `TicketedEventId`. Settings SHALL include at minimum: SMTP host, SMTP port, from-address, authentication mode (none, basic), and credentials when applicable. The aggregate SHALL carry a `Version` token for optimistic concurrency and SHALL be persisted in a dedicated `email` database schema.

#### Scenario: Create email settings for an event
- **WHEN** an organizer creates email settings for event "devconf-2026" with host "smtp.acme.org", port 587, from-address "events@acme.org", auth "basic", username "noreply", password "secret"
- **THEN** an `EventEmailSettings` aggregate is persisted in the `email` schema with `TicketedEventId` referencing "devconf-2026"

#### Scenario: At most one settings record per event
- **WHEN** an organizer attempts to create a second email settings record for an event that already has one
- **THEN** the request is rejected with an "already exists" error

---

### Requirement: Secret fields are encrypted at rest using ASP.NET Data Protection
The Email module SHALL encrypt secret fields (SMTP password and any future API tokens or full connection strings) using the ASP.NET Core Data Protection API before persisting them. Decryption SHALL only happen inside the Email module's infrastructure. Read APIs SHALL never expose the plaintext secret to other modules or to API responses.

#### Scenario: Password is encrypted in the database
- **WHEN** email settings are saved with password "p@ssw0rd"
- **THEN** the password column in `email.event_email_settings` does not contain the literal string "p@ssw0rd"

#### Scenario: Admin GET masks the password
- **WHEN** an organizer reads email settings for an event via the admin endpoint
- **THEN** the response contains metadata (host, port, from-address, auth mode, hasPassword=true) but does not include the plaintext password

#### Scenario: Cross-purpose decryption is rejected
- **WHEN** code outside the Email module attempts to decrypt the protected blob using a different Data Protection purpose string
- **THEN** decryption fails

---

### Requirement: Organizers can update email settings via admin endpoints
The Email module SHALL expose admin HTTP endpoints to read and update an event's email settings. Updates SHALL accept the current `Version` for optimistic concurrency. Updates that omit a secret field SHALL preserve the existing stored value. Endpoints SHALL be authorized via team membership on the event's owning team.

#### Scenario: Update from-address only
- **WHEN** an organizer submits an update with only `fromAddress` changed and the correct `Version`
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Reject update with stale version
- **WHEN** an organizer submits an update with a `Version` older than the stored value
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the event's owning team attempts to update email settings
- **THEN** the request is denied with a 403 response

---

### Requirement: Email module exposes a facade for cross-module configuration checks
The Email module's Contracts project SHALL expose an `IEventEmailFacade` interface with a method that reports whether email is configured for a given event. The facade implementation SHALL return true if and only if an `EventEmailSettings` aggregate exists for the event and its domain `IsValid` check passes (all required fields populated). The facade SHALL NOT perform an SMTP connectivity probe.

#### Scenario: Reports configured when settings exist and are valid
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose `EventEmailSettings` row exists with all required fields populated
- **THEN** the facade returns true

#### Scenario: Reports not configured when no settings exist
- **WHEN** `IsEmailConfiguredAsync` is called for an event with no `EventEmailSettings` row
- **THEN** the facade returns false

#### Scenario: Reports not configured when required fields are missing
- **WHEN** `IsEmailConfiguredAsync` is called for an event whose `EventEmailSettings` row is missing the from-address
- **THEN** the facade returns false

---

### Requirement: Email module CRUD endpoints register in all hosts
Admin endpoints and the `IEventEmailFacade` implementation SHALL be registered in any host that loads the Email module assembly (API host, Worker host, Migrations host). Email-sending capability (future) MAY be gated behind `HostCapability.Email`, but settings management and the configuration-status facade are metadata operations and SHALL NOT be capability-gated.

#### Scenario: Facade resolvable in API host
- **WHEN** the API host requests `IEventEmailFacade` from DI
- **THEN** an implementation is resolved without requiring `HostCapability.Email`
