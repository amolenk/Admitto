## ADDED Requirements

### Requirement: Admin UI authenticates users with passkeys
The system SHALL use passkeys (WebAuthn) as the only authentication factor for the
Admin UI. Authentication is performed against the configured external identity
provider; the Admitto codebase SHALL NOT implement the WebAuthn ceremony itself.
Passwords, one-time codes, and other factors SHALL NOT be offered as primary
authentication for end users.

#### Scenario: Sign in with a registered passkey
- **WHEN** a user with a registered passkey clicks "Sign in" in the Admin UI
- **THEN** the identity provider's hosted page prompts for a passkey assertion and, on success, the user is redirected back to the Admin UI with an active session

#### Scenario: Sign in fails without a registered passkey
- **WHEN** a user without any registered passkey attempts to sign in
- **THEN** the identity provider denies the sign-in and no Admitto session is created

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
The system SHALL select between identity-provider implementations based on the
presence of provider-specific configuration sections. When `Authentication:Auth0`
is present, the Auth0 implementation SHALL be used. Otherwise, when
`Authentication:Keycloak` is present, the Keycloak implementation SHALL be used.
At most one provider SHALL be active per process.

#### Scenario: Use Auth0 when configured for production
- **WHEN** the application starts with `Authentication:Auth0` configured
- **THEN** `IUserDirectory` is fulfilled by the Auth0 implementation and invitations are issued through the Auth0 Management API

#### Scenario: Use Keycloak when configured for development
- **WHEN** the application starts with `Authentication:Keycloak` configured and `Authentication:Auth0` absent
- **THEN** `IUserDirectory` is fulfilled by the Keycloak implementation and invitations are issued through the Keycloak Admin API

#### Scenario: Reject a misconfigured environment
- **WHEN** the application starts with neither `Authentication:Auth0` nor `Authentication:Keycloak` configured
- **THEN** startup fails with a clear configuration error

---

### Requirement: A bootstrap admin user is provisioned on startup
When `Organization:BootstrapAdmin:EmailAddress` is configured, the system SHALL
ensure on startup that a `User` record exists with that email and the Admin role.
If the IdP record for that user does not yet exist, the system SHALL provision
one and trigger a passkey-enrollment invitation. The operation SHALL be
idempotent across restarts.

#### Scenario: Provision the bootstrap admin on a fresh database
- **WHEN** the API starts with `Organization:BootstrapAdmin:EmailAddress = "admin@example.com"` and no user exists with that email
- **THEN** an Admin user is created for "admin@example.com" and a passkey-enrollment invitation is issued through the configured identity provider

#### Scenario: Bootstrap is a no-op when the admin already exists
- **WHEN** the API starts with `Organization:BootstrapAdmin:EmailAddress = "admin@example.com"` and an Admin user with that email already exists
- **THEN** no new user is created and no new invitation is issued

#### Scenario: Skip bootstrap when not configured
- **WHEN** the API starts without `Organization:BootstrapAdmin:EmailAddress` configured
- **THEN** no bootstrap user is created

---

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

### Requirement: A user who loses their passkey is recovered by re-invite
The system SHALL NOT provide a self-service recovery flow for users who lose
their passkey. To restore access for such a user, an admin SHALL delete the user
and re-invite them, which SHALL provision a fresh identity-provider account.

#### Scenario: Recover a user who lost their passkey
- **WHEN** an admin deletes "alice@example.com" and then invites her again
- **THEN** a new identity-provider account is provisioned and "alice@example.com" enrols a new passkey via the IdP's hosted enrollment page

---

### Requirement: End-to-end tests obtain tokens from Keycloak via password grant
The system SHALL support end-to-end tests against a development Keycloak realm by
exposing a confidential test client with the `direct access grants` flow enabled
and seeded test users with known credentials and no required actions. This
configuration SHALL exist only in the development realm and SHALL NOT be
replicated to production.

#### Scenario: Mint a token for a seeded test user
- **WHEN** an end-to-end test posts to the Keycloak token endpoint with the test client credentials, `grant_type = password`, and a seeded test user's username and password
- **THEN** Keycloak returns a JWT that the API accepts

#### Scenario: Production realm refuses password grant
- **WHEN** any client posts to the production Auth0 tenant with `grant_type = password`
- **THEN** the token request is rejected because the password grant is not enabled
