# Email Settings Specification

## Purpose

The Email module uses deployment-provided system SMTP configuration for application email. Organizers do not manage SMTP settings through Admitto, and no per-team or per-event SMTP credentials are persisted.

## Requirements

### Requirement: System SMTP settings come from host configuration

The Email module SHALL send application email using SMTP settings supplied through host configuration. Required configuration SHALL include SMTP host, port, authenticated sender address, sender display name, and authentication mode/credentials when applicable. The configured sender address SHALL use an Admitto-controlled domain.

Both the `From` address and the visible `From` display name SHALL come from this configuration and SHALL NOT be hard-coded, so that non-production deployments never send under a production sender identity. Teams SHALL NOT own any sender or reply-to setting.

#### Scenario: Worker resolves system SMTP settings

- **WHEN** the Worker sends an application email and valid system SMTP configuration is present
- **THEN** the send path uses the configured SMTP server and Admitto sender address

#### Scenario: Missing system SMTP configuration is operational failure

- **WHEN** the Worker attempts to send an application email and required system SMTP configuration is missing
- **THEN** the send attempt fails as an operator-visible configuration problem rather than as team-owned missing email settings

#### Scenario: Sender identity comes from configuration, not from code

- **WHEN** a deployment configures a sender address of `noreply@tickets.example.test`
- **THEN** outgoing messages use exactly that address and the configured display name, with no hard-coded fallback to any other domain
