# ADR-011: Auth0 with passkeys as the production identity provider

## Status
Superseded by [ADR-012](adr-012-keycloak-production-identity-provider.md).

## Context
Admitto needs a production identity provider (IdP) for the Admin UI. The initial design referenced Microsoft Entra External ID. After evaluation, the following requirements shaped the decision:

1. **Passwordless authentication.** Organizers and team members should authenticate using passkeys (WebAuthn) — no passwords to rotate, no phishing risk.
2. **M2M provisioning.** The backend must provision new admin accounts automatically (bootstrap admin on startup, invitation flow for new team members) and send passkey-enrollment tickets without requiring a human to log in to an IdP console.
3. **Low operational cost.** The IdP should not require maintaining a tenant directory, federation configuration, or Azure subscription just for a small ticketing system.
4. **Local development must not require cloud connectivity.** E2E tests and local runs should work entirely offline.

Microsoft Entra External ID was the original candidate but was deprioritised because:
- Passkey-only flows require Custom Authentication Extensions that add significant configuration complexity.
- M2M provisioning via the Graph API has a large permission surface.
- The free tier limits are low for external user volumes.

## Decision

### D1 — Auth0 is the production identity provider

Auth0 is used in production because it has first-class passkey support in Universal Login, a clean Management API for M2M provisioning, and a generous free tier suitable for the initial audience.

Auth0 Universal Login is configured to require passkey enrollment before first sign-in. Users cannot fall back to passwords.

### D2 — The `Authentication:Auth0` configuration section is the activation switch

Both IdP adapters (Auth0, Keycloak) are registered at startup. The `Authentication:Auth0` section being present in configuration activates the Auth0 `IUserDirectoryService` implementation. If the section is absent, the system defaults to the Keycloak adapter. This makes the IdP choice a deployment-time concern with no code changes required.

### D3 — Auth0 Management API (M2M) is used for user provisioning

A dedicated M2M application in Auth0 is granted the following scopes on the Management API:

| Scope | Purpose |
| :---- | :------ |
| `create:users` | Provision a new Auth0 user account |
| `delete:users` | Remove a user during team-member removal |
| `update:users` | Patch user metadata |
| `create:user_tickets` | Generate a passkey-enrollment invitation ticket |

The API uses client-credentials flow (`client_id` / `client_secret` from `Authentication:Auth0`) to obtain a Management API token. The token is cached and refreshed before expiry.

### D4 — A bootstrap admin is provisioned on startup

On API startup, `BootstrapAdminInitializer` reads `Organization:BootstrapAdmin:EmailAddress`. If no user with that email address exists in the database, it:

1. Creates a `User` entity in the Organization module.
2. Calls `IUserDirectoryService.InviteUserAsync` to create an Auth0 account and generate a passkey-enrollment ticket URL.
3. Stores the returned `ExternalUserId` (`sub`) on the entity.

The initialiser is idempotent: if the user already exists and has an `ExternalUserId`, it skips provisioning silently.

### D5 — Keycloak is used for local development and E2E testing

`Admitto.AppHost` provisions a Keycloak container automatically. No Auth0 configuration is needed for local development. E2E tests authenticate using Keycloak's Resource Owner Password Credentials (ROPC) grant for programmatic token acquisition — this grant is intentionally disabled in Auth0 production tenants.

## Rationale

- **Passkeys eliminate credential management risk.** There are no passwords to leak, rotate, or reset.
- **Auth0 Universal Login with passkey enforcement** is the lowest-friction path to a passkey-only experience. The enrollment invitation ticket drives users through the setup flow on first access.
- **Configuration-driven IdP selection** avoids environment-specific code paths. Swapping IdPs in the future (or running a second environment against a different provider) requires only a config change.
- **Bootstrap admin on startup** removes the chicken-and-egg problem of needing an admin to create the first admin. A single environment variable is sufficient to bootstrap a fresh deployment.

## Consequences

### Positive
- No password storage or rotation anywhere in the system.
- M2M provisioning is fully automated; no manual IdP console steps after initial tenant setup.
- Local development is identical to production modulo the IdP adapter; architecture tests enforce the abstraction boundary.

### Negative
- Auth0 Management API token must be cached and refreshed; a stale or revoked token causes provisioning failures.
- The passkey-enrollment ticket has an expiry. If a new user does not enroll within the expiry window, a new invitation must be triggered.
- Keycloak ROPC grant used in E2E tests is not available in production, so E2E test coverage of the Auth0 flow is limited to integration test doubles.

### Neutral
- The `Authentication:Bearer:Authority` configuration (OIDC discovery URL) is separate from the `Authentication:Auth0` M2M credentials section. Changing the OIDC authority does not automatically update the Management API base URL, and vice versa.

## References
- arc42 chapter 5 — building-block view (infrastructure mapping table).
- arc42 chapter 6 — runtime view (§6.11 user sign-in and ExternalUserId binding, §6.12 bootstrap admin provisioning).
- arc42 chapter 7 — deployment view (production shape).
- Change: `openspec/changes/swap-entra-for-auth0-with-passkeys/`.
