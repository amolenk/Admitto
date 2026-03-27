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

- `BusinessRuleViolationException` is thrown when a `ValidationResult<T>` is unwrapped on failure.
- `ApplicationErrorExceptionHandler` maps business exceptions to ProblemDetails responses.
- `GlobalExceptionHandler` catches unexpected exceptions.
- `IPostgresExceptionMapping` (keyed per module) maps database constraint violations to domain errors.
- Optimistic concurrency conflicts (`DbUpdateConcurrencyException`) are mapped to `ConcurrencyConflictError`.

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
