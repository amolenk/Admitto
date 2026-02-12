# Admitto AI Instructions

Admitto is an open-source ticketing system for small, free events. It is a modular monolith built with .NET 10 and organized around explicit modules and multiple hosts (API + Worker).

Architecture references
- arc42: `docs/arc42/README.md`
- ADRs: `docs/adrs/`

## Repository layout

- `src/Admitto.Api`: Minimal API host.
- `src/Admitto.Worker`: Background processing host.
- `src/Admitto.Migrations`: Database migration runner.
- `src/Admitto.AppHost`: .NET Aspire orchestration for local dev.
- `src/Admitto.Cli`: Admin/ops CLI.
- `src/Admitto.UI.Admin`: Next.js admin UI.
- `src/Admitto.Application`, `src/Admitto.Domain`, `src/Admitto.Infrastructure`: Core application, domain, and infrastructure.
- `src/Admitto.Organization.*`: Organization module.
- `src/Admitto.Registrations.*`: Registrations module.
- `src/Admitto.Shared.*`: Shared cross-cutting code.
- `tests/`: Test projects.

## Key technical constraints

- .NET 10 SDK pinned in `global.json`.
- Modular monolith with multiple hosts per ADR-001.
- Minimal APIs with feature-sliced endpoints per ADR-002.
- PostgreSQL with schema-per-module; EF Core for persistence.
- FluentValidation for validation.
- Azure Storage Queues and outbox pattern for messaging.
- Quartz for scheduled work (via `quartz-db`).

## Local development

- Restore/build:
  - `dotnet restore`
  - `dotnet build --no-restore`
- Run API host:
  - `dotnet run --project src/Admitto.Api`
- Run Worker host:
  - `dotnet run --project src/Admitto.Worker`
- Run migrations:
  - `dotnet run --project src/Admitto.Migrations`
- Orchestrated local dev (requires Docker):
  - `dotnet run --project src/Admitto.AppHost`
- Admin UI:
  - `cd src/Admitto.UI.Admin`
  - `pnpm install`
  - `pnpm dev`

## Configuration touchpoints

- Connection strings: `admitto-db`, `quartz-db`, `better-auth-db`.
- API auth authority: `AUTHENTICATION__BEARER__AUTHORITY`.
- Identity providers: Keycloak or Microsoft Graph (API); BetterAuth (UI).

## Implementation conventions

- Keep module boundaries intact; use module `Contracts` projects for cross-module DTOs.
- Add endpoints next to their use cases (feature slicing).
- Use domain entities and value objects from the Domain projects.
- Use FluentValidation validators for request validation.
- Prefer outbox + queue for async workflows.

## Solutions and tests

- Solution file: `Admitto.slnx`.
- Example test runs:
  - `dotnet test tests/Admitto.Api.Tests`
  - `dotnet test tests/Admitto.Organization.Application.Tests`
  - `dotnet test tests/Admitto.Registrations.Domain.Tests`
