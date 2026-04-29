# Admitto Agent Guide

## Scope
This file applies to the entire repository. Nested `AGENTS.md` files in subdirectories provide stricter local guidance.

## Architecture Guardrails
Before proposing or implementing changes:
- Read `docs/arc42/` — treat it as the source of truth for constraints, decisions, and concepts.
- If a request conflicts with the docs, explain the conflict and propose an ADR, a code change, or both.

Key sections:
- `docs/arc42/05-building-block-view.md` — module structure
- `docs/arc42/06-runtime-view.md` — key runtime flows
- `docs/arc42/08-crosscutting-concepts.md` — patterns and conventions
- `docs/arc42/10-quality-requirements.md` — quality scenarios and test strategy

## Project Boundaries
- Use `Admitto.slnx` to determine active projects and module boundaries.
- Modules follow `Admitto.Module.*` naming (e.g. `Admitto.Module.Organization`, `Admitto.Module.Registrations`).
- Each module has one main project (`Domain/`, `Application/`, `Infrastructure/` folders) and a separate Contracts project.
- Shared code lives in `Admitto.Module.Shared` and `Admitto.Module.Shared.Kernel`.

### Aggregate Ownership
- `Admitto.Module.Organization` owns `Team` (and team membership). It only tracks ticketed event existence for slug/id resolution and team-archive guards.
- `Admitto.Module.Registrations` owns `TicketedEvent` (slug/name/dates, lifecycle, registration/cancellation/reconfirm policies), `TicketCatalog`, `Coupon`, and `Registration`. New per-event configuration belongs here.

## Non-Negotiable Conventions
- API endpoint handlers own the transaction boundary and commit the module unit of work.
- Command handlers must not inject or commit unit-of-work objects.
- Admin routes run FluentValidation in the endpoint filter before handler execution.
- Cross-module communication goes via contracts/facades, not cross-module DbContext access.
- Events must follow the domain/module/integration taxonomy in `docs/arc42/08-crosscutting-concepts.md`.

## Running the Application
This is a .NET Aspire project. **Never run `Admitto.Api` or other services directly.**

Start the full stack (API, Postgres, Keycloak, queues, etc.) using the Aspire CLI/skill:
```
aspire start
```
The Aspire dashboard shows the dynamic URL assigned to the `api` service.

## Regenerating the Admin UI SDK
When backend endpoints change, regenerate the generated SDK:
1. Start the Aspire AppHost.
2. Regenerate: `cd src/Admitto.UI.Admin && pnpm run openapi-ts`
3. Use the newly generated functions from `app/lib/admitto-api/generated/` in proxy routes.

## Testing
Run targeted tests for the modules you changed. See `tests/AGENTS.md` for commands and suite selection.

## Documentation Hygiene
See `docs/AGENTS.md` for doc update rules.

## Admin UI Design
Keep design of new features in line with existing features. The `design` directory contains the original UI design.

## Feature Implementation Checklist
Before declaring a feature complete:
- Read the full feature spec in `openspec/specs/` (view `openspec/specs/<capability>/spec.md` or use `openspec spec show <capability>`).
- Each user story maps to one primary slice or implementation unit whenever possible.
- HTTP-exposed slices: command/query, handler, endpoint, request/validator/response as needed.
- Internal event-driven work: event-handler pattern under `Application/UseCases/.../EventHandlers/`; jobs under `Application/Jobs/`.
- Endpoint wiring updated in the module's endpoint registration entry point.
- Each admin API endpoint has a corresponding CLI command in `src/Admitto.Cli/Commands/` (see `src/Admitto.Cli/AGENTS.md`).
- Each acceptance scenario (`SC-*`) has a corresponding test method with scenario ID prefix (`SC001_...`).
- Tests use fixture/builder patterns, not inline setup.
- All new and existing tests pass.
- Domain model changes are covered by domain-level tests.
