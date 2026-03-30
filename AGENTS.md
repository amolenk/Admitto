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
dotnet test tests/Admitto.Module.Organization.Tests/Admitto.Module.Organization.Tests.csproj
dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Documentation Hygiene
- Architecture documentation lives in `docs/arc42/` (arc42 format, one file per chapter).
- If a structural decision changes, update the relevant arc42 chapter and ADRs in `docs/adrs/`.
- Link ADRs from `docs/arc42/09-architectural-decisions.md`.

## Feature Implementation Checklist

Before declaring a feature complete, verify every item:

- Read the full feature spec in `docs/specs/` (see `docs/specs/AGENTS.md` for interpretation rules).
- Each user story has a corresponding vertical slice folder under `Application/UseCases/{Feature}/`.
- Each slice follows the standard file layout (command/query, handler, endpoint, request, validator).
- Endpoint is wired in `{Module}ApiEndpoints.cs`.
- Each admin API endpoint has a corresponding CLI command in `src/Admitto.Cli/Commands/` (see `src/Admitto.Cli/AGENTS.md`).
- Each acceptance scenario (`SC-*`) has a corresponding test method with scenario ID prefix (`SC001_...`).
- Tests use fixture/builder patterns, not inline setup.
- All new and existing tests pass.
- Domain model changes are covered by domain-level tests.
