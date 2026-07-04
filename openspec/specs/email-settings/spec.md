# Email Settings Specification

## Purpose

The Email module uses deployment-provided system SMTP configuration for application email. Organizers do not manage SMTP settings through Admitto, and no per-team or per-event SMTP credentials are persisted.

## Requirements

### Requirement: System SMTP settings come from host configuration

The Email module SHALL send application email using SMTP settings supplied through host configuration. Required configuration SHALL include SMTP host, port, authenticated sender address, and authentication mode/credentials when applicable. The configured sender address SHALL use an Admitto-controlled domain.

Team-owned reply-to email addresses are not SMTP sender settings. They SHALL be stored as team metadata and projected into Email's context for the `Reply-To` header and visible `From` display name only.

#### Scenario: Worker resolves system SMTP settings

- **WHEN** the Worker sends an application email and valid system SMTP configuration is present
- **THEN** the send path uses the configured SMTP server and Admitto sender address

#### Scenario: Missing system SMTP configuration is operational failure

- **WHEN** the Worker attempts to send an application email and required system SMTP configuration is missing
- **THEN** the send attempt fails as an operator-visible configuration problem rather than as team-owned missing email settings

#### Scenario: Team reply-to is not an SMTP sender setting

- **WHEN** a team has reply-to email address `help@example.com`
- **THEN** the Worker still authenticates and sends through the configured system SMTP sender address while using `help@example.com` only for reply routing and the visible `From` display name
