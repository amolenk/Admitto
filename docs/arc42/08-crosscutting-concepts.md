# 8. Cross-cutting concepts

## 8.1 Endpoint-owned unit of work

API endpoints — not command handlers — own the transaction boundary. The endpoint resolves a keyed `IUnitOfWork` for its module and calls `SaveChangesAsync` after the handler returns. This keeps handlers framework-agnostic and testable without persistence concerns.

**Rule:** Command handlers must not inject or call `IUnitOfWork`.

Reference: `IUnitOfWork` registered per module via `AddModuleDatabaseServices<TWriteModel, TDbContext>()` in `Admitto.Module.Shared/Infrastructure/DependencyInjection.cs`.

## 8.2 Validation

FluentValidation validators are discovered per module assembly and registered in DI. For admin endpoints, `ValidationFilter` (an endpoint filter) runs validation on all request DTO arguments before the handler executes. Invalid requests return a standard `ValidationProblem` response.

**Rule:** When the handler runs, the request DTO has already been validated.

Reference: `Admitto.Api/Middleware/ValidationFilter.cs`, applied in `Admitto.Api/Endpoints/AdminEndpoints.cs`.

## 8.3 Authentication and authorization

- **Authentication:** JWT Bearer tokens validated against a configurable authority. Challenge and forbidden responses return ProblemDetails.
- **Admin authorization:** `AdminAuthorizationRequirement` checked by `AdminAuthorizationHandler` via `IAdministratorRoleService`.
- **Team membership authorization:** `TeamMembershipAuthorizationRequirement` checked by `TeamMembershipAuthorizationHandler` via `IOrganizationFacade.GetTeamMembershipRoleAsync`. Admin users bypass team checks.

Endpoints declare requirements with `policy.RequireAdminRole()` or `policy.RequireTeamMembership(role)`.

## 8.4 Organization scope binding

`OrganizationScope` is a bindable record that resolves team and event identity from URL route parameters. It implements ASP.NET Core's `BindAsync` pattern, so endpoints can inject it directly. Resolution goes through `IOrganizationScopeResolver`, which queries the Organization facade and caches the result per request.

## 8.5 Messaging and outbox

Three event tiers, each with distinct scope:

| Tier | Scope | Persistence | Location |
| :--- | :---- | :---------- | :------- |
| Domain event | In-transaction, synchronous | Not persisted separately | `Domain/DomainEvents/` |
| Module event | Async, within/between modules | Outbox table | `Application/ModuleEvents/` |
| Integration event | Async, external contracts | Outbox table | `*.Contracts/IntegrationEvents/` |

**Why three tiers?** Domain events are dispatched synchronously within the same transaction — they don't need the message bus. When an event does need async processing, we distinguish between module events and integration events. Module events are defined inside the module's `Application/ModuleEvents/` folder, so internal workflows can evolve without affecting other modules. Integration events live in the module's Contracts project (`*.Contracts/IntegrationEvents/`) as a public, versioned surface for external consumers.

Each module declares a `MessagePolicy` that maps domain events to module and/or integration events. The `DomainEventsInterceptor` calls the policy during `SaveChanges`; mapped events are written to the outbox table in the same transaction. `OutboxDispatcher` attempts best-effort dispatch immediately, with background retry via the Worker host.

## 8.6 Error handling

### Pipeline

- `BusinessRuleViolationException` is thrown when a `ValidationResult<T>` is unwrapped on failure, or directly when a business rule is violated.
- `ApplicationErrorExceptionHandler` maps business exceptions to ProblemDetails responses.
- `GlobalExceptionHandler` catches unexpected exceptions.
- `IPostgresExceptionMapping` (keyed per module) maps database constraint violations to domain errors.
- Optimistic concurrency conflicts (`DbUpdateConcurrencyException`) are mapped to `ConcurrencyConflictError`.

### Error placement — three tiers

Errors are defined as close as possible to the code that throws them. Three tiers cover all cases:

| Tier | Where defined | Visibility | Used by | Examples |
| :--- | :------------ | :--------- | :------ | :------- |
| Shared helper | `Shared.Kernel/ErrorHandling/` | `public static` | Any layer | `NotFoundError.Create<T>()`, `AlreadyExistsError`, `ConcurrencyConflictError` |
| Entity-nested | Nested `Errors` class in entity or value object | `internal` | Methods of that entity/VO only | `User.Errors.UserAlreadyTeamMember`, `Coupon.Errors.NoTicketTypes` |
| Handler-local | Nested `Errors` class in handler | `internal` | That handler only | `CreateCouponHandler.Errors.EventNotActive` |

**Rules:**

1. An error is defined in the same class that throws it.
2. Entity-nested errors are for rules the entity validates inside its own methods (`Create`, `Revoke`, etc.).
3. Handler-local errors are for application-level checks the handler performs (e.g., cross-module lookups, precondition checks via facades).
4. If a pattern repeats across multiple entities or handlers (not-found, already-exists, concurrency), promote it to a shared helper in the kernel.
5. Visibility is `internal`, not `public`, so errors stay testable via `InternalsVisibleTo` without leaking to other modules.
6. Never add an error to an entity for a rule that the entity does not validate itself.

## 8.7 Persistence

- EF Core `DbContext` per module, each targeting a separate PostgreSQL schema.
- `AuditInterceptor` populates `CreatedAt`, `LastChangedAt`, `LastChangedBy` on auditable entities.
- `DomainEventsInterceptor` dispatches domain events and writes outbox messages during `SaveChanges`.
- Shared value converters for kernel types (`Slug`, `EmailAddress`, `TeamId`, etc.) in `Admitto.Module.Shared` (under `Infrastructure/`).

## 8.8 Observability

Service defaults (`Admitto.ServiceDefaults`) configure:

- OpenTelemetry tracing and metrics
- Health checks at `/health` and `/alive`
- Request timeouts
- Output caching

## 8.9 Vertical slice structure

Each use case lives in its own feature folder under `Application/UseCases/{Feature}/{Slice}/`. The table below shows the canonical files for an admin-facing command slice and which are optional.

| File | Purpose | Required |
| :--- | :------- | :------- |
| `{Slice}Command.cs` | Immutable record sent via `IMediator.SendAsync` | Always |
| `{Slice}Handler.cs` | Business logic; must not inject or commit `IUnitOfWork` | Always |
| `AdminApi/{Slice}HttpEndpoint.cs` | Minimal API endpoint; owns the UoW commit | When HTTP-exposed |
| `AdminApi/{Slice}HttpRequest.cs` | Inbound DTO with `ToCommand()` helper | When HTTP-exposed |
| `AdminApi/{Slice}Validator.cs` | FluentValidation validator for the request DTO | When HTTP-exposed |
| `EventHandlers/{Event}DomainEventHandler.cs` | Translates a domain event into the command and dispatches it | When triggered by a domain event |

Feature group naming mirrors the aggregate it modifies: `TeamManagement/`, `TicketedEvents/`, `Users/`.

### Domain-event-triggered slices

When a slice is triggered by a domain event rather than an HTTP request, the event handler lives in `EventHandlers/` inside the slice folder and is kept intentionally dumb — it translates the event into the slice's command and dispatches it via `IMediator`:

```
TeamManagement/
  RegisterTicketedEventCreation/
    EventHandlers/
      TicketedEventCreatedDomainEventHandler.cs   ← translates event → command
    RegisterTicketedEventCreationCommand.cs
    RegisterTicketedEventCreationHandler.cs        ← business logic
```

The owning aggregate's feature folder hosts the domain event handler (the aggregate that *reacts*), not the feature folder that *produced* the event.

### HTTP endpoint registration

All admin endpoints are wired in `{Module}ApiEndpoints.cs` via `MapXxx()` extension methods. Groups mirror the URL hierarchy:

```csharp
// /admin/teams
var teams = group.MapGroup("/teams");
teams.MapCreateTeam();     // POST /
teams.MapGetTeams();       // GET  /

// /admin/teams/{teamSlug}
var team = teams.MapGroup("/{teamSlug}");
team.MapGetTeam();         // GET  /
team.MapUpdateTeam();      // PUT  /
team.MapArchiveTeam();     // POST /archive
```

## 8.10 EF Core query rules

Several EF-specific gotchas discovered during development:

### Computed properties are not translatable

C# computed properties (e.g. `IsArchived => ArchivedAt.HasValue`) cannot be translated to SQL. LINQ queries must reference the backing column directly:

```csharp
// ❌ Runtime exception — EF cannot translate
.Where(t => !t.IsArchived)

// ✅ Correct
.Where(t => t.ArchivedAt == null)
```

### Value object comparisons in LINQ

EF value converters handle persistence, but LINQ predicates must use the full value object, not the inner primitive:

```csharp
// ❌ Not translatable
.Where(t => t.Id.Value == guid)

// ✅ Correct
.Where(t => t.Id == TeamId.From(guid))
```

### AsNoTracking for guard queries

Queries used only as precondition checks (e.g. "does an active event exist?") should use `.AsNoTracking()` to avoid polluting the EF change tracker with entities that will not be modified.

### Optimistic concurrency

The `Version` property on all aggregates (`[Timestamp]`, `uint`) is the EF row-version concurrency token. `DbSetExtensions.GetAsync(key, expectedVersion?)` validates the token on load and throws `ConcurrencyConflictError` on mismatch. Clients read the current version when fetching a resource and supply it on mutating operations.

## 8.11 Domain event dispatch — in-process pattern

`DomainEventsInterceptor` fires inside `SavingChangesAsync` (before the actual write), so domain event handlers run **within the same database transaction** as the triggering aggregate. This guarantees atomicity.

### EF change tracker reuse

When a command handler loads an aggregate and a domain event handler (running during save) loads the same aggregate, EF returns the already-tracked instance from the change tracker. No extra database round-trip occurs.

### Write-amplifier pattern for concurrency tokens

When one aggregate must protect against concurrent modifications triggered by another aggregate, increment a dedicated counter on the first aggregate whenever the second is modified. This forces a write to the first aggregate's row, advancing its EF `Version` token, so any concurrent operation holding the old token fails at commit.

Example: `Team.TicketedEventScopeVersion` is incremented by `RegisterTicketedEventCreationHandler` whenever a `TicketedEvent` is created under the team. This closes the TOCTOU window between the active-events guard in `ArchiveTeamHandler` and its commit.

### Fast-fail guard rule

Domain event handlers are not executed in the test `DatabaseTestContext` (no `DomainEventsInterceptor` registered). Business rules that need test coverage must therefore be enforced as an explicit guard in the command handler *before* the save, not solely in the domain event handler. The domain event handler provides defence in depth in production; the handler guard provides testability.

## 8.12 Handler and event handler DI registration

Command handlers, domain event handlers, module event handlers, and integration event handlers are all auto-discovered by assembly scan (`Scrutor`) — no manual registrations required. The scan uses `AssignableTo(typeof(ICommandHandler<>))` (and equivalents for other handler types) to find all concrete implementations regardless of folder.

**Rule:** Do not register handlers manually in `DependencyInjection.cs`. Place the class in any folder within the module assembly and implement the correct interface.

**Caveat:** The scan for domain/module/integration event handlers uses `AssignableTo(typeof(IXxxHandler<>))`, *not* `Where(t => t.IsGenericType …)`. The latter only matches open generic types (never a concrete handler) and is a known pitfall if the scan helper is extended.
