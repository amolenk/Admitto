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
- All domain modules live as folders inside the single `Admitto.Core` project: `Organization/`, `Registrations/`, `Email/`, and `Shared/`.
- Each module folder contains `Domain/`, `Application/`, and `Infrastructure/` sub-folders. Modules that expose cross-module contracts also have a `Contracts/` sub-folder (Organization, Registrations, Shared).
- Shared utilities and primitives live in `Admitto.Core/Shared/`, with a `Kernel/` sub-folder for the lowest-level building blocks.

### Aggregate Ownership
- The **Organization module** (`Admitto.Core/Organization`) owns `Team` (and team membership). It only tracks ticketed event existence for slug/id resolution and team-archive guards.
- The **Registrations module** (`Admitto.Core/Registrations`) owns `TicketedEvent` (slug/name/dates, lifecycle, registration/cancellation/reconfirm policies), `TicketCatalog`, `Coupon`, and `Registration`. New per-event configuration belongs here.

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

When working in a worktree or another concurrent agent session, prefer:
```
aspire start --isolated
```

Before relying on the API endpoint, wait for it explicitly:
```
aspire wait api
aspire describe
```

In this Codex environment, sandboxed `curl` to the Aspire-published `localhost` endpoint can fail even when Aspire reports the resource as healthy. If you need to verify the live API spec or probe the running service, retry the local `curl` outside the sandbox and fetch `/openapi/v1.json` from the URL reported by `aspire describe`.

## Regenerating the Admin UI SDK

**⚠️ NON-NEGOTIABLE: Never write a manual proxy route or hand-code an API client call when the backend endpoint already exists or was just added. ALWAYS regenerate the SDK first and use the generated function.**

Whenever a backend endpoint is added, removed, or its contract changes, regenerate the Admin UI SDK **before** writing any proxy route or UI code that calls it:

1. `aspire start --isolated`
2. `aspire wait api`
3. Fetch the spec: `curl -sf http://<api-url>/openapi/v1.json -o src/Admitto.UI.Admin/openapi-spec.json`
4. Regenerate: `cd src/Admitto.UI.Admin && pnpm openapi-ts`
5. Use the newly generated functions from `app/lib/admitto-api/generated/` in proxy routes.

Proxy routes must always use the generated SDK — pattern:

```ts
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { myGeneratedFunction } from "@/lib/admitto-api/generated/sdk.gen";
import type { MyRequestType } from "@/lib/admitto-api/generated/types.gen";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; ... }> },
) {
    const { teamSlug, ... } = await params;
    const body = await request.json() as MyRequestType;
    return callAdmittoApi(() => myGeneratedFunction({ path: { teamSlug, ... }, body }));
}
```

If generation is blocked, fix the Aspire/spec access problem first. **Do not add handwritten replacements as a shortcut — not even temporarily.**

## Testing
Run targeted tests for the modules you changed. See `tests/AGENTS.md` for commands and suite selection.

**Always run architecture tests first:**
```bash
dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj
```
If ArchTests fail, fix the architectural violation before running other test suites.

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
- Tests use fixture/builder patterns, not inline setup.
- All new and existing tests pass.
- Domain model changes are covered by domain-level tests.
