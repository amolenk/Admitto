## ADDED Requirements

### Requirement: Keycloak identity emails are rendered and delivered through Admitto
The system SHALL provide an internal API endpoint that accepts structured Keycloak identity-email requests, including passkey-enrollment and account-action events, and hands them off to Admitto's email template rendering and delivery infrastructure. Keycloak SHALL call this endpoint through a custom email template/renderer provider instead of rendering final email content or using direct SMTP settings in production. The endpoint SHALL authenticate Keycloak with a dedicated internal shared-secret signature and SHALL reject unauthenticated or incorrectly signed requests. Identity-email templates SHALL be built-in, internal-only, non-overridable, and excluded from Admin UI template management.

#### Scenario: Keycloak enrollment email is accepted by Admitto
- **WHEN** the production Keycloak email renderer posts a structured `passkey-enrollment` identity-email request to the Admitto API with a valid signature
- **THEN** the API accepts the request for template rendering and delivery and returns a success response to Keycloak

#### Scenario: Keycloak does not send rendered email content
- **WHEN** Keycloak requests a production passkey-enrollment email
- **THEN** the Keycloak renderer sends JSON containing the supported identity-email type, recipient, action link, expiration, locale, and idempotency key, and does not send final subject, text body, or HTML body fields

#### Scenario: Admitto renders identity email from templates
- **WHEN** the Worker processes an accepted `passkey-enrollment` identity-email request
- **THEN** Admitto resolves the built-in internal system identity-email template and renders the subject, text body, and HTML body using the JSON request data

#### Scenario: Identity template cannot be overridden by team or event template
- **WHEN** a team or event has an email template with a name matching a Keycloak identity-email type
- **THEN** identity-email rendering ignores that team or event template and uses the built-in internal identity template

#### Scenario: Identity template is not exposed in Admin UI template management
- **WHEN** an administrator lists, creates, updates, or deletes templates through the Admin UI or admin template APIs
- **THEN** Keycloak identity-email templates are not listed and cannot be created, updated, or deleted through those surfaces

#### Scenario: Unsigned Keycloak email request is rejected
- **WHEN** a request posts to the Keycloak identity-email endpoint without the required signature headers
- **THEN** the API rejects the request and no email is accepted for delivery

#### Scenario: Invalid Keycloak email signature is rejected
- **WHEN** a request posts to the Keycloak identity-email endpoint with a signature that does not match the shared secret and request body
- **THEN** the API rejects the request and no email is accepted for delivery

#### Scenario: Duplicate Keycloak email request is idempotent
- **WHEN** Keycloak retries the same identity-email request with the same idempotency key
- **THEN** Admitto accepts the retry without sending duplicate emails for that idempotency key

#### Scenario: Worker sends accepted Keycloak email
- **WHEN** the API has accepted a valid Keycloak identity-email request
- **THEN** the Worker renders the email with built-in internal Admitto identity templates, sends it through Admitto's configured system email sender, and records the delivery outcome
