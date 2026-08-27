# 8. Cross-cutting concepts

## 8.1 Endpoint-owned unit of work

API endpoints — not command handlers — own the transaction boundary. The endpoint resolves a keyed `IUnitOfWork` for its module and calls `SaveChangesAsync` after the handler returns. This keeps handlers framework-agnostic and testable without persistence concerns.

**Rule:** Command handlers must not inject or call `IUnitOfWork`.

Reference: `IUnitOfWork` registered per module via `AddModuleDatabaseServices<TWriteModel, TDbContext>()` in `Admitto.Core/Shared/Infrastructure/DependencyInjection.cs`.

## 8.2 Validation

FluentValidation validators are discovered per module assembly and registered in DI. For admin endpoints, `ValidationFilter` (an endpoint filter) runs validation on all request DTO arguments before the handler executes. Invalid requests return a standard `ValidationProblem` response.

**Rule:** When the handler runs, the request DTO has already been validated.

Reference: `Admitto.Api/Middleware/ValidationFilter.cs`, applied in `Admitto.Api/Endpoints/AdminEndpoints.cs`.

## 8.3 Authentication and authorization

- **Admin API:** Endpoints under `/admin/...` serve the Admin UI and require JWT bearer authentication plus the relevant admin or team-membership authorization policy. Admin handlers use explicit team/event IDs from the route after `UserContextResolutionMiddleware` has resolved and checked the route scope.
- **Partner API:** Endpoints under `/api/...` serve trusted partner event websites and require `X-Api-Key`. The key authenticates as its owning team and carries the shared `team_id` claim defined in `Admitto.Core.Shared.Application.Auth.ApiKeyClaims`. Partner routes use `/api/events/{eventSlug}/...`; they do not include team ID or team slug. Endpoint code extracts `TeamId` from the API-key principal, resolves `TicketedEvent.PublicSlug` inside that team scope, and fails closed with 401 if the claim is missing or invalid.
- **Public API:** Anonymous attendee-facing routes live under `/e/...`. They resolve Admitto-owned public event slugs and either redirect to the configured partner website URL/path or return a registration QR-code PNG. These routes do not accept request-controlled redirect targets and do not require `X-Api-Key`.
- **Authentication:** JWT Bearer tokens validated against a configurable authority. Production Admin UI sign-in uses Keycloak's hosted passkey-only browser flow and starts directly at passkey detection; passwords and direct grants are not enabled for the production Admin UI client. The Keycloak account-console client is disabled; Admitto owns the user-facing account-management surface. Local development intentionally uses a separate Keycloak realm where the standard username/password form is shown first with passkey as an alternative; end-to-end tests retain local-only direct-grant clients. Challenge and forbidden responses return ProblemDetails.
- **Admin authorization:** `AdminAuthorizationRequirement` checked by `AdminAuthorizationHandler` via `IAdministratorRoleService`.
- **User context resolution:** `UserContextResolutionMiddleware` resolves the authenticated JWT subject to a domain user before authorization runs. It classifies admin route values into explicit global, team, or event scope first. Malformed route values, or `eventId` without `teamId`, fail closed with 403 before authorization or endpoint handlers run. When the route is event-scoped, `UserContextResolver` verifies that the event belongs to the team; non-admin mismatches return 403 before endpoint authorization or handler execution.
- **Team membership authorization:** `TeamMembershipAuthorizationRequirement` checked by `TeamMembershipAuthorizationHandler` against the pre-resolved user context. Admin users bypass team checks.

Endpoints declare requirements with `policy.RequireAdminRole()` or `policy.RequireTeamMembership(role)`.

### Keycloak account-action email delivery

Keycloak account-action emails are infrastructure-owned identity-provider emails. Admitto triggers them through Keycloak's Admin API `execute-actions-email` endpoint, and Keycloak sends them through its own SMTP configuration using the Admitto Keycloak email theme. They use the same deployment SMTP parameter set as the Worker, but remain outside Admitto's Email module templates, logs, and outbox.

### Public and Partner API rate limiting

Public and Partner endpoints use ASP.NET Core rate limiting policies configured under `RateLimiting:Public` in `Admitto.Api`. The limits are IP-partitioned middleware guards, not business-level abuse controls. They must be sized for the expected deployment model where the external event website can proxy many attendees through a single source IP. Business-specific limits, such as OTP request throttling per email/event, remain enforced in application handlers.

## 8.4 Organization scope resolution and cross-module facades

Admin endpoints declare `teamId` and `eventId` as explicit GUID path parameters in their handler signatures. No slug-to-ID translation is needed; the IDs from the route are used directly to load aggregates.

Before authorization, `UserContextResolutionMiddleware` parses those route values into one of three valid scopes: global (no route IDs), team (`teamId`), or event (`teamId` + `eventId`). Invalid combinations or unparsable IDs return 403. The middleware passes only this explicit scope to `UserContextResolver`, which selects the user's membership role for the requested team and uses Organization's ticketed-event tracking state to verify that the supplied event belongs to the supplied team. This centralized route-scope guard means handlers do not need to add parent team/event existence checks solely to defend against guessed event GUIDs, though handlers should keep `TeamId` filters when team ownership is part of the resource being queried or mutated.

The `TeamMembershipAuthorizationHandler` reads the pre-resolved user context instead of querying route values directly.

### Synchronous cross-module facades

Some workflows need to consult another module's state inside the same request without going through the outbox. We expose these as **facade interfaces** in the target module's `*.Contracts` project, implemented inside the module proper. Callers depend only on the interface.

| Facade | Module | Used by | Purpose |
| :----- | :----- | :------ | :------ |
| `IOrganizationFacade` | Organization | Registrations | Check team membership, look up team by ID |
Facades are read-only and side-effect-free. Cross-module *writes* still go through commands and integration events on the outbox (see §8.6).

Email does not synchronously query Organization or Registrations for reusable email rendering context. It owns eventually consistent team/event context projections populated from integration events and uses live Registrations facade reads only for attendee-source recipient resolution and reconfirm candidate eligibility during the hourly event evaluation.

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
| `{Surface}/{Slice}HttpEndpoint.cs` | Minimal API endpoint; owns the UoW commit. `Surface` follows the established module convention, such as `AdminApi/`, `PartnerApi/`, `PublicApi/`, or `InternalApi/`. | When HTTP-exposed |
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

### Application projections / read models

Read models that are derived from in-module domain events live under `<Module>/Application/Projections/{ProjectionName}/`. Their persisted row types are application projection types, not domain entities or aggregates. They may still be mapped by the module's EF Core infrastructure when the projection is stored in the module schema.

Synchronous projections that must be transactionally consistent with the originating aggregate implement `IDomainEventHandler<T>` and are dispatched by `DomainEventsInterceptor` inside the same unit of work. They do not use Inbox processing because domain events are in-process and not redeliverable queue messages. Multi-event projection maintainers should be named `*Projector`, for example `ActivityLogProjector`, rather than being split into command slices solely to satisfy single-event handler naming.

### HTTP endpoint registration

All admin endpoints are wired in the module's endpoint registration entry point
(for example `OrganizationApiEndpoints.cs` or `RegistrationsModule.cs`) via
`MapXxx()` extension methods. Groups mirror the URL hierarchy:

```csharp
var teams = group.MapGroup("/teams");
teams.MapCreateTeam();   // POST /admin/teams
teams.MapGetTeams();     // GET  /admin/teams

var team = teams.MapGroup("/{teamId:guid}");
team.MapGetTeam();       // GET  /admin/teams/{teamId}
team.MapUpdateTeam();    // PUT  /admin/teams/{teamId}
team.MapArchiveTeam();   // POST /admin/teams/{teamId}/archive
```

## 8.6 Messaging and outbox

Two event tiers, each with distinct scope:

| Tier | Scope | Persistence | Location |
| :--- | :---- | :---------- | :------- |
| Domain event | In-transaction, synchronous | Not persisted separately | `Domain/DomainEvents/` |
| Command / Integration event | Async, via outbox | Outbox table | `Application/…/` or `*.Contracts/IntegrationEvents/` |

**Why two tiers?** Domain events are dispatched synchronously within the same transaction via `IDomainEventHandler<T>` — they don't cross the message bus. Handlers that need async processing (fan-out, cross-module writes) inject `IOutbox` and call `outbox.Enqueue(command)` or `outbox.Enqueue(integrationEvent)` inside the same handler. The `DomainEventsInterceptor` publishes domain events after `SaveChanges`; the outbox message was already inserted in the same transaction.

**Commands vs integration events on the outbox**

- `ICommand` — used for internal, within-module async work (e.g. a scheduled Quartz fan-out triggered by a domain event). Type key: `command.{module-kebab}.{command-name-kebab}` (strips `-command` suffix). The `QueueMessageDispatcher` deserialises and routes these to the module's `IMediator`.
- `IIntegrationEvent` — used for cross-module contracts. Type key: `integration.{module-kebab}.{event-name-kebab}` (strips `-integration-event` suffix). Lives in `*.Contracts/IntegrationEvents/`.

**Message contracts declare exactly one public constructor** (enforced by `MessagingConventionTests`), covering integration events, commands, and domain events.

For integration events and commands the reason is serialisation: both are written to the outbox as JSON (`OutboxMessage.From` accepts exactly these two) and rehydrated by the queue dispatcher, so their shape is a wire contract. A convenience constructor that defaults or drops fields compiles cleanly but emits a payload that cannot round-trip, and consumers silently observe defaulted values. This has caused real defects — a discarded `TeamAccentColor` argument, and overloads that zeroed the aggregate version fields used for stale-message detection.

Domain events are dispatched in-process and never serialised, so that argument does not apply to them — but their observed failure mode was worse. Their convenience constructors fabricated plausible-looking domain data (`EventName "Unknown event"`, `Slug "unknown-event"`, `FirstName "Unknown"`), which `RegistrationsIntegrationEventPublisher` then copied onto the wire and into read-model projections. Placeholder values that look real are harder to spot than nulls, so the same rule applies.

Tests that want a terse construction path use a builder under `tests/Admitto.Testing/Builders/` (for example `TicketedEventCreatedIntegrationEventBuilder`) rather than an overload on the contract.

`OutboxDispatcher` attempts best-effort dispatch immediately after a successful unit-of-work commit. The Worker host also runs `OutboxRetryBackgroundService`, which scans every registered module `IOutboxDbContext` for bounded batches of `Pending` rows older than the configured retry minimum age and marks them `Sent` after queue send succeeds. The minimum age avoids racing the unit-of-work's immediate post-commit dispatch. Duplicate queue sends are still tolerated because the outbox cannot atomically couple the external queue send with the database update; receiving handlers must therefore stay idempotent.

On the consuming side, the Worker's `ServiceBusMessageProcessor` hosted service wraps the Azure SDK's push-based `ServiceBusProcessor`.
The broker pushes messages over a long-lived AMQP link that the SDK keeps alive, re-establishes after a fault, and uses to renew the message lock while a handler runs.
Dispatch is sequential (`MaxConcurrentCalls = 1`), and settlement is explicit (`AutoCompleteMessages = false`): a message is completed once `QueueMessageDispatcher` succeeds and abandoned for redelivery when it fails, so a persistently failing message is dead-lettered by the broker once it exceeds the queue's max delivery count.
Link and connection faults surface through the processor's error handler as warnings rather than errors, because the processor recovers from them on its own; a real outage shows up as the warning repeating.
Recovery latency is bounded by `ServiceBusRetryOptions.MaxDelay`, set to 5 seconds in `AddSharedInfrastructureMessagingServices` so a consumer cannot idle for the SDK's 60-second default after a blip (see [ADR-015](../adr/adr-015-service-bus-push-based-consumption.md)).

For Email module SMTP delivery, `EmailLog` is the send claim. Trigger handlers write a pending log row and enqueue internal delivery work before SMTP is attempted. Delivery handlers and bulk fan-out treat terminal rows as no-ops and retryable pending rows as recoverable work. SMTP itself is non-transactional, so the documented guarantee is duplicate minimization through database-backed claims, not perfect exactly-once delivery.

### Cross-module lifecycle events

Event creation is a Registrations-owned operation *gated* by Organization. Organization emits `TicketedEventCreationRequested` (carrying a `CreationRequestId`) to request materialisation; Registrations inserts the authoritative `TicketedEvent`, creates an Active `TicketCatalog` in the same unit of work, and emits `TicketedEventCreated` or — on other validation failure — `TicketedEventCreationRejected`.

Lifecycle transitions on the `TicketedEvent` aggregate (`Archive`) raise an in-module `TicketedEventStatusChanged` domain event that projects onto `TicketCatalog.EventStatus` in the same transaction as the source-of-truth status change. In parallel, a separate `IDomainEventHandler<TicketedEventStatusChangedDomainEvent>` outboxes `TicketedEventArchived` integration events so Organization can advance the team's counters (`ActiveEventCount`, `ArchivedEventCount`).

All cross-module integration-event handlers are idempotent:

- Organization's `TicketedEventCreated` / `TicketedEventCreationRejected` handlers key off `CreationRequestId`.
- Organization's `TicketedEventArchived` handler keys off `TicketedEventId` plus the observed transition (so redelivery after the counter has already moved is a no-op).

Handlers that require inbox protection insert a `ProcessedMessage` marker before mutating state. The marker is committed in the same unit of work as the handler's aggregate changes. If two deliveries race, both can pass the initial marker lookup, but the unique `message_key` constraint makes one `SaveChangesAsync` fail; the module `UnitOfWork` converts that database conflict to `DuplicateProcessedMessageException`, and the queue dispatcher acknowledges it as an already-processed delivery.

Ticket type data is owned entirely by the Registrations module — no cross-module sync.

### Email context projections

The Email module persists one `email.team_email_context_view` row per `team_id` and one `email.event_email_context_view` row per `(team_id, ticketed_event_id)`. These are application read models (`TeamEmailContextView` and `EventEmailContextView` under `Application/Projections/`), maintained by focused role-based projectors (`TeamEmailContextProjector` for Organization team events and `EventEmailContextProjector` for Registrations event events), and exposed only through the module read store (`IEmailReadStore`) — projectors write projections through the read store, mirroring the Registrations `ActivityLogView`/`ActivityLogProjector` convention. Organization publishes team-level created/details-updated events carrying team name and accent color; it never enumerates events for Email branding. SMTP message construction does not consult these projections at all: the `From` address and display name are deployment configuration and no `Reply-To` is set (see ADR-013). The projected team name is used only as the `team_name` template parameter, and the accent color only for template theming. Registrations `TicketedEventDetailsChanged` events update event name, website URL, public slug, and time zone; other Registrations events update reconfirm policy, self-service ticket count, and lifecycle state.

Projection events carry source aggregate versions (`TeamVersion`, `TicketedEventVersion`, and `TicketCatalogVersion`). Projection rows store the last applied source version and ignore older deliveries, so duplicate and late Service Bus messages cannot overwrite newer source state. Projection rows also use EF row-version concurrency; queue handlers save with retryable concurrency semantics so races between workers are retried instead of acknowledged as deterministic failures. The projector may create partial rows so out-of-order delivery can be filled in by later events. Schedule-affecting updates, including policy clearing and archive, change whether an event is enabled and Active for the hourly reconfirm evaluation. Reusable reads are their own query slices under `Application/UseCases/EventEmailContexts/`; rendering reads validate required team and event fields and fail/defer deterministically when either projection is not ready.

## 8.6.1 Quartz scheduling

Quartz is configured once in shared infrastructure (`AddSharedQuartzInfrastructure`) and uses the `quartz-db` PostgreSQL database as a persistent store with clustering enabled. Module extension methods register jobs and triggers through additive `AddQuartz(...)` calls; they must not configure their own scheduler store.

The Worker host starts `QuartzHostedService`. API handlers may resolve `ISchedulerFactory` to persist schedules, but API does not host job execution. This lets schedules written by API or queue handlers be acquired by any Worker replica while Quartz guarantees that a trigger fires on only one cluster node.

`Admitto.AppHost/DatabaseScripts/quartz.sql` owns the `QRTZ_` schema initialization for `quartz-db`, using Quartz's PostgreSQL table layout. The schema script is idempotent and is not managed by EF Core migrations because Quartz owns those tables.

## 8.7 Error handling

### Pipeline

- `BusinessRuleViolationException` is thrown when a `ValidationResult<T>` is unwrapped on failure, or directly when a business rule is violated.
- `ApplicationErrorExceptionHandler` maps business exceptions to ProblemDetails responses.
- `GlobalExceptionHandler` catches unexpected exceptions.
- `IPostgresExceptionMapping` (keyed per module) maps database constraint violations to domain errors.
- Optimistic concurrency conflicts (`DbUpdateConcurrencyException`) are mapped to `ConcurrencyConflictError`.

### Error placement — three tiers

ProblemDetails responses include the stable `code` extension and any explicit `Error.Details` entries as additive top-level extensions. Public handlers must only put safe, client-actionable details into errors; for example, self-service registration ticket-state conflicts expose only the submitted ticket type IDs grouped by current state, after email-verification token validation succeeds.

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

### System SMTP secrets

Application-email SMTP credentials are deployment secrets, supplied through host configuration and secret providers under `Email:System`. They are not persisted in module-owned tables and are not managed by organizers through the API or Admin UI.

### Registration-bound public links

Attendee-held registration links, including QR-code retrieval and self-service cancellation/edit redirects, treat `RegistrationId` as a high-entropy bearer secret. Anonymous Public API links use `/e/{eventSlug}/...`; the event slug resolves `TicketedEventId`, and QR-code retrieval loads registrations only by `(eventId, registrationId)`. Partner self-service mutation endpoints use `/api/events/{eventSlug}/...`, resolve the slug to `TicketedEventId` within the API-key owner's team scope, and still require `X-Api-Key`.

QR codes encode the literal registration ID string. They do not include a per-event HMAC signature, and `TicketedEvent` does not carry a QR signing key. Future check-in flows should validate QR payloads server-side under the selected event/team, or introduce a dedicated check-in token design if offline validation becomes a real requirement.

**Endpoint validation order** for anonymous public QR-code retrieval: resolve event by `PublicSlug` (404) → load the aggregate by `(eventId, registrationId)` (404 on missing/wrong event) → return the PNG. Partner registration-bound mutation endpoints first authenticate `X-Api-Key` and resolve `TeamId` from the shared `team_id` claim (401), then resolve event and registration within that team scope. The attendee-editable self-service update endpoint replaces first name, last name, additional details, and final ticket selection atomically at `PUT /api/events/{eventSlug}/registrations/{registrationId}`; the previous ticket-only `/tickets` Partner route is intentionally not retained.

The only remaining registration signing mechanism is the short-lived email-verification token issued by the OTP flow. It is HMAC-signed with the configured `Registrations:VerificationToken:SigningKey`, embeds event/team/email claims, and is independent of `TicketedEvent` state.

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
- `DomainEventsInterceptor` dispatches domain events after `SaveChanges`; outbox messages are written inside `IDomainEventHandler<T>` implementations in the same transaction.
- Value converters bridge value objects (§8.8) to their primitive column types.
- Enum values are stored as text (`HasConversion<string>()`) rather than ordinals so database rows stay readable during debugging and operations.

### Module stores

Application code depends on module store interfaces rather than directly on the EF `DbContext`. Write paths use the module write store, such as `IRegistrationsWriteStore`, because they mutate aggregates, inbox state, or outbox state through the current unit of work. Persisted application projections/read models are exposed through companion read stores, such as `IRegistrationsReadStore` for `ActivityLogView`, to keep derived read-side storage distinct from command-side aggregate storage even when the current implementation is backed by the same EF context.

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

When one aggregate must protect against concurrent modifications triggered by another aggregate, store a monotonically-incrementing counter (or equivalent bounded state) on the first aggregate and advance it whenever the second is modified. This forces a write to the first aggregate's row, advancing its `Version` token, so any concurrent operation holding the old token fails at commit.

Example: the `Team` aggregate's `ActiveEventCount` / `PendingEventCount` (advanced by integration-event handlers reacting to `TicketedEventCreationRequested` / `TicketedEventCreated` / `TicketedEventArchived`) close the TOCTOU window between the active/pending-events guard in `Team.Archive()` and its commit: a concurrent event creation or lifecycle transition writes the team row, and the archive's optimistic concurrency check fails.

A second example inside a single module: Registrations projects `TicketedEvent.Status` onto `TicketCatalog.EventStatus` in the *same* unit of work as archive. An in-flight registration that loaded `TicketCatalog` at a prior version fails its claim with a `DbUpdateConcurrencyException`.

## 8.10 Domain event dispatch — in-process pattern

`DomainEventsInterceptor` fires inside `SavingChangesAsync` (before the actual write), so domain event handlers run **within the same database transaction** as the triggering aggregate. This guarantees atomicity between the event and its side effects.

### EF change tracker reuse

When a command handler loads an aggregate and a domain event handler (running during save) loads the same aggregate by the same key, EF returns the already-tracked instance from the change tracker — no extra database round-trip.

### Fast-fail guard rule

`DatabaseTestContext` (used in integration tests) only registers `AuditInterceptor`. `DomainEventsInterceptor` is **not** registered, so domain event handlers do not fire during handler-level tests.

Business rules that need test coverage must be enforced as an explicit guard in the command handler *before* the save, in addition to any enforcement inside a domain event handler. The command handler guard provides testability; the domain event handler provides defence in depth in production.

```csharp
// In CreateTicketedEventHandler — explicit guard keeps handler behavior testable
var team = await writeStore.Teams.GetAsync(TeamId.From(command.TeamId), cancellationToken);
team.EnsureNotArchived();   // fast-fail here

// TicketedEvent.Create() raises TicketedEventCreatedDomainEvent,
// which triggers RegisterTicketedEventCreationHandler during SaveChanges —
// that handler also calls EnsureNotArchived() inside RegisterTicketedEventCreation(),
// providing defence in depth in production at no extra DB cost (change tracker reuse).
```

## 8.11 Handler and event handler DI registration

Command handlers, query handlers, domain event handlers, and integration event handlers are all auto-discovered by Scrutor assembly scan. No manual registration is needed.

| Handler type | Registration method | Scrutor selector |
| :----------- | :------------------ | :--------------- |
| `ICommandHandler<T>` / `ICommandHandler<T,R>` | `AddCommandHandlersFromAssembly` | `AssignableTo<ICommandHandler>()` (marker interface) |
| `IQueryHandler<T,R>` | `AddQueryHandlersFromAssembly` | `AssignableTo(typeof(IQueryHandler<,>))` |
| `IDomainEventHandler<T>` | `AddDomainEventHandlersFromAssembly` | `AssignableTo(typeof(IDomainEventHandler<>))` |
| `IIntegrationEventHandler<T>` | `AddIntegrationEventHandlersFromAssembly` | `AssignableTo(typeof(IIntegrationEventHandler<>))` |

**Rule:** Place the handler class anywhere in the module assembly and implement the correct interface. Do not use `Where(t => t.IsGenericType …)` as a filter — this matches only open generic types and never selects a concrete handler.

## 8.12 Observability

Service defaults (`Admitto.ServiceDefaults`) configure:

- OpenTelemetry tracing and metrics
- Health checks at `/health` and `/alive`
- Request timeouts
- Output caching

### Production logging policy

API and Worker configuration keeps Admitto application logs visible while suppressing routine framework and SDK noise before telemetry ingestion:

| Category | Level | Applies from | Rationale |
| :------- | :---- | :----------- | :-------- |
| `Default` | `Information` | `appsettings.json` | Keeps general operational signal. |
| `Amolenk.Admitto` | `Information` | `appsettings.json` | Keeps domain/application lifecycle logs. |
| `Azure.Messaging.ServiceBus` | `Warning` | `appsettings.json` | Suppresses receive/link lifecycle logs while retaining queue warnings and errors. |
| `Azure.Core` | `Warning` | `appsettings.json` | Suppresses Azure SDK internals except warnings/errors. |
| `Microsoft.AspNetCore` | `Warning` | `appsettings.Production.json` | Suppresses routine framework request logs. |
| `Microsoft.EntityFrameworkCore.Database.Command` | `Warning` | `appsettings.Production.json` | Suppresses SQL command chatter while retaining command failures. |
| `Quartz` | `Warning` | `appsettings.Production.json` | Suppresses scheduler chatter while retaining job/scheduler failures. |
| `Microsoft.Hosting.Lifetime` | `Information` | (default) | Keeps startup/shutdown signal. |

The two Azure SDK categories are suppressed in the shared `appsettings.json` baseline because they carry no application signal in any environment: they narrate AMQP link and HTTP pipeline mechanics that the SDK already recovers from on its own.
The framework categories that developers do want locally - EF Core SQL, ASP.NET Core requests, Quartz scheduling - stay at `Information` outside production and are suppressed only in `appsettings.Production.json`.
`appsettings.Development.json` raises `Amolenk.Admitto` to `Debug`.
Developers can temporarily raise any category through normal configuration overrides during an incident.

### Log severity expectations

`Error` and `Critical` application logs are operator-actionable and feed Azure alert evaluation. Use them for unexpected failures such as unhandled API exceptions, failed queue message processing, unrecoverable job failures, or startup reconciliation failures. Expected validation, authorization, not-found, concurrency, and business-rule outcomes must stay below `Error` and be returned as ProblemDetails or domain errors rather than logged as alerting failures.

The existing API and Worker `LogError` calls are reserved for unhandled exceptions, queue processing failures, bulk-email failures that require operator attention, and reconfirm evaluation failures.

### Azure Monitor sampling

When `APPLICATIONINSIGHTS_CONNECTION_STRING` is present, `Admitto.ServiceDefaults` enables Azure Monitor export and reads `Observability:AzureMonitor:SamplingRatio`. If the setting is absent or empty, the code default is `0.1`; configured values must be between `0` and `1`. AppHost publish mode wires the same setting to API and Worker through `OBSERVABILITY__AZUREMONITOR__SAMPLINGRATIO` from the optional `azureMonitorSamplingRatio` publish parameter.

The sampling ratio is trace sampling. `Admitto.ServiceDefaults` keeps Azure Monitor trace-based log sampling enabled (`EnableTraceBasedLogsSampler=true`) so logs associated with sampled-out traces are also dropped. This keeps the Azure Monitor setup close to defaults and provides a stronger ingestion-cost reduction than trace sampling alone. Operators can temporarily increase `Observability:AzureMonitor:SamplingRatio` during incidents when higher trace/log fidelity is needed.

Local Aspire diagnostics use the OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is present, so local visibility does not depend on the Azure Monitor sampling ratio.

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

## 8.14 In-aggregate lifecycle invariants

The Registrations module enforces "no policy edits / no registrations after archive" directly on the `TicketedEvent` aggregate (for policy mutators) and on the `TicketCatalog` aggregate (for the registration claim).

### `TicketedEvent` policy mutators

Every policy mutator (`ConfigureRegistrationPolicy`, `ConfigureReconfirmPolicy`, `ConfigureWaitlistPolicy`, `UpdateDetails`) refuses when the aggregate's `Status` is not Active. Optimistic concurrency is supplied by the aggregate's own `Version` (EF `[Timestamp]` row-version). A concurrent `Archive()` on the same aggregate advances the row-version, so any policy edit loaded at the prior version fails its commit with a `DbUpdateConcurrencyException`.

### `TicketCatalog.EventStatus` projection

`TicketCatalog` stores a single read-only `EventStatus` field projected from `TicketedEvent` in the *same unit of work* as the lifecycle transition. `TicketCatalog.Claim(...)` refuses when `EventStatus` is Archived, giving an atomic status + capacity gate on ticket allocation:

```
TicketCatalog
├── EventStatus : EventStatus (Active / Archived)
├── capacity / reservations (existing)
└── Version : uint (EF [Timestamp] row-version)
```

Because `TicketedEvent.Archive()` commits the projection onto `TicketCatalog` in the same transaction as the source-of-truth status change, an in-flight registration that loaded `TicketCatalog` at a prior version fails at `SaveChanges` — the write-amplifier mechanism described in §8.9.

### Why not a separate lifecycle-guard aggregate?

Previous designs used a dedicated `TicketedEventLifecycleGuard` aggregate to mirror event status from the Organization module into Registrations. That guard is gone: with `TicketedEvent` now owned by Registrations, the aggregate enforces its own invariants and the only out-of-aggregate projection is the single `EventStatus` field on `TicketCatalog`, which exists solely to make the status + capacity check atomic. See [ADR-008](../adr/adr-008-ticketed-event-ownership-in-registrations.md).

## 8.15 Architecture enforcement (ArchUnitNET)

All architectural rules below are machine-checked by `tests/Admitto.Core.ArchTests` using [ArchUnitNET](https://github.com/TNG/ArchUnitNET). The suite is the **first test step** after `dotnet build` — if it fails, fix the violation before touching other tests.

### Dependency direction rules (`DependencyRulesTests`)

| Rule | Constraint |
| :--- | :--------- |
| `Shared.Kernel` isolation | `Admitto.Core.Shared.Kernel.*` must not reference any other `Admitto.Core.*` namespace |
| Domain purity | `*.Module.X.Domain.*` must not reference `*.Module.X.Application.*` or `*.Module.X.Infrastructure.*` |
| Application cross-module | `*.Module.X.Application.*` must not reference another module's `Domain`, `Application`, or `Infrastructure` — only `*.Contracts` |
| Infrastructure cross-module | `*.Module.X.Infrastructure.*` must not reference another module's namespaces except via `*.Contracts` |

### Naming conventions (`NamingRulesTests`)

These are enforced via MSTest reflection checks on the loaded `Admitto.Core` assembly:

| Interface | Required class name |
| :-------- | :------------------ |
| `IDomainEventHandler<T>` | `{T.Name}Handler`, or role-based `*Publisher` / `*Projector` for multi-event side-effect classes |
| `IIntegrationEventHandler<T>` | `{T.Name}Handler` |
| `ICommandHandler<T>` | `T` name with `Command` replaced by `Handler` (e.g. `CreateTeamCommand` → `CreateTeamHandler`) |
| `IQueryHandler<T,R>` | `T` name with `Query` replaced by `Handler` |

### Messaging conventions (`MessagingConventionTests`)

| Rule | Constraint |
| :--- | :--------- |
| Single constructor | Every `IIntegrationEvent`, `ICommand`, and `IDomainEvent` implementation declares exactly one public constructor carrying the full field set — no convenience overloads on message contracts |
| Deserialization target | Should overloads ever be reintroduced on a *serialised* contract (`IIntegrationEvent`, `ICommand`), exactly one must carry `[JsonConstructor]` |

### Placement rules (`PlacementRulesTests`)

| Class pattern | Required namespace suffix |
| :------------ | :------------------------ |
| `*DomainEventHandler`, `*IntegrationEventHandler` | `…EventHandlers` |
| `*HttpEndpoint` | `…AdminApi`, `…PartnerApi`, `…PublicApi`, or `…InternalApi` |
| `AbstractValidator<T>` subclasses | `…AdminApi`, `…PartnerApi`, `…PublicApi`, or `…InternalApi` |
| `*Command` or `*Query` | `*.Application.UseCases.*` |

### Contracts namespace convention

A module's **public surface** is its `Contracts` sub-namespace (e.g. `Admitto.Core.Module.Organization.Contracts`). This namespace holds:
- Facade interfaces (`IOrganizationFacade`, `IEventEmailFacade`)
- Integration event DTOs consumed by other modules
- Response/request DTOs shared across module boundaries

No other module may import from a sibling module's `Domain`, `Application`, or `Infrastructure` sub-namespaces. ArchUnitNET enforces this boundary automatically.
