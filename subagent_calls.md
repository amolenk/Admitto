2026-04-02T08:53:28Z | feature-backend-coder | Implement FEAT-001 Team Management backend in /Users/amolenk/Code/amolenk/Admitto. Read AGENTS.md, docs/specs/AGENTS.md, docs/specs/FEAT-001-team-management.md, and arc42 chapters 5, 6, 8, and 10. Audit current implementation, then add any missing backend functionality for US-001 through US-006 under the Organization module only: domain behavior, application slices, API endpoints, persistence/schema changes, endpoint wiring, and any required cross-module serialization with ticketed event creation. Follow project conventions: one user story per use-case folder, handlers do not commit unit of work, validators for admin requests, no direct cross-module DbContext access, no tests. If schema changes are needed, use the EF workflow rather than hand-editing migrations. Build the touched backend surface and summarize changed files, build result, and any blockers.
2026-04-02T08:54:49Z | feature-tester | Prepare to write FEAT-001 Team Management tests after backend and CLI functionality are in place. Read docs/specs/FEAT-001-team-management.md, docs/specs/AGENTS.md, tests/AGENTS.md, and arc42 chapter 10. Do not start coding yet; wait for completed implementation context and then map SC-001 through SC-016 to integration/API tests with fixtures and scenario-prefixed names.
2026-04-02T09:31:29Z | feature-backend-coder | Implement remaining FEAT-001 Team Management backend in /Users/amolenk/Code/amolenk/Admitto.

## Context

FEAT-001 spec is at docs/specs/FEAT-001-team-management.md. Read it fully before starting.

Previous work has already implemented: CreateTeam, GetTeam, GetTeamId, UpdateTeam, CreateTicketedEvent.
A bug in CreateTeamHttpEndpoint has already been fixed: MapPost("/teams", ...) → MapPost("/", ...).

## What still needs implementing

### 1. ListTeams use case (US-003 + US-006 merged by design)

Design decision: a **single** `GET /admin/teams` endpoint with role-based filtering.
- Admin callers → return all active (non-archived) teams
- Non-admin authenticated callers → return only the teams they are a member of (active only)
- Same DTO for both cases: slug, name, emailAddress, version (same fields as TeamDto)
- Excluded archived teams in both cases (FR-006, SC-013)

Put files under: `src/Admitto.Module.Organization/Application/UseCases/Teams/GetTeams/`
- `GetTeamsQuery.cs` — record with `Guid CallerId` and `bool CallerIsAdmin`
- `GetTeamsHandler.cs` — IQueryHandler returning `IReadOnlyList<TeamListItemDto>`
- `TeamListItemDto.cs` — record with Slug, Name, EmailAddress, Version
- `AdminApi/GetTeamsHttpEndpoint.cs` — GET "/", requires auth (any authenticated user); inject `IUserContextAccessor` to get caller user ID; inject `IAdministratorRoleService` to check admin status; map `RequireAuthorization()` (no specific role); return `Ok<IReadOnlyList<TeamListItemDto>>`

For the handler:
- If CallerIsAdmin: query `writeStore.Teams.AsNoTracking().Where(t => !t.IsArchived).Select(...)`
- If not: join through `writeStore.Users` to get the caller's memberships, then inner-join to active teams

IAdministratorRoleService is in `src/Admitto.Api/Auth/`. IUserContextAccessor is in `src/Admitto.Module.Shared/Application/Auth/`.

### 2. ArchiveTeam use case (US-005)

- `ArchiveTeamCommand.cs` — record with `Guid TeamId`, `uint ExpectedVersion`
- `ArchiveTeamHandler.cs` — gets team with expected version; checks for active ticketed events (EventWindow.End > now); if any exist throws BusinessRuleViolationException with a `team.has_active_events` error; calls `team.Archive(DateTimeOffset.UtcNow)`
- `AdminApi/ArchiveTeamHttpRequest.cs` — record with `uint ExpectedVersion`; `ToCommand(Guid teamId)`
- `AdminApi/ArchiveTeamHttpEndpoint.cs` — POST "/archive" on the `/{teamSlug}` group; RequireTeamMembership(Owner); commits unit of work; returns Ok
- No validator needed (only field is ExpectedVersion which is a non-negative uint)

Put files under: `src/Admitto.Module.Organization/Application/UseCases/Teams/ArchiveTeam/`

### 3. Wire both into OrganizationApiEndpoints.cs

Current structure (after the CreateTeam fix):
```csharp
group
    .MapGroup("/teams")
    .MapCreateTeam()         // POST /admin/teams
    .MapGroup("/{teamSlug}")
    .MapAssignTeamMembership()
    .MapGetTeam()            // GET /admin/teams/{teamSlug}
    .MapUpdateTeam()         // PUT /admin/teams/{teamSlug}
    .MapGroup("/events")
    .MapCreateTicketedEvent()
    .MapGroup("/{eventSlug}");
```

Add:
- `.MapGetTeams()` on the `/teams` group (chained before the `MapGroup("/{teamSlug}")`) → `GET /admin/teams`
- `.MapArchiveTeam()` on the `/{teamSlug}` group (alongside MapGetTeam, MapUpdateTeam) → `POST /admin/teams/{teamSlug}/archive`

## Conventions to follow

- Handlers are registered via DI (the Application/DependencyInjection.cs already wires them via assembly scan or explicit registration — check that file and add if needed).
- Handlers do NOT commit unit of work — that's the endpoint's job.
- Use `writeStore.Teams.GetAsync(TeamId.From(...), command.ExpectedVersion, cancellationToken)` for version-aware gets.
- For active events check in ArchiveTeam: `writeStore.TicketedEvents.AsNoTracking().AnyAsync(e => e.TeamId == teamId && e.EventWindow.End > now, cancellationToken)`
- Error for active events: `new BusinessRuleViolationException(new Error("team.has_active_events", "The team has active ticketed events.", Type: ErrorType.Validation))`
- The `IsArchived` property on Team is `ArchivedAt.HasValue` — use it for filtering
- GlobalUsings in Application already has `using Microsoft.EntityFrameworkCore` so no need to add it

## After implementing

Run `dotnet build src/Admitto.Module.Organization/Admitto.Module.Organization.csproj` and fix any errors.
Also run `dotnet build src/Admitto.Api/Admitto.Api.csproj` since the endpoint wiring touches the API.
Report: list of files created/modified, build result, any blockers.
2026-04-02T09:31:29Z | feature-tester | Write FEAT-001 Team Management tests — see prompt below.
2026-04-02T09:31:29Z | feature-reviewer | Review FEAT-001 Team Management implementation — see prompt below.
