## Purpose

The system uses passkeys (WebAuthn) as the sole authentication factor for the Admin UI, delegating the ceremony to a configured external identity provider. API authentication is provider-agnostic and based on standard JWT bearer tokens.
## Requirements
### Requirement: Admin UI authenticates users with passkeys
In production, the system SHALL use passkeys (WebAuthn passwordless credentials) as the only authentication factor for Admin UI users. Authentication SHALL be performed by Keycloak's hosted login and enrollment pages; the Admitto codebase SHALL NOT implement the WebAuthn ceremony itself. Passwords, one-time codes, and other non-passkey factors SHALL NOT be offered as primary authentication for production Admin UI users.

Local development and end-to-end testing MAY use email/password authentication against a local Keycloak realm with seeded users and test-only clients.

#### Scenario: Production sign in with a registered passkey
- **WHEN** a production user with a registered passkey clicks "Sign in" in the Admin UI
- **THEN** Keycloak prompts for a passkey assertion and, on success, redirects the user back to the Admin UI with an active session

#### Scenario: Production sign in fails without a registered passkey
- **WHEN** a production user without any registered passkey attempts to sign in
- **THEN** Keycloak denies the sign-in and no Admitto session is created

#### Scenario: Production sign in does not offer password fallback
- **WHEN** a production user reaches the Admin UI Keycloak login flow
- **THEN** Keycloak does not offer username/password sign-in as an alternative to passkey authentication

#### Scenario: Local development sign in uses seeded password credentials
- **WHEN** a developer signs in against the local Keycloak realm with a seeded user's email and password
- **THEN** Keycloak authenticates the user and redirects back to the Admin UI with an active session

---

### Requirement: API validates JWTs from the configured authority
The API SHALL validate every bearer token using OIDC discovery against the
configured `Authentication:Bearer:Authority`, accepting only tokens whose `aud`
claim matches the configured audience. Token signature validation SHALL use the
JWKS published by the authority. The API SHALL NOT contain identity-provider-
specific claim mapping; it SHALL rely on the standard `sub` claim to identify
the caller.

#### Scenario: Accept a valid token from the configured authority
- **WHEN** a request arrives with a bearer token signed by the configured authority and an `aud` matching the configured audience
- **THEN** the request is authenticated and the caller's `sub` claim is used as the external user identifier

#### Scenario: Reject a token from a different authority
- **WHEN** a request arrives with a bearer token whose issuer does not match the configured authority
- **THEN** the request is rejected with HTTP 401

#### Scenario: Reject a token with a wrong audience
- **WHEN** a request arrives with a bearer token whose `aud` claim does not match the configured audience
- **THEN** the request is rejected with HTTP 401

---

### Requirement: API key authentication remains available for external integrations
The system SHALL continue to accept API key authentication on endpoints that
expose data to external websites, independent of the bearer-token scheme used
for Admin UI users.

#### Scenario: Authenticate an external integration with an API key
- **WHEN** an external website calls a public endpoint with a valid `X-Api-Key` header
- **THEN** the request is authenticated as the team that owns the API key

#### Scenario: Reject a request without bearer or API key
- **WHEN** a request to a protected endpoint arrives without a bearer token and without an API key
- **THEN** the request is rejected with HTTP 401

---

### Requirement: Identity provider is selected by configuration
The system SHALL use Keycloak as the identity provider for production, local development, and end-to-end testing. Environment-specific Keycloak realm imports and AppHost wiring SHALL determine whether the active realm uses production passkey-only browser authentication or local password-based development authentication. At most one Keycloak authority SHALL be active per process.

#### Scenario: Use production Keycloak passkey realm in deployed environments
- **WHEN** the application is deployed with the production Keycloak realm import and public Keycloak authority configured
- **THEN** Admin UI sign-in uses the production passkey-only browser flow and API tokens are issued by that authority

#### Scenario: Use local Keycloak password realm in development
- **WHEN** the application starts in local development with the local Keycloak realm import
- **THEN** Admin UI sign-in can use seeded email/password users and API tokens are issued by the local Keycloak authority

#### Scenario: Reject a missing bearer authority
- **WHEN** the API starts without `Authentication:Bearer:Authority` configured
- **THEN** startup fails with a clear configuration error or authenticated requests fail closed with HTTP 401

---

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

### Requirement: External user identifier is bound on first authenticated request
The system SHALL treat the `User.ExternalUserId` field as the authoritative link
between a domain user and their identity-provider account. When an authenticated
request arrives with an `ExternalUserId` (the JWT's `sub` claim) that is not yet
recorded, the system SHALL look up the corresponding `User` by the email claim and
set `ExternalUserId` if and only if it is currently null. A user record with a
null `ExternalUserId` SHALL be considered "invited but not yet enrolled."

#### Scenario: Bind external user ID on first sign-in after invitation
- **WHEN** an invited user signs in for the first time after registering a passkey, with a JWT carrying `sub = "auth0|abc123"` and email "alice@example.com", and the `User` for "alice@example.com" has a null `ExternalUserId`
- **THEN** the `User`'s `ExternalUserId` is set to "auth0|abc123" and subsequent requests resolve the user by `ExternalUserId`

#### Scenario: Reject a sign-in with an external ID belonging to an unknown email
- **WHEN** an authenticated request arrives with a `sub` claim not matching any known user and an email claim not matching any invited `User`
- **THEN** the request is rejected with HTTP 403

#### Scenario: Do not overwrite an existing external user ID
- **WHEN** an authenticated request arrives with a `sub` claim that does not match the `ExternalUserId` already stored for the user resolved by email
- **THEN** the request is rejected with HTTP 403 and the stored `ExternalUserId` is not changed

---

### Requirement: Passkey recovery is not self-service
The system SHALL NOT provide a public self-service recovery flow for users who lose their passkey. Recovery and enrollment resend behavior are out of scope for this change and SHALL be handled by a future controlled operator/admin flow.

#### Scenario: Anonymous user cannot request recovery
- **WHEN** an unauthenticated user attempts to request a passkey-enrollment resend
- **THEN** the system rejects the request and sends no enrollment email

---

### Requirement: End-to-end tests obtain tokens from Keycloak via password grant
The system SHALL support end-to-end tests against a development Keycloak realm by
exposing a confidential test client with the `direct access grants` flow enabled
and seeded test users with known credentials and no required actions. This
configuration SHALL exist only in the local/test realm and SHALL NOT be
replicated to production.

#### Scenario: Mint a token for a seeded test user
- **WHEN** an end-to-end test posts to the Keycloak token endpoint with the test client credentials, `grant_type = password`, and a seeded test user's username and password
- **THEN** Keycloak returns a JWT that the API accepts

#### Scenario: Production realm refuses password grant
- **WHEN** any client posts to the production Keycloak realm with `grant_type = password`
- **THEN** the token request is rejected because the password grant is not enabled

