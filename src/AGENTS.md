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
- Use module write-store abstractions (`IOrganizationWriteStore`, `IRegistrationsWriteStore`) in handlers.
- Keep data ownership inside module boundaries (schema-per-module).
- Resolve `IUnitOfWork` by module key (`OrganizationModuleKey.Value`, `RegistrationsModule.Key`).

## Messaging and Events Rule
- Domain events live in `Domain/DomainEvents/` within each module project.
- Module events live in `Application/ModuleEvents/` within each module project.
- Integration events live in `*.Contracts` projects under `IntegrationEvents/`.
- Map domain events via module `*MessagePolicy` classes; do not hand-roll ad hoc event translation in handlers.

## Cross-Module Rule
- Cross-module reads should go through contracts/facades (for example `IOrganizationFacade`), not direct DbContext access across modules.

## Feature Implementation Workflow

When implementing a feature:

### 1. Read the Spec First
Read the full feature spec at `openspec/specs/<capability>/spec.md` (or use `openspec spec show <capability>`).

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

### 5. Wire the Endpoint
Register the endpoint in the module's endpoint registration entry point.

### Canonical Examples
- **Command:** `UseCases/TeamManagement/CreateTeam/` in `Admitto.Module.Organization`
- **Query:** `UseCases/TeamManagement/GetTeam/` in `Admitto.Module.Organization`

## When You Change Architecture
- Update the relevant chapter in `docs/arc42/`.
- If the change is an architecture decision, add or update an ADR in `docs/adrs/`.
