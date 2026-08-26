# Source Code Agent Guide

## Scope
This file applies to `/src`.

## HTTP and Use Case Rules
- Keep minimal API endpoints feature-sliced (`UseCases/.../*HttpEndpoint.cs`).
- Use explicit `string teamSlug` / `string eventSlug` path parameters and `IOrganizationScopeResolver` for route-derived organization context.
- For write endpoints:
  - map request DTO to command
  - dispatch via `IMediator`
  - commit the keyed module `IUnitOfWork` in the endpoint
- Do not commit transactions inside individual command handlers.

## Validation Rule
- Admin routes run `ValidationFilter` at the route group level.
- Endpoint handlers should assume validated request DTOs on admin routes and avoid duplicate validation logic.

## Persistence Rule
- Use module write-store abstractions (`IOrganizationWriteStore`, `IRegistrationsWriteStore`, `IEmailWriteStore`) in handlers that mutate aggregates.
- Use module read-store abstractions (`IRegistrationsReadStore`, `IEmailReadStore`) for persisted application projections/read models.
- Each persisted `DbSet` lives on exactly one store abstraction: aggregates on the write store, projections/read models on the read store. Do not expose the same `DbSet` on both.
- Projections are written through the read store too: a `*Projector` (or projection upsert) mutates projection rows via the read store, not the write store.
- Keep data ownership inside module boundaries (schema-per-module).
- Resolve `IUnitOfWork` by module key (`OrganizationModuleKey.Value`, `RegistrationsModule.Key`, `EmailModule.Key`).

## Messaging and Events Rule
- Domain events live in `Domain/DomainEvents/` within each module project.
- Module events live in `Application/ModuleEvents/` within each module project.
- Integration events live in `*.Contracts` projects under `IntegrationEvents/`.

## Cross-Module Rule
- Cross-module reads should go through contracts/facades (for example `IOrganizationFacade`), not direct DbContext access across modules.

## Feature Implementation Workflow

When implementing a feature:

### 1. Read Existing Tests First
Read the existing tests for the affected module/use case before changing code. Ensure new or touched tests include the three-line Given/When/Then scenario comments required by `tests/AGENTS.md`.

### 2. Use Existing Capability Grouping First
The top-level folder under `Application/UseCases/` should extend the existing
capability grouping when one already fits the feature. Create a new grouping only
when no established structure fits cleanly.

```
Application/UseCases/
├── TeamManagement/          # FEAT-001 Team Management
│   ├── CreateTeam/
│   │   ├── CreateTeamCommand.cs
│   │   ├── CreateTeamHandler.cs
│   │   └── AdminApi/
│   │       ├── CreateTeamHttpEndpoint.cs
│   │       ├── CreateTeamHttpRequest.cs
│   │       └── CreateTeamValidator.cs
│   ├── GetTeam/
│   │   ├── GetTeamQuery.cs
│   │   ├── GetTeamHandler.cs
│   │   └── AdminApi/
│   │       └── GetTeamHttpEndpoint.cs
│   └── ListTeams/
│       └── ...
├── EventManagement/         # FEAT-003 Event Management
│   └── ...
```

### 3. One User Story → One Subfolder
Each user story (`US-*`) in the spec should become its own primary subfolder
whenever possible. Do not merge multiple user stories into a single handler unless
the spec or existing architecture explicitly documents the exception.

### 4. Standard Slice Files
HTTP-exposed use case subfolders typically contain:
- `{Name}Command.cs` or `{Name}Query.cs` — the request object
- `{Name}Handler.cs` — the business logic (must NOT commit `IUnitOfWork`)
- `AdminApi/` (or `Public/`) subfolder with:
  - `{Name}HttpEndpoint.cs` — maps route, dispatches, commits `IUnitOfWork`
  - `{Name}HttpRequest.cs` — DTO with `ToCommand()` / `ToQuery()` mapper when the endpoint needs an inbound DTO
  - `{Name}Validator.cs` — FluentValidation rules for the request DTO when validation is required

Internal event-driven slices omit the HTTP folder and keep event translation in
`EventHandlers/`. Jobs live under `Application/Jobs/`.

Application projections/read models derived from domain events live under
`Application/Projections/{ProjectionName}/`. A synchronous multi-event projection
may use a `*Projector` class that implements the relevant `IDomainEventHandler<T>`
interfaces directly, without a command slice or Inbox processing.

### 6. Domain Event Handler Naming
Domain event handler classes are named after the **domain event they handle**, not the
side-effect they produce. The intent is clear from the use-case folder.

✅ `TicketsChangedDomainEventHandler` in `WriteActivityLog/EventHandlers/`  
❌ `WriteTicketsChangedActivityLogHandler` — describes the side-effect instead

For multi-event application projections, use role-based `*Projector` naming instead
of per-event forwarding handlers.

### 5. Wire the Endpoint
Register the endpoint in the module's endpoint registration entry point.

### Canonical Examples
- **Command:** `UseCases/TeamManagement/CreateTeam/` in `Admitto.Core` (Organization module)
- **Query:** `UseCases/TeamManagement/GetTeam/` in `Admitto.Core` (Organization module)

## When You Change Architecture
- Update the relevant chapter in `docs/arc42/`.
- If the change is an architecture decision, add or update an ADR in `docs/adrs/`.
