## Why

The first administrator currently has a chicken-and-egg problem: a bootstrap user can be created without any usable password, while the production Keycloak login flow still offers password-based sign-in. We need a clear environment-specific authentication model that keeps local development simple with email/password but makes production bootstrap and organizer sign-in passwordless through passkeys.

## What Changes

- Configure production Keycloak for Admitto Admin UI users to use passkey-only browser authentication with no password fallback.
- Keep local development and end-to-end testing on email/password with seeded users and direct-grant test clients.
- Make bootstrap admin provisioning create or reconcile the Admitto admin user and the corresponding Keycloak user, then send a passkey-enrollment action email.
- Add a Keycloak email-template/renderer plugin that sends structured identity-email JSON to the Admitto API instead of rendering final email content or requiring SMTP configuration inside Keycloak.
- Render Keycloak identity emails with built-in Admitto system templates that cannot be overridden and are not exposed in the Admin UI template management screens.
- Authenticate Keycloak-to-Admitto email calls with a dedicated internal shared-secret signature, not an end-user token.
- Update architecture documentation to reflect Keycloak passkey-only production behavior and local password-based development behavior.
- Remove remaining Auth0-era requirements from the passkey authentication capability.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `passkey-authentication`: Clarify production Keycloak passkey-only authentication, local email/password development behavior, and bootstrap admin enrollment behavior.
- `email-sending`: Add system identity-email rendering and delivery for Keycloak identity-email events flowing through the Admitto API.

## Impact

- Keycloak realm imports under `src/Admitto.AppHost/KeycloakConfiguration/`.
- Keycloak custom provider packaging under `src/Admitto.AppHost/KeycloakConfiguration/`.
- Internal API endpoint and authentication for Keycloak email delivery.
- Email module support for system identity emails that are not scoped to a team or event.
- Organization bootstrap and external user provisioning under `src/Admitto.Core/Organization/`.
- Aspire AppHost environment wiring for production, local development, and tests.
- Admin UI sign-in copy or behavior if it references passwordless assumptions.
- Architecture documentation in `docs/arc42/` and ADRs under `docs/adrs/`.
- Tests covering bootstrap idempotency, Keycloak provisioning intent, and environment-specific auth configuration.
