## REMOVED Requirements

### Requirement: Secret fields are encrypted at rest using ASP.NET Data Protection
**Reason**: Organizer-managed SMTP credentials are removed. Application email uses deployment-provided system SMTP settings instead of per-team or per-event persisted SMTP secrets.
**Migration**: Remove persisted SMTP password columns and Data Protection usage that existed only for `EmailSettings` secrets. Deployment secrets move to normal host configuration/secret providers.

### Requirement: Organizers can update email settings via admin endpoints
**Reason**: Organizers no longer manage SMTP settings through Admitto.
**Migration**: Remove email-settings admin endpoints, generated SDK functions, proxy routes, and UI forms.

### Requirement: Email module exposes a facade for cross-module configuration checks
**Reason**: Email configuration is no longer team/event data and registration behavior is not gated by organizer SMTP configuration.
**Migration**: Remove `IEventEmailFacade` usages that only check whether event email settings exist. If future cross-module email status is needed, model it as platform health/operability, not event state.

### Requirement: Email module CRUD endpoints register in all hosts
**Reason**: Email settings CRUD endpoints are removed.
**Migration**: Remove endpoint registrations and related handler/query registrations.

### Requirement: Email module owns email server settings as a scoped aggregate
**Reason**: SMTP server settings are deployment configuration, not a persisted Email aggregate.
**Migration**: Drop or stop using `email.email_settings` SMTP fields/table through EF migration, and route senders through system email options.

### Requirement: Email module exposes effective settings to its own send path
**Reason**: Effective settings no longer resolve per event/team from database rows.
**Migration**: Replace database-backed effective-settings resolution with system sender configuration plus team branding context.

### Requirement: Organizers can send a diagnostic test email via the saved settings of either scope
**Reason**: There are no saved organizer SMTP settings to test.
**Migration**: Remove diagnostic email endpoints and Admin UI actions. Operational SMTP validation belongs to deployment checks or operator tooling.

## ADDED Requirements

### Requirement: System SMTP settings come from host configuration
The Email module SHALL send application email using SMTP settings supplied through host configuration. Required configuration SHALL include SMTP host, port, authenticated sender address, and authentication mode/credentials when applicable. The configured sender address SHALL use an Admitto-controlled domain.

#### Scenario: Worker resolves system SMTP settings
- **WHEN** the Worker sends an application email and valid system SMTP configuration is present
- **THEN** the send path uses the configured SMTP server and Admitto sender address

#### Scenario: Missing system SMTP configuration is operational failure
- **WHEN** the Worker attempts to send an application email and required system SMTP configuration is missing
- **THEN** the send attempt fails as an operator-visible configuration problem rather than as team-owned missing email settings
