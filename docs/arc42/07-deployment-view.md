# 7. Deployment view

## 7.1 Local development (Aspire AppHost)

`Admitto.AppHost` orchestrates all dependencies for local development:

| Resource | Purpose |
| :------- | :------ |
| `api` | API host |
| `worker` | Worker host |
| `migrations` | Database migration runner |
| `postgres` | PostgreSQL with databases: `admitto-db`, `quartz-db`, `better-auth-db` |
| `keycloak` | Identity provider |
| `queues` | Azure Storage Queue emulator |
| `maildev` | Local SMTP server with web UI |

Start everything: `dotnet run --project src/Admitto.AppHost`

The local `keycloak` resource mounts the custom login theme from
`src/Admitto.AppHost/KeycloakConfiguration/themes/admitto` and the imported
`admitto` realm selects that theme via `loginTheme`. Because Keycloak runs with a
persistent data volume, an already-imported local realm may need the theme set in
the Keycloak admin console or a fresh Keycloak volume before the realm-import
setting is visible.

## 7.2 Production shape

- **API** and **Worker** deploy as separate containerized workloads.
- **Migrations** run as a deployment job (not a long-running process).
- PostgreSQL, queue service, SMTP, and identity provider are managed external dependencies.
- No service mesh or discovery needed — the API and Worker share the same database and queue.
- **Auth0** is the production identity provider, activated by the presence of the `Authentication:Auth0` configuration section. It provides passkey-only (WebAuthn) authentication and exposes the Management API for M2M user provisioning. See [ADR-011](../adrs/adr-011-auth0-passkeys.md).

<!-- TODO: add infrastructure diagram when production deployment is finalized -->
