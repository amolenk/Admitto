# Admitto Agent Guide

## Scope
This file applies to the entire repository. Nested `AGENTS.md` files in subdirectories provide stricter local guidance.

## Architecture guardrails

Before you propose or implement changes:

- Read the arc42 documentation in `docs/arc42/`.
- Treat it as the source of truth for constraints, decisions, and concepts.
- If a request conflicts with the docs, do not "pick a side".
  Explain the conflict and propose a change to the docs (ADR), the code, or both.

Focus on these sections when implementing changes:
- `docs/arc42/05-building-block-view.md` — module structure
- `docs/arc42/06-runtime-view.md` — key runtime flows
- `docs/arc42/08-crosscutting-concepts.md` — patterns and conventions
- `docs/arc42/10-quality-requirements.md` — quality scenarios and test strategy

## Project Boundaries
- Use `Admitto.slnx` to determine active projects and module boundaries.
- Module projects follow the `Admitto.Module.*` naming convention (e.g. `Admitto.Module.Organization`, `Admitto.Module.Registrations`).
- Each module has one main project (with `Domain/`, `Application/`, `Infrastructure/` folders) and a separate Contracts project.
- Shared code lives in `Admitto.Module.Shared` and `Admitto.Module.Shared.Kernel`.

## Non-Negotiable Conventions
- API endpoint handlers own the transaction boundary and commit the module unit of work.
- Command handlers must not inject or commit unit-of-work objects.
- For admin routes, FluentValidation runs in the endpoint filter before endpoint handler execution.
- Cross-module communication should happen via contracts/facades, not cross-module DbContext access.
- Domain events, module events, and integration events must follow the taxonomy in `docs/arc42/08-crosscutting-concepts.md`.

## Testing Expectations
- Run targeted tests for the modules you changed.
- Run broader suites when shared components or host wiring are changed.

Suggested commands:
```bash
dotnet test tests/Admitto.Module.Organization.Domain.Tests/Admitto.Module.Organization.Domain.Tests.csproj
dotnet test tests/Admitto.Module.Organization.Tests/Admitto.Module.Organization.Tests.csproj
dotnet test tests/Admitto.Module.Registrations.Domain.Tests/Admitto.Module.Registrations.Domain.Tests.csproj
dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Running the Application

This is a .NET Aspire project. **Never run `Admitto.Api` or other services directly.**

To start the full stack (API, Postgres, Keycloak, queues, etc.), ask the user to run:

```
! dotnet run --project src/Admitto.AppHost/Admitto.AppHost.csproj
```

The Aspire dashboard will show the dynamic URL assigned to the `api` service.

### Regenerating the Admin UI SDK

When backend endpoints change (new or updated), regenerate the generated SDK:

1. Ask the user to start the AppHost (above) and provide the `api` service URL.
2. Fetch the fresh spec: `curl <api-url>/openapi/v1.json -o src/Admitto.UI.Admin/openapi-spec.json`
3. Regenerate: `cd src/Admitto.UI.Admin && pnpm run openapi-ts`
4. Use the newly generated functions from `app/lib/admitto-api/generated/` in proxy routes.
5. **Do not use `proxyAdmittoApi` for endpoints that exist in the generated SDK.**

## Documentation Hygiene
- Architecture documentation lives in `docs/arc42/` (arc42 format, one file per chapter).
- If a structural decision changes, update the relevant arc42 chapter and ADRs in `docs/adrs/`.
- Link ADRs from `docs/arc42/09-architectural-decisions.md`.

## Admin UI Design

- Keep design of new features in line with existing features.
- The `design` directory contains the original UI design. Use it when designing UI for new/changed features.

## Feature Implementation Checklist

Before declaring a feature complete, verify every item:

- Read the full feature spec in `openspec/specs/` (use `openspec spec show <capability>` or view the file directly).
- Each user story maps to one primary slice or implementation unit whenever possible. Follow only documented exceptions.
- HTTP-exposed slices follow the standard file layout where applicable: command/query, handler, endpoint, and request/validator/response types as needed by the surface.
- Internal event-driven work follows the event-handler pattern under `Application/UseCases/.../EventHandlers/`, and jobs live under `Application/Jobs/`.
- Endpoint wiring is updated in the module's endpoint registration entry point.
- Each admin API endpoint has a corresponding CLI command in `src/Admitto.Cli/Commands/` (see `src/Admitto.Cli/AGENTS.md`).
- Each acceptance scenario (`SC-*`) has a corresponding test method with scenario ID prefix (`SC001_...`).
- Tests use fixture/builder patterns, not inline setup.
- All new and existing tests pass.
- Domain model changes are covered by domain-level tests.
