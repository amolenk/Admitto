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

## 8.4 Organization scope resolution and cross-module facades

Admin endpoints declare `teamSlug` and `eventSlug` as explicit path parameters in their handler signatures. An `IOrganizationScopeResolver` is injected to translate slugs into IDs via the Organization facade. The resolver returns an `OrganizationScope` record containing the resolved team/event identity and caches the result per request.

Endpoints call `scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken)` to obtain the scope. The resolver itself is HTTP-agnostic — it receives slugs from its callers rather than extracting them from route values. This keeps path parameters visible to the OpenAPI generator and makes the resolver testable without an HTTP context.

The `TeamMembershipAuthorizationHandler` extracts `teamSlug` from `HttpContext.GetRouteValue()` because authorization runs before endpoint binding.

### Synchronous cross-module facades

Some workflows need to consult another module's state inside the same request without going through the outbox. We expose these as **facade interfaces** in the target module's `*.Contracts` project, implemented inside the module proper. Callers depend only on the interface.

| Facade | Module | Used by | Purpose |
| :----- | :----- | :------ | :------ |
| `IOrganizationFacade` | Organization | Registrations, API auth | Resolve team/event slugs → IDs, check team membership |
| `IEventEmailFacade` | Email | Registrations | Check whether per-event SMTP credentials are configured before allowing registration to open |

Facades are read-only and side-effect-free. Cross-module *writes* still go through module/integration events on the outbox (see §8.6).

## 8.5 Use case slice layout

Each use case lives in a vertical slice folder under `Application/UseCases/{FeatureGroup}/{SliceName}/`.
The top-level group follows the module's established capability structure. Prefer
extending an existing group when it already fits the feature cleanly. Create a new
group only when no established structure fits.

One user story should map to one slice whenever possible. If a spec intentionally
merges behavior, document the exception in the spec or architecture record rather
than relying on an implicit convention.

### Standard HTTP-exposed slice

```
UseCases/TeamManagement/
  CreateTeam/
    CreateTeamCommand.cs
    CreateTeamHandler.cs
    AdminApi/
      CreateTeamHttpRequest.cs
      CreateTeamHttpEndpoint.cs
      CreateTeamValidator.cs
```

| File | Purpose | Required |
| :--- | :------- | :------- |
| `{Slice}Command.cs` / `{Slice}Query.cs` | Immutable record sent via `IMediator` | Always |
| `{Slice}Handler.cs` | Business logic; must not inject or commit UoW | Always |
| `{Surface}/{Slice}HttpEndpoint.cs` | Minimal API endpoint; owns the UoW commit. `Surface` follows the established module convention, such as `AdminApi/` or `Public/`. | When HTTP-exposed |
| `{Surface}/{Slice}HttpRequest.cs` | Inbound DTO with `ToCommand()` or `ToQuery()` helper | When the endpoint accepts structured input |
| `{Surface}/{Slice}Validator.cs` | FluentValidation validator for the request DTO | When the endpoint uses a validated request DTO |
| `EventHandlers/{Event}DomainEventHandler.cs` | Translates a domain event into the slice command | When triggered by domain event |

### Domain-event-triggered (internal) slice

Slices triggered by domain events omit the `AdminApi/` subfolder. The event handler lives in `EventHandlers/` inside the slice folder and is kept intentionally **dumb** — it only translates the event into the slice's command and dispatches it via `IMediator`. All business logic stays in the command handler.

```
UseCases/TeamManagement/
  RegisterTicketedEventCreation/
    RegisterTicketedEventCreationCommand.cs
    RegisterTicketedEventCreationHandler.cs   ← business logic
    EventHandlers/
      TicketedEventCreatedDomainEventHandler.cs   ← translates event → command
```

The domain event handler lives in the feature folder of the aggregate that **reacts** (not the feature that produced the event).

### HTTP endpoint registration

All admin endpoints are wired in the module's endpoint registration entry point
(for example `OrganizationApiEndpoints.cs` or `RegistrationsModule.cs`) via
`MapXxx()` extension methods. Groups mirror the URL hierarchy:

```csharp
var teams = group.MapGroup("/teams");
teams.MapCreateTeam();   // POST /admin/teams
teams.MapGetTeams();     // GET  /admin/teams

var team = teams.MapGroup("/{teamSlug}");
team.MapGetTeam();       // GET  /admin/teams/{teamSlug}
team.MapUpdateTeam();    // PUT  /admin/teams/{teamSlug}
team.MapArchiveTeam();   // POST /admin/teams/{teamSlug}/archive
```

## 8.6 Messaging and outbox

Three event tiers, each with distinct scope:

| Tier | Scope | Persistence | Location |
| :--- | :---- | :---------- | :------- |
| Domain event | In-transaction, synchronous | Not persisted separately | `Domain/DomainEvents/` |
| Module event | Async, within/between modules | Outbox table | `Application/ModuleEvents/` |
| Integration event | Async, external contracts | Outbox table | `*.Contracts/IntegrationEvents/` |

**Why three tiers?** Domain events are dispatched synchronously within the same transaction — they don't need the message bus. When an event does need async processing, we distinguish between module events and integration events. Module events are defined inside the module's `Application/ModuleEvents/` folder, so internal workflows can evolve without affecting other modules. Integration events live in the module's Contracts project (`*.Contracts/IntegrationEvents/`) as a public, versioned surface for external consumers.

Each module declares a `MessagePolicy` that maps domain events to module and/or integration events. The `DomainEventsInterceptor` calls the policy during `SaveChanges`; mapped events are written to the outbox table in the same transaction. `OutboxDispatcher` attempts best-effort dispatch immediately, with background retry via the Worker host.

### Cross-module lifecycle events

When a ticketed event is created, cancelled, or archived in the Organization module, the corresponding domain events (`TicketedEventCreatedDomainEvent`, `TicketedEventCancelledDomainEvent`, `TicketedEventArchivedDomainEvent`) are mapped via `OrganizationMessagePolicy` to module events (`TicketedEventCreatedModuleEvent`, `TicketedEventCancelledModuleEvent`, `TicketedEventArchivedModuleEvent`). The Registrations module handles these via the event→command→handler pattern (see §8.5):

- **Created** → `HandleEventCreatedHandler` creates an `EventRegistrationPolicy` with `EventLifecycleStatus = Active` and `RegistrationStatus = Draft`. Idempotent: re-delivery is a no-op.
- **Cancelled** / **Archived** → set the corresponding lifecycle status on the existing policy.

Registrations handlers that operate on the policy (open/close registration, set window, manage ticket types, register attendees, …) **trust this sync**: they look the policy up by `TicketedEventId` and throw a `NotFound` error (`EventRegistrationPolicy.Errors.EventNotFound`) when it is missing instead of creating one on demand. This keeps "the event is unknown to Registrations" distinct from "the event exists but is not active", and surfaces sync bugs immediately as 404s instead of hiding them behind misleading validation messages.

Ticket type data is owned entirely by the Registrations module — no cross-module sync.

## 8.7 Error handling

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

### Test assertion convention

Tests assert on errors using `ShouldMatch(expectedErrorObject)`, **never** via raw string code comparison:

```csharp
// ❌ Brittle — breaks silently on rename, only checks code
exception.Error.Code.ShouldBe("team.has_active_events");

// ✅ Correct — compile-time safe, checks Code + Type + Message + Details in one call
exception.Error.ShouldMatch(ArchiveTeamHandler.Errors.HasActiveEvents);
```

`ShouldMatch` verifies `Code`, `Type`, `Message`, and `Details` in a single assertion.
Referencing the static error object instead of an inline string means a rename of the error
class or code is caught at compile time.

The static error object is `internal`, so test projects require `InternalsVisibleTo` access
(already configured for all module test projects).

### Secret protection

Per-event SMTP passwords (and similar at-rest secrets owned by a module) are protected with **ASP.NET Data Protection**. Each module that stores secrets injects an `IProtectedSecret` adapter that wraps `IDataProtectionProvider` with a stable purpose string (e.g. `"Admitto.Email.ConnectionString.v1"`). The adapter is wired into EF as a value converter or property accessor on the secret column, so encryption on write and decryption on read are transparent to handlers.

The Data Protection key ring is **persisted to a stable backing store and shared across hosts** (API and Worker). Without a shared, persistent key ring, secrets written by one host would become unreadable after a restart or by another host. See `Admitto.Module.Email/Infrastructure/` for the reference implementation.

## 8.8 Value objects

Aggregates and validators express format and range invariants through small **value objects**, never raw `string` or `int` parameters. The same VO is the single owner of the rule, the constant (e.g. `MaxLength`), and the error returned when input is invalid; both EF and FluentValidation reference it.

See §8.7 for the nested `Errors` convention they participate in, and §8.9 for the EF value-converter wiring.

### Anatomy

```csharp
public readonly record struct Hostname : IStringValueObject
{
    public const int MaxLength = 255;

    public string Value { get; }

    private Hostname(string value) => Value = value;

    public static ValidationResult<Hostname> TryFrom(string? value)
        => StringValueObject.TryFrom(value, MaxLength, v => new Hostname(v));

    public static Hostname From(string? value) => TryFrom(value).GetValueOrThrow();
}
```

- `readonly record struct` for value semantics with no allocation overhead.
- Implements `IStringValueObject` or `IInt32ValueObject` from the shared kernel.
- Private constructor; construction goes through `TryFrom` (returns `ValidationResult<T>`) or `From` (throws on failure).
- `StringValueObject.TryFrom` / `Int32ValueObject.TryFrom` in the kernel encapsulate the common rules (non-empty, length cap, range) and return `CommonErrors.TextEmpty` / `CommonErrors.TextTooLong(MaxLength)` / out-of-range errors.

### Constants live on the VO

`public const int MaxLength = N;` lives on the VO and is the single source of truth — EF references it through `HasMaxLength(Foo.MaxLength)`, never through a separate constants class.

### Validation reuse

FluentValidation surfaces the VO's error code through the shared `MustBeParseable` extension, so the rule is not duplicated:

```csharp
RuleFor(x => x.SmtpHost).MustBeParseable(Hostname.TryFrom);
RuleFor(x => x.SmtpPort).MustBeParseable(Port.TryFrom);
```

`MustBeParseable` lives in `Admitto.Module.Shared.Application.Validation.FluentValidationResultExtensions` and writes the VO's error `Code` into `ValidationFailure.ErrorCode`.

### Marker types

Some types exist purely for type-level safety with no format check — e.g. `ProtectedPassword` wraps the ciphertext output of `IProtectedSecret.Protect(...)`:

```csharp
public readonly record struct ProtectedPassword
{
    public string Ciphertext { get; }

    private ProtectedPassword(string ciphertext) => Ciphertext = ciphertext;

    internal static ProtectedPassword FromCiphertext(string ciphertext) => new(ciphertext);
}
```

- Module-internal factory so plaintext cannot be wrapped from outside the module.
- No format validation (the encrypted blob is opaque).
- Domain code can take `ProtectedPassword` as a parameter type, making it impossible to accidentally pass plaintext to a property that expects an encrypted value.

### Where to place a VO

- **Module-local first.** New VOs live under `<Module>/Domain/ValueObjects/`. Module-local VO converters live under `<Module>/Infrastructure/Persistence/ValueConverters/` and are wired in the module's `DbContext.ConfigureConventions` (see §8.9).
- **Promote to shared kernel only when a second consumer appears.** `Slug`, `DisplayName`, `EmailAddress`, and `TicketedEventId` live in the shared kernel because multiple modules need them; module-local types like `Hostname` or `Port` stay local until they are needed elsewhere.

### What does NOT belong in a VO

- Cross-field rules (e.g. "Basic auth requires both username and password") — those stay in the aggregate.
- Side effects, service calls, or DB access.
- Mutable state.

## 8.9 Persistence

- EF Core `DbContext` per module, each targeting a separate PostgreSQL schema.
- `AuditInterceptor` populates `CreatedAt`, `LastChangedAt`, `LastChangedBy` on auditable entities.
- `DomainEventsInterceptor` dispatches domain events and writes outbox messages during `SaveChanges`.
- Value converters bridge value objects (§8.8) to their primitive column types.

### Value converter wiring

- **Shared kernel types** (`Slug`, `DisplayName`, `EmailAddress`, `TeamId`, …) have shared converters in `Admitto.Module.Shared/Infrastructure/Persistence/ValueConverters/`. They are registered globally by `ConfigureSharedConventions(...)`, which every module's `DbContext.ConfigureConventions` calls first.
- **Module-local types** (e.g. `Hostname`, `Port` in the Email module) have converters under `<Module>/Infrastructure/Persistence/ValueConverters/`. Register them in the module's `ConfigureConventions` after the shared call:

  ```csharp
  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
      configurationBuilder.ConfigureSharedConventions();

      configurationBuilder.Properties<Hostname>().HaveConversion<HostnameConverter>();
      configurationBuilder.Properties<Port>().HaveConversion<PortConverter>();
  }
  ```

  Once the convention is in place, `IEntityTypeConfiguration` only needs `HasMaxLength(Foo.MaxLength)` — no inline `HasConversion(...)` calls per property.

### EF Core query rules

Several EF-specific pitfalls to be aware of:

**Computed properties are not translatable.** C# computed properties (e.g. `IsArchived => ArchivedAt.HasValue`) cannot be translated to SQL. LINQ queries must reference the backing column directly:

```csharp
// ❌ Runtime exception — EF cannot translate
.Where(t => !t.IsArchived)

// ✅ Correct
.Where(t => t.ArchivedAt == null)
```

**Value object comparisons in LINQ.** EF value converters handle persistence, but LINQ predicates must use the full value object, not the inner primitive:

```csharp
// ❌ Not translatable
.Where(t => t.Id.Value == guid)

// ✅ Correct
.Where(t => t.Id == TeamId.From(guid))
```

**Use `.AsNoTracking()` for guard queries.** Queries used only as precondition checks (e.g. "does an active event exist?") should call `.AsNoTracking()` to avoid polluting the change tracker with entities that will not be modified.

### Optimistic concurrency

The `Version` property on all aggregates (`[Timestamp]`, `uint`) is the EF row-version concurrency token. `DbSetExtensions.GetAsync(key, expectedVersion?)` validates the token on load and throws `ConcurrencyConflictError` on mismatch. Clients read the current version when fetching a resource and supply it on mutating operations.

### Write-amplifier pattern

When one aggregate must protect against concurrent modifications triggered by another aggregate, store a monotonically-incrementing counter on the first aggregate and increment it whenever the second is modified. This forces a write to the first aggregate's row, advancing its `Version` token, so any concurrent operation holding the old token fails at commit.

Example: `Team.TicketedEventScopeVersion` is incremented by `RegisterTicketedEventCreationHandler` each time a `TicketedEvent` is created under the team. This closes the TOCTOU window between the active-events guard in `ArchiveTeamHandler` and its commit: if a ticketed event is created concurrently, the team row changes, and the archive's optimistic concurrency check fails.

Note: `TicketedEventScopeVersion` is a monotonically-increasing counter, not a count of currently-active events. It never decrements.

## 8.10 Domain event dispatch — in-process pattern

`DomainEventsInterceptor` fires inside `SavingChangesAsync` (before the actual write), so domain event handlers run **within the same database transaction** as the triggering aggregate. This guarantees atomicity between the event and its side effects.

### EF change tracker reuse

When a command handler loads an aggregate and a domain event handler (running during save) loads the same aggregate by the same key, EF returns the already-tracked instance from the change tracker — no extra database round-trip.

### Fast-fail guard rule

`DatabaseTestContext` (used in integration tests) only registers `AuditInterceptor`. `DomainEventsInterceptor` is **not** registered, so domain event handlers do not fire during handler-level tests.

Business rules that need test coverage must be enforced as an explicit guard in the command handler *before* the save, in addition to any enforcement inside a domain event handler. The command handler guard provides testability; the domain event handler provides defence in depth in production.

```csharp
// In CreateTicketedEventHandler — explicit guard so SC-015 is testable
var team = await writeStore.Teams.GetAsync(TeamId.From(command.TeamId), cancellationToken);
team.EnsureNotArchived();   // fast-fail here

// TicketedEvent.Create() raises TicketedEventCreatedDomainEvent,
// which triggers RegisterTicketedEventCreationHandler during SaveChanges —
// that handler also calls EnsureNotArchived() inside RegisterTicketedEventCreation(),
// providing defence in depth in production at no extra DB cost (change tracker reuse).
```

## 8.11 Handler and event handler DI registration

Command handlers, query handlers, domain event handlers, module event handlers, and integration event handlers are all auto-discovered by Scrutor assembly scan. No manual registration is needed.

| Handler type | Registration method | Scrutor selector |
| :----------- | :------------------ | :--------------- |
| `ICommandHandler<T>` / `ICommandHandler<T,R>` | `AddCommandHandlersFromAssembly` | `AssignableTo<ICommandHandler>()` (marker interface) |
| `IQueryHandler<T,R>` | `AddQueryHandlersFromAssembly` | `AssignableTo(typeof(IQueryHandler<,>))` |
| `IDomainEventHandler<T>` | `AddDomainEventHandlersFromAssembly` | `AssignableTo(typeof(IDomainEventHandler<>))` |
| `IModuleEventHandler<T>` | `AddModuleEventHandlersFromAssembly` | `AssignableTo(typeof(IModuleEventHandler<>))` |
| `IIntegrationEventHandler<T>` | `AddIntegrationEventHandlersFromAssembly` | `AssignableTo(typeof(IIntegrationEventHandler<>))` |

**Rule:** Place the handler class anywhere in the module assembly and implement the correct interface. Do not use `Where(t => t.IsGenericType …)` as a filter — this matches only open generic types and never selects a concrete handler.

## 8.12 Observability

Service defaults (`Admitto.ServiceDefaults`) configure:

- OpenTelemetry tracing and metrics
- Health checks at `/health` and `/alive`
- Request timeouts
- Output caching

## 8.13 Scheduled jobs (Quartz.NET)

Background work that cannot be expressed as a domain or integration event — for example, polling for records whose grace period has expired — is implemented as a Quartz `IJob`.

### Capability gating

Jobs are registered only in hosts that carry the `HostCapability.Jobs` flag. The `[RequiresCapability(HostCapability.Jobs)]` attribute on a job class gates its DI registration in the module's `AddOrganizationJobs()` helper, which is only called when `capabilities.HasFlag(HostCapability.Jobs)` is true.

The Worker host (`Admitto.Worker`) sets this flag. The API host does not.

### Transaction ownership

A Quartz job is the **transaction boundary owner** for its work, exactly as an HTTP endpoint is for a request. The job injects the keyed `IUnitOfWork` for its module and calls `SaveChangesAsync` after each logical unit of work:

```csharp
[RequiresCapability(HostCapability.Jobs)]
[DisallowConcurrentExecution]
public sealed class DeprovisionUserIdpJob(
    IOrganizationWriteStore writeStore,
    IExternalUserDirectory userDirectory,
    [FromKeyedServices(OrganizationModuleKey.Value)] IUnitOfWork unitOfWork,
    ILogger<DeprovisionUserIdpJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var users = await writeStore.Users
            .Where(u => u.DeprovisionAfter != null && u.DeprovisionAfter <= DateTimeOffset.UtcNow)
            .ToListAsync(context.CancellationToken);

        foreach (var user in users)
        {
            // ... mutate user ...
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
```

Committing per record (not per batch) limits the blast radius of failures and allows partial progress.

### Concurrency control

`[DisallowConcurrentExecution]` prevents a second trigger from starting while the previous execution is still running. This is always applied to jobs that mutate database state.

### Scheduling

Jobs are registered with an in-memory trigger (no persistent Quartz store). The trigger fires on an interval appropriate for the SLA of the business operation — hourly for IdP deprovisioning. The schedule is defined alongside the job registration in the module's `DependencyInjection.cs`.

### Testing jobs

Because jobs own the transaction boundary, they are tested at the integration level (like endpoints), not the unit level. The test:

1. Seeds the database state that should trigger the job (e.g. a user with `DeprovisionAfter` in the past via raw SQL, bypassing the domain's grace-period constraint).
2. Creates the job with the real DbContext and NSubstitute mocks for external services.
3. Supplies a substitute `IJobExecutionContext` so the job's `CancellationToken` is bound to the test's token.
4. Executes the job and asserts the resulting database state.

A thin `DbContextUnitOfWork` adapter is used in tests to forward `SaveChangesAsync` to the underlying `DbContext`, replacing the keyed DI service that is not available in the test context.
