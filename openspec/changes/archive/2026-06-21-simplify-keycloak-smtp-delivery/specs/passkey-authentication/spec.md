## MODIFIED Requirements

### Requirement: A bootstrap admin user is provisioned on startup
When `Organization:BootstrapAdmin:EmailAddress` is configured, the system SHALL
ensure on startup that a `User` record exists with that email and the Admin role.
If the Keycloak record for that user does not yet exist, the system SHALL provision one and trigger a passkey-enrollment invitation in production. Production passkey-enrollment invitation email rendering and delivery SHALL be handled by Keycloak's built-in account-action email flow using Keycloak SMTP configuration. The operation SHALL be idempotent across restarts and SHALL NOT require a temporary password.

#### Scenario: Provision the bootstrap admin on a fresh production database
- **WHEN** the API starts in production with `Organization:BootstrapAdmin:EmailAddress = "admin@example.com"` and no user exists with that email
- **THEN** an Admin user is created for `admin@example.com`, a Keycloak user is created or reconciled, and Keycloak sends a passkey-enrollment invitation through its configured SMTP server

#### Scenario: Bootstrap is a no-op when the admin already exists and is provisioned
- **WHEN** the API starts with `Organization:BootstrapAdmin:EmailAddress = "admin@example.com"` and an Admin user with that email already exists with an external user identifier
- **THEN** no new Admitto user is created and no new invitation is issued by default

#### Scenario: Bootstrap reconciles an admin without an external user identifier
- **WHEN** the API starts with `Organization:BootstrapAdmin:EmailAddress = "admin@example.com"` and an Admin user with that email exists without an external user identifier
- **THEN** the system provisions or finds the Keycloak user, stores the external user identifier, and issues a passkey-enrollment invitation when required by the active environment

#### Scenario: Production requires Keycloak SMTP configuration for account-action email
- **WHEN** Keycloak sends the bootstrap administrator's passkey-enrollment action email in production
- **THEN** Keycloak connects to its configured SMTP server and does not call an Admitto identity-email endpoint or publish an Admitto identity-email queue message

#### Scenario: Local account-action email is delivered to MailDev
- **WHEN** a developer triggers a Keycloak execute-actions email in local development
- **THEN** Keycloak sends the email to MailDev using the local realm SMTP settings

#### Scenario: Skip bootstrap when not configured
- **WHEN** the API starts without `Organization:BootstrapAdmin:EmailAddress` configured
- **THEN** no bootstrap user is created
