# ADR-012: Keycloak as the production identity provider

## Status

Accepted. Supersedes [ADR-011](adr-011-auth0-passkeys.md).

## Context

Admitto already uses Keycloak for local development and tests. Moving deployment ownership into Aspire makes it practical to deploy Keycloak as part of the same application model instead of relying on an external Auth0 tenant.

The custom Admitto email theme must be available in production. Local bind mounts are not available in Azure Container Apps, so production cannot depend on `WithBindMount` for theme files. Custom login-page theming has been removed; production login pages use the stock `keycloak.v2` theme until a Keycloakify-based login theme is reintroduced.

The first production administrator must be able to enroll without a temporary password. Keycloak already supports account-action emails for passkey enrollment through SMTP, which is simpler to operate than a bespoke Admitto handoff for this narrow identity-provider-owned flow.

## Decision

Use Keycloak as the identity provider in production and local development.

Production Keycloak is configured as the passkey-only identity provider for Admin UI users. The deployment realm's Admin UI browser flow requires WebAuthn passwordless authentication and does not offer username/password fallback or password grant for Admin UI sign-in. Local development uses a separate local realm import that keeps seeded email/password users and direct-grant test clients.

Production Keycloak is deployed as a container app built from `src/Admitto.AppHost/KeycloakConfiguration/Dockerfile`. The image includes:

- the explicit deployment realm import from `AdmittoRealm.Deployment.json`, copied to `/opt/keycloak/data/import/admitto-realm.json`
- the custom email theme copied to `/opt/keycloak/themes/admitto`

Keycloak stores state in the `keycloak-db` database on the same Azure PostgreSQL Flexible Server used by the rest of the application.

Deployment-specific realm values use Keycloak environment substitution:

- `ADMITTO_UI_PUBLIC_URL` configures UI redirect URIs and web origins.
- `ADMITTO_UI_CLIENT_SECRET` configures the `admitto-ui` confidential client secret.
- `KEYCLOAK_SMTP_*` values configure Keycloak's own SMTP delivery for account-action emails.
- `keycloakAdminUser` and `keycloakAdminPassword` configure the initial Keycloak master-realm bootstrap administrator.

Local development uses `AdmittoRealm.Local.json`, which keeps local seed users and local-only clients such as `admitto-test-runner` and `admitto-scalar` out of the deployment import. Local Keycloak SMTP points at MailDev so account-action emails can still be inspected during development. The deployment import also omits exported local realm keys and uses environment-substituted production SMTP settings instead of MailDev.

Production Keycloak account-action emails, including bootstrap administrator passkey enrollment, are sent directly by Keycloak through its configured SMTP server. Admitto continues to trigger those emails by calling Keycloak's `execute-actions-email` Admin API endpoint with `webauthn-register-passwordless`, but Keycloak owns token generation, link expiry, template rendering, and SMTP delivery.

## Consequences

- The AppHost owns identity-provider deployment, realm import, and theme packaging.
- Production no longer needs Auth0 parameters or Auth0 Management API credentials.
- Keycloak availability and database durability become part of Admitto's production operations.
- Production passkey-only authentication is an explicit realm and AppHost responsibility; local password login remains an intentional development/test exception.
- Keycloak account-action email delivery depends on Keycloak SMTP parameters that are separate from Admitto application-email SMTP settings.
- Admitto's Email module no longer records or renders Keycloak account-action emails; troubleshooting uses Keycloak logs and SMTP provider logs.
