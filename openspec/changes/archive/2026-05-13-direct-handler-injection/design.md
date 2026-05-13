## Context

The application uses a hand-rolled `IMediator` that routes commands, queries, and domain events to handlers discovered via Scrutor assembly scanning. A `[RequiresCapability]` attribute gates certain handlers from being registered in hosts that lack the required infrastructure (e.g., Email, Quartz). The Worker host and API host call the same module DI methods but pass different `HostCapability` flags to control what gets registered.

This works but it is more complex than necessary: dependencies are hidden behind the mediator, IDE navigation does not go directly to handlers, and the capability attribute system requires knowing the scan-time filtering rules. For a small, bounded application this machinery is overhead.

The goal is to replace it with straightforward constructor injection and explicit DI registration — patterns any .NET developer recognises without needing to understand project-specific conventions.

## Goals / Non-Goals

**Goals:**
- Remove `IMediator`, `Mediator`, `RequiresCapabilityAttribute`, `HostCapability`, and Scrutor
- Inject concrete handler classes everywhere injection is statically possible
- Replace assembly scanning with explicit, readable DI registration lists
- Introduce a clean `AddXModule()` / `AddXModuleWorker()` split that makes deployment topology visible at the call site
- Make `MessageTypeRegistry` populated explicitly rather than by scanning

**Non-Goals:**
- Changing any user-facing behaviour
- Changing the `IntegrationEventRouter` dispatch mechanism (keyed services pattern stays)
- Changing the `DomainEventsInterceptor` scope or transactional behaviour
- Removing `IDomainEventHandler<T>` or `IIntegrationEventHandler<T>` interfaces (still needed for type-erased runtime dispatch)

## Decisions

### D1 — Inject concrete handler types, not interfaces, wherever the type is statically known

**Decision**: Endpoints, facades, and integration event handlers inject the concrete handler class (e.g., `CreateTeamHandler`) rather than `ICommandHandler<CreateTeamCommand>`.

**Rationale**: The interface adds no value when there is exactly one implementation and the caller knows the type. Concrete injection means F12 navigates directly to the handler, constructor dependencies are visible in the handler class, and no registration interface is needed.

**Alternative considered**: Keep `ICommandHandler<T>` everywhere for consistency. Rejected — the interface only has value where there are multiple implementations or where the type must be resolved at runtime.

### D2 — Keep `ICommandHandler<T>` only for queue-dispatched commands

**Decision**: `ICommandHandler<T>` interface registration is used only for commands that travel through the Azure Storage Queue as serialized CloudEvents. Today: `RegisterExternalUserCommand` and `TriggerBulkEmailJobCommand`. `QueueMessageDispatcher` resolves these type-erased at runtime.

**Rationale**: These are the only sites where the command type is unknown at compile time. Every other command dispatch is statically known.

### D3 — `IDomainEventHandler<T>` and `IIntegrationEventHandler<T>` stay interface-registered

**Decision**: Domain and integration event handlers remain registered behind their generic interfaces.

**Rationale**: `DomainEventsInterceptor` and `IntegrationEventRouter` receive an `IDomainEvent` / `IIntegrationEvent` whose concrete type is only known at runtime. They must resolve handlers via `IServiceProvider.GetServices(handlerInterfaceType)`. This is an inherent property of event dispatch and cannot be replaced with static injection.

**Alternative considered**: Pre-compute a static dispatch table at startup. Rejected — more complexity than the existing `IServiceProvider.GetServices` call for no meaningful benefit.

### D4 — `DomainEventsInterceptor` uses `IServiceProvider` directly, no mediator wrapper

**Decision**: The interceptor (which already holds `IServiceProvider`) resolves `IDomainEventHandler<T>` directly via reflection-assisted `GetServices`, removing the `IMediator` dependency entirely.

**Rationale**: `IMediator.PublishDomainEventAsync` was itself just `IServiceProvider.GetServices` wrapped in an activity span. The span can stay — it is moved into the interceptor directly.

**What replaces it**:
```csharp
var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
var handlers = serviceProvider.GetServices(handlerType);
// dispatch via reflection (same logic, no new abstraction)
```

### D5 — `AddXModule()` collapses Application + Infrastructure; `AddXModuleWorker()` is a superset

**Decision**:
- `AddXModule()` — replaces `AddXApplicationServices()` + `AddXInfrastructureServices()`. Used by both API and Worker.
- `AddXModuleWorker()` — calls `AddXModule()` internally, then registers integration event handlers (keyed), queue command handlers, Quartz jobs, and this module's message types in the `MessageTypeRegistryBuilder`.

**Rationale**: Callers (Program.cs) should not need to know that a module has separate application and infrastructure layers. The App/Infrastructure split is an internal concern. The Worker/non-Worker split is a deployment topology concern and belongs at the call site.

**Result**:
```
API:    AddOrganizationModule() / AddRegistrationsModule() / AddEmailModule()
Worker: AddOrganizationModuleWorker() / AddRegistrationsModuleWorker() / AddEmailModuleWorker()
```

### D6 — `MessageTypeRegistry` is built explicitly via a `MessageTypeRegistryBuilder`

**Decision**: Replace assembly scanning in `MessageTypeRegistry` with a builder that each module's Worker DI method populates. The Worker's Program.cs calls `ConfigureMessageTypes(...)` which passes a builder to each module.

**Rationale**: Assembly scanning registers every `ICommand` and `IIntegrationEvent` type it finds, including ones that do not travel the queue. The explicit list is the definitive declaration of what the Worker handles.

**Shape**:
```csharp
// Worker Program.cs
builder.Services.AddMessageTypeRegistry(types =>
{
    types.AddOrganizationMessageTypes();
    types.AddRegistrationsMessageTypes();
    types.AddEmailMessageTypes();
});

// Each module contributes:
static void AddOrganizationMessageTypes(this MessageTypeRegistryBuilder types)
{
    types.AddCommand<RegisterExternalUserCommand>();
    types.AddIntegrationEvent<TicketedEventCreatedIntegrationEvent>();
    // ...
}
```

### D7 — Integration event handlers are only registered in the Worker

**Decision**: Integration event handler registrations move from the shared `AddXApplicationServices()` (which was called by both hosts) into `AddXModuleWorker()`.

**Rationale**: Integration event handlers only execute when the queue consumer is running, which is exclusively in the Worker. Registering them in the API was harmless but wasteful and conceptually wrong.

### D8 — Facade classes inject concrete query handlers

**Decision**: `OrganizationFacade` and `RegistrationsFacade` replace `IMediator` with direct constructor injection of each query handler they delegate to.

**Rationale**: Facades are thin delegation classes — injecting the handlers they actually call makes their dependencies explicit. The facade interface (`IOrganizationFacade`, `IRegistrationsFacade`) is unchanged, so cross-module callers are unaffected.

## Risks / Trade-offs

**Loss of per-handler OpenTelemetry spans from Mediator** → The Mediator wrapped every handler call in an activity span. With direct injection, those spans disappear. HTTP endpoint spans (from ASP.NET Core) cover request-level tracing; queue receive spans (from `IntegrationEventRouter` and `QueueMessageDispatcher`) cover queue-level tracing. The only gap is domain event handlers, whose spans move into `DomainEventsInterceptor`. Overall tracing fidelity is preserved.

**Explicit registration list must be kept in sync when handlers are added** → With scanning, a new handler was automatically discovered. Now it must be added to the module's DI method. Risk: a handler is added but not registered, causing a runtime resolution failure. Mitigation: this is a compile-time-visible omission — the injection site will not compile if the handler type is not registered. Architecture tests can additionally verify that every concrete handler class is registered.

**`AddXModuleWorker()` calling `AddXModule()` internally is a convention, not enforced** → If a developer calls only `AddXModuleWorker()` in the Worker, they get everything. If they call only `AddXModule()`, they get no queue handlers. There is no way to accidentally get a "partial" Worker registration. The risk is forgetting to call `AddXModuleWorker()` in the Worker entirely — caught immediately by missing queue handler registrations.

## Migration Plan

This is an in-process refactor with no database changes and no external API changes. No migration plan is required. The change can be applied in one PR. The application should continue to function identically after the change — verified by the existing end-to-end test suite.
