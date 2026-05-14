## Why

The Admin UI currently authenticates against Entra External ID in production and Keycloak in development, using passwords as the primary credential. Passwords are a poor user experience and a security liability. Passkeys (WebAuthn) eliminate password phishing, are faster to use, and are now first-class in both Keycloak (already in the realm config but unused) and Auth0. Replacing Entra External ID with Auth0 also gives us a managed production IdP that natively supports passkeys on its free tier (with a custom domain), removes the operational complexity of Microsoft Graph, and reinforces Admitto's value as a sample app by exercising the existing `IUserDirectory` abstraction with a clean second implementation.

## What Changes

- **BREAKING** Entra External ID is no longer supported as a production identity provider. The `MicrosoftGraphUserDirectory`, `MicrosoftGraphUserManagementService`, the Entra-specific `oid` claim mapping in `HttpContextUserContextAccessor`, and the Entra configuration section are removed. Microsoft.Graph package references are removed from the Organization module.
- **BREAKING** Production deployments must configure an Auth0 tenant. A new `Auth0UserDirectory` implementation is introduced that fulfills `IUserDirectory` by calling the Auth0 Management API for invitations and deprovisioning.
- Passkey (WebAuthn) becomes the primary authentication mechanism for the Admin UI. The Keycloak realm enables the `webauthn-register-passwordless` required action; Auth0 is configured to require passkey enrollment on first login.
- The invite flow no longer asks users to set a password. Instead, the invited user clicks a one-time link, lands on the IdP's hosted enrollment page, registers a passkey on their device, and is signed in.
- A bootstrap admin user (configured via `Organization:BootstrapAdmin:EmailAddress` in appsettings) is provisioned at API startup so a fresh deployment has a path to the first sign-in.
- The OpenAPI Bearer security scheme transformer becomes IdP-agnostic — it derives URLs from the configured authority instead of hardcoding Keycloak paths.
- E2E tests continue to use the dev Keycloak container; Keycloak's Resource Owner Password Credentials grant is used to mint test tokens for seeded test users (passkey enrollment is bypassed in the test realm).
- The API key authentication scheme used by external websites is **unchanged**.

## Capabilities

### New Capabilities
- `passkey-authentication`: How users authenticate to the Admin UI with passkeys, how the API validates IdP-issued JWTs, how the IdP is swapped between dev (Keycloak) and prod (Auth0), and how a bootstrap admin is provisioned.

### Modified Capabilities
- `team-membership`: The invite flow now provisions a user in the configured IdP and triggers a passkey-enrollment required action / ticket instead of a password-set. Deprovisioning no longer involves Microsoft Graph guest-user revocation.

## Impact

- **Code removed**: `src/Admitto.Core/Organization/Infrastructure/UserDirectories/MicrosoftGraph/`, Entra `oid` claim handling in `src/Admitto.Api/Auth/HttpContextUserContextAccessor.cs`, the Microsoft Graph branch of `src/Admitto.Core/Organization/DependencyInjection.cs`, and any Entra-specific appsettings and AppHost wiring.
- **Code added**: `src/Admitto.Core/Organization/Infrastructure/UserDirectories/Auth0/` (Auth0UserDirectory, Auth0UserManagementService, Auth0Options, Management API client), bootstrap-admin seeding service, IdP-agnostic OpenAPI Bearer transformer.
- **Configuration**: Removal of `Authentication:Microsoft*` / Entra sections; addition of `Authentication:Auth0` (Domain, ClientId, ClientSecret, Audience) and `Organization:BootstrapAdmin` sections. `Authentication:Bearer:Authority` continues to be set per-environment (Keycloak in dev, Auth0 tenant or custom domain in prod).
- **Infrastructure**: `src/Admitto.AppHost/KeycloakConfiguration/AdmittoRealm.json` is updated to enable WebAuthn passwordless registration and to seed test users + a confidential test client with direct-access-grants enabled for E2E tests. No new containers in dev.
- **Dependencies**: Microsoft.Graph and related packages removed from `Admitto.Core`. An Auth0 Management API client is added (either the official `Auth0.ManagementApi` NuGet or a thin HTTP client).
- **Documentation**: `docs/arc42/04-solution-strategy.md`, `06-runtime-view.md`, `07-deployment-view.md`, and `09-architecture-decisions.md` (new ADR for "Auth0 + Passkeys") are updated. README and any setup guides referencing Entra are revised.
- **Tests**: API E2E tests switch from any password-based Entra/Keycloak flow to a Keycloak ROPC token mint helper. Domain and integration tests for `team-membership` invite flow are updated for the new IdP semantics.
- **Operational**: Production rollout requires creating an Auth0 tenant, an API resource (`admitto-api`), an M2M application with `create:users`, `create:user_tickets`, `read:users`, `delete:users` scopes, a custom domain (free tier supports one), and configuring passkey as the only enrollment factor.
