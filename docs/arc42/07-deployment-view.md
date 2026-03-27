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

## 7.2 Production shape

- **API** and **Worker** deploy as separate containerized workloads.
- **Migrations** run as a deployment job (not a long-running process).
- PostgreSQL, queue service, SMTP, and identity provider are managed external dependencies.
- No service mesh or discovery needed — the API and Worker share the same database and queue.

<!-- TODO: add infrastructure diagram when production deployment is finalized -->
