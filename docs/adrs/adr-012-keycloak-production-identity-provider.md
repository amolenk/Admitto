# ADR-012: Keycloak as the production identity provider

## Status

Accepted. Supersedes [ADR-011](adr-011-auth0-passkeys.md).

## Context

Admitto already uses Keycloak for local development and tests. Moving deployment ownership into Aspire makes it practical to deploy Keycloak as part of the same application model instead of relying on an external Auth0 tenant.

The custom Admitto login theme must be available in production. Local bind mounts are not available in Azure Container Apps, so production cannot depend on `WithBindMount` for theme files.

## Decision

Use Keycloak as the identity provider in production and local development.

Production Keycloak is deployed as a container app built from `src/Admitto.AppHost/KeycloakConfiguration/Dockerfile`. The image includes:

- the explicit deployment realm import from `AdmittoRealm.Deployment.json`, copied to `/opt/keycloak/data/import/admitto-realm.json`
- the custom login theme copied to `/opt/keycloak/themes/admitto`

Keycloak stores state in the `keycloak-db` database on the same Azure PostgreSQL Flexible Server used by the rest of the application.

Deployment-specific realm values use Keycloak environment substitution:

- `ADMITTO_UI_PUBLIC_URL` configures UI redirect URIs and web origins.
- `ADMITTO_UI_CLIENT_SECRET` configures the `admitto-ui` confidential client secret.
- `keycloakAdminUser` and `keycloakAdminPassword` configure the initial Keycloak master-realm bootstrap administrator.

Local development uses `AdmittoRealm.Local.json`, which keeps local seed users and local-only clients such as `admitto-test-runner` and `admitto-scalar` out of the deployment import. The deployment import also omits exported local realm keys and MailDev SMTP settings.

## Consequences

- The AppHost owns identity-provider deployment, realm import, and theme packaging.
- Production no longer needs Auth0 parameters or Auth0 Management API credentials.
- Keycloak availability and database durability become part of Admitto's production operations.
- Passkey-only authentication is no longer assumed by the architecture unless configured in the Keycloak realm.
