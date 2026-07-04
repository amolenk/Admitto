## Context

All event-scoped API routes are nested under `/teams/{teamId}/events/{eventId}`. The `TeamMembershipAuthorizationHandler` validates that the authenticated user belongs to the team identified by `teamId` in the route — this is the only team-level check that currently happens. No handler (command or query) verifies that `eventId` actually belongs to `teamId`. A user on Team A can make requests using their own valid `teamId` and Team B's `eventId`, bypassing the intended event isolation.

The fix pattern is already supported by the existing `DbSetExtensions.GetAsync(predicate)` infrastructure and by the fact that most affected entities already store `TeamId` in the database. The one exception is `BadgesEvent`, which is a projection and currently only stores `event_id` and `status`.

## Goals / Non-Goals

**Goals:**
- Every handler that loads an event-scoped aggregate SHALL reject the request if the aggregate does not belong to the `teamId` supplied in the route.
- Use a uniform, minimal-ceremony pattern that is consistent across all modules.
- Return 404 (not 403) for cross-team access attempts — do not reveal whether an event exists under a different team.
- Propagate `teamId` into all commands and queries that currently ignore it.

**Non-Goals:**
- Changing the API surface (routes, request/response shapes remain the same).
- Introducing new authorization middleware or a cross-cutting interceptor — enforcement stays in handlers.
- Addressing scope enforcement for non-event-scoped endpoints (team management, email settings, etc.) — those are already correct or out of scope.

## Decisions

### 1. Enforce at the handler level, not middleware

**Decision**: Handlers are the enforcement point. The API endpoints pass `teamId` into the command/query; the handler includes it in the load predicate.

**Rationale**: A middleware or authorization attribute cannot easily access the loaded aggregate to compare `TeamId` without re-fetching it. Handler-level enforcement is already the pattern for other business rule violations (they throw `BusinessRuleViolationException`), keeps the check co-located with the load, and requires no new infrastructure. Middleware would couple routing concerns to domain logic.

**Alternative considered**: A generic `ITeamEventScopeValidator` service injected into handlers. Rejected — adds indirection without benefit; the predicate in `GetAsync` is already one line.

### 2. Use predicate-based `GetAsync` for direct TeamId owners

**Decision**: For aggregates that store `TeamId` directly (`TicketedEvent`, `Coupon`, `Registration`, `BulkEmailJob`, `BadgesEvent` after migration), use the existing predicate overload:

```csharp
GetAsync(e => e.Id == id && e.TeamId == teamId, ct)
```

If no row matches (wrong team or not found), `GetAsync` throws `BusinessRuleViolationException` → 404. No additional null check required.

**Rationale**: Already supported. One-line change per handler. Consistent with existing usage in `GetRegistrationDetailsHandler` and `GetRegistrationsHandler`.

### 3. Add `TeamId` to `TicketCatalog` (migration required)

**Decision**: Add `TeamId` to `TicketCatalog` and use the same predicate pattern as all other aggregates:

```csharp
GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, ct)
```

**Rationale**: `Waitlist` already stores `TeamId` directly (already mapped in DB). Giving `TicketCatalog` the same treatment makes the enforcement pattern uniform across every aggregate — no special-casing, no parent-verify detour, no extra query. This also enables the ArchTest (Decision 5) to cover `TicketCatalog` handlers without exceptions.

**Migration safety**: `TicketedEvent` and `TicketCatalog` are created in the same integration-event handler and always share the same `TeamId`. Backfill with a single SQL join: `UPDATE ticket_catalogs SET team_id = te.team_id FROM registrations.ticketed_events te WHERE ticket_catalogs.id = te.id`.

### 4. Add `TeamId` to `BadgesEvent` (migration required)

**Decision**: Add a `team_id` column (non-nullable `uuid`) to the `badges_events` table. Populate existing rows from `registrations.ticketed_events` via a data migration. Propagate `TeamId` from `TicketedEventCreatedIntegrationEvent` through `CreateBadgesEventCommand` → `CreateBadgesEventHandler` → `BadgesEvent.Create()`.

**Rationale**: `TicketedEventCreatedIntegrationEvent` already carries `TeamId`. The badges module has no other path to team ownership. Without the column, all badge handlers would need a cross-module join or a service call — unacceptable coupling.

**Migration safety**: Existing rows in `badges_events` can be backfilled with a single SQL statement joining to `registrations.ticketed_events` (same database). The column is added nullable first, backfilled, then made non-nullable — standard safe migration pattern.

## Risks / Trade-offs

**[Risk] BadgesEvent migration backfill fails if a `badges_events` row has no matching `ticketed_events` row** → In practice this cannot happen (projection is created synchronously from the integration event); add a `NOT NULL` constraint violation check in the migration and fail fast if orphans exist.

**[Risk] New `teamId` parameter in commands/queries is a silent no-op if a handler accidentally omits it** → Mitigated by the ArchTest in Decision 5: any `GetAsync` predicate containing `EventId` without `TeamId` fails CI immediately.

## Decision 5. ArchTest safety net for handler scope enforcement

**Decision**: Add a Roslyn-based architecture test to `Admitto.Core.ArchTests` that scans every handler source file and asserts: any `GetAsync` lambda predicate that filters by `EventId` or `TicketedEventId` must also filter by `TeamId`.

**Rationale**: Handler-level enforcement is the chosen pattern, but it is convention-based — nothing in the type system prevents a new handler from including `EventId` in its predicate while omitting `TeamId`. The ArchTest converts this convention into a CI gate that fails immediately on the first violation, without requiring any new runtime infrastructure.

**Implementation**: Uses `Microsoft.CodeAnalysis.CSharp` (already a dependency in `Admitto.Core.ArchTests`) to parse handler `.cs` files, locate `GetAsync` invocations, extract lambda arguments, and verify the lambda body contains `TeamId` whenever it contains `EventId`.

**Coverage**: Covers all modules uniformly. Because `TicketCatalog` and `Waitlist` now store `TeamId` directly (Decisions 3 and the pre-existing `Waitlist.TeamId`), no aggregate requires a special-case exemption.

## Migration Plan

1. Generate EF Core migration for `Admitto.Core` (Badges schema): add `team_id uuid NULL` to `badges_events`.
2. Add a raw SQL step in the migration to backfill: `UPDATE badges_events SET team_id = te.team_id FROM registrations.ticketed_events te WHERE badges_events.event_id = te.id`.
3. Add a check that no rows remain with `team_id IS NULL` (orphan guard).
4. Alter column to `NOT NULL`.
5. Deploy — standard rolling deploy; no API changes, no client impact.

**Rollback**: Drop the `team_id` column. Application code must be reverted simultaneously (the column is referenced in the predicate).
