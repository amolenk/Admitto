## 1. Centralise module-key convention

- [x] 1.1 Add `internal static string GetModuleKey(Type type)` to `MessageTypeRegistry` — extracts segment 3 from the `Amolenk.Admitto.Core.<Module>` namespace; throws `InvalidOperationException` for non-conforming types
- [x] 1.2 Update `MessageTypeRegistryBuilder` to call `MessageTypeRegistry.GetModuleKey(type)` instead of its own private `ModuleNameFor` helper, then delete `ModuleNameFor`

## 2. Rewrite QueueMessageDispatcher

- [x] 2.1 Remove the `IntegrationEventRouter` constructor parameter; add `IUnitOfWork` access via keyed service resolution in the dispatch loop
- [x] 2.2 Add a per-handler-interface-type cached invoker: `ConcurrentDictionary<Type, Func<object, object, CancellationToken, ValueTask>>` built from `MethodInfo.Invoke` on `HandleAsync`
- [x] 2.3 Replace the `switch (entry.Kind)` block with a unified `DispatchToHandlersAsync` method that: (a) builds the closed handler interface type, (b) calls `GetServices(handlerInterfaceType)`, (c) for each handler invokes `HandleAsync` via the cached invoker, (d) resolves the keyed `IUnitOfWork` using `MessageTypeRegistry.GetModuleKey(handler.GetType())`, and (e) calls `SaveChangesAsync`
- [x] 2.4 Add a warning log when `GetServices` returns no handlers for a recognised message type
- [x] 2.5 Move per-handler activity tracing (currently in `IntegrationEventRouter`) into the unified loop with tags for message kind, type, handler type, and module key

## 3. Delete dead code

- [x] 3.1 Delete `IntegrationEventRouter.cs`
- [x] 3.2 Delete `IntegrationEventSubscriber.cs`
- [x] 3.3 Remove the `AddScoped<IntegrationEventRouter>()` registration from `DependencyInjection.AddSharedInfrastructureQueueConsumer`

## 4. Add assembly-scanning helpers

All modules live in a single `Admitto.Core` assembly, so scanning helpers must accept a **namespace prefix** to limit scanning to a given module's types. Signature pattern: `AddXxxFromAssembly(Assembly assembly, string namespacePrefix)`.

Three helpers on `IServiceCollection` (in the shared infrastructure layer), plus one on `MessageTypeRegistryBuilder`:

- [x] 4.1 `AddConcreteCommandHandlersFromAssembly(Assembly assembly, string namespacePrefix)` — scans for non-abstract types in `namespacePrefix.*` implementing any `ICommandHandler<>` variant and registers each as `AddScoped<THandler>()` (concrete only, no interface mapping).
- [x] 4.1b `AddConcreteQueryHandlersFromAssembly(Assembly assembly, string namespacePrefix)` — same as above but for `IQueryHandler<>` variants.
- [x] 4.2 `AddDomainEventHandlersFromAssembly(Assembly assembly, string namespacePrefix)` — scans for non-abstract types in `namespacePrefix.*` implementing closed `IDomainEventHandler<TEvent>` and registers each as `AddScoped<IDomainEventHandler<TEvent>, THandler>()`. Used in `AddModule` methods.
- [x] 4.3 `AddIntegrationEventHandlersFromAssembly(Assembly assembly, string namespacePrefix)` — scans for non-abstract types in `namespacePrefix.*` implementing closed `IIntegrationEventHandler<TEvent>` and registers each as `AddScoped<IIntegrationEventHandler<TEvent>, THandler>()`. Used in `AddModuleWorker` methods (and `AddEmailModule` which keeps integration event handlers there).
- [x] 4.4 `AddFromAssembly(Assembly assembly, string namespacePrefix)` on `MessageTypeRegistryBuilder` — scans for non-abstract concrete types in `namespacePrefix.*` implementing `ICommand` or `IIntegrationEvent` and calls `AddCommand<T>()` / `AddIntegrationEvent<T>()` for each via reflection.

## 5. Update module DI to use scanning

**Registration module — no interface changes needed** (commands/queries were already concrete-only):
- [x] 5.1 `AddRegistrationsModule`: replace per-type concrete handler registrations with `services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations")` and `AddConcreteQueryHandlersFromAssembly(...)`; replace domain event handler registrations with `services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations")`
- [x] 5.2 `AddRegistrationsModuleWorker`: replace single `IIntegrationEventHandler<>` registration with `services.AddIntegrationEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations")`
- [x] 5.3 `AddRegistrationsMessageTypes`: replace per-type calls with `builder.AddFromAssembly(assembly, "Amolenk.Admitto.Core.Registrations")` — scans only Registrations-owned types; removes the need for other modules to re-register them

**Organization module — remove dead interface registrations first:**
- [x] 5.4 `AddOrganizationModule`: remove ALL `ICommandHandler<>` and `IQueryHandler<>` interface registrations (the factory-lambda lines — these are dead code, no endpoint resolves by interface); replace per-type concrete registrations with `services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization")` and `AddConcreteQueryHandlersFromAssembly(...)`; replace the domain event handler registration with `services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization")`
- [x] 5.5 `AddOrganizationModuleWorker`: replace per-type `IIntegrationEventHandler<>` registrations with `services.AddIntegrationEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization")`; keep the explicit `ICommandHandler<RegisterExternalUserCommand>` registration (concrete already covered by 5.4 scan, only the interface mapping needs to stay explicit here)
- [x] 5.6 `AddOrganizationMessageTypes`: replace with `builder.AddFromAssembly(assembly, "Amolenk.Admitto.Core.Organization")` — scans only Organization-owned types; remove cross-module event registrations (Registrations-owned events are covered by 5.3)

**Email module — remove dead interface registrations first:**
- [x] 5.7 `AddEmailModule`: remove ALL `ICommandHandler<>` and `IQueryHandler<>` interface registrations (dead code); replace per-type concrete registrations with `services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email")` and `AddConcreteQueryHandlersFromAssembly(...)`; replace domain event handler registration with `services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email")`; replace integration event handler registrations with `services.AddIntegrationEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email")`
- [x] 5.8 `AddEmailMessageTypes`: replace with `builder.AddFromAssembly(assembly, "Amolenk.Admitto.Core.Email")` — only Email-owned types; cross-module Registrations events are already covered by 5.3

## 6. Verify

- [x] 6.1 Run `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` — fix any architectural violations
- [x] 6.2 Run the full test suite; confirm no regressions
