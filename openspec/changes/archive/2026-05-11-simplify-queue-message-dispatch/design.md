## Context

The worker's queue consumer pipeline currently has two separate dispatch paths:

- **Commands** (`QueueMessageDispatcher.SendCommandAsync`): resolves `ICommandHandler<T>` via a cached delegate and calls `HandleAsync`. No `IUnitOfWork.SaveChangesAsync` is ever called, so EF Core change-tracking mutations are silently discarded.
- **Integration events** (`IntegrationEventRouter.DispatchAsync`): was designed around `IntegrationEventSubscriber` records (one per subscribing module) and keyed-service handler resolution. `IntegrationEventSubscriber` is never registered in DI, so the loop body never executes — integration events pulled from the queue are silently dropped.

Separately, every module's `Application/DependencyInjection` manually registers each command and integration event handler with an individual `AddScoped<ICommandHandler<T>, Handler>()` call, and the Worker's message type registry is likewise built entry-by-entry. This is verbose and error-prone — forgetting a registration causes a silent runtime miss.

## Goals / Non-Goals

**Goals:**
- Unify command and integration event dispatch into a single loop in `QueueMessageDispatcher`.
- Fix the UoW commit gap: after every handler call, commit the handler's module `IUnitOfWork`.
- Fix the integration event dispatch bug: handlers must actually be invoked.
- Add convention-based assembly scanning so modules can register all handlers and registry entries in one call.
- Centralise the namespace → module-key convention in `MessageTypeRegistry.GetModuleKey(Type)`.

**Non-Goals:**
- Idempotency / duplicate-detection (separate concern, separate change).
- Dead-letter store for unresolvable messages (already noted as follow-up in the code).
- Splitting the Azue Storage Queue per module — single queue remains.
- Changing how endpoints commit UoW (endpoint handlers own their transaction boundary, unchanged).

## Decisions

### D1 — Unified dispatch loop via `GetServices`

Replace the command/integration-event split with a single path:

1. Resolve the concrete handler interface type (`ICommandHandler<T>` or `IIntegrationEventHandler<T>`) from the registry entry.
2. Call `serviceProvider.GetServices(handlerInterfaceType)` to get all registered handlers.
3. For each handler: invoke `HandleAsync` via a cached `MethodInfo`-based delegate, then resolve the keyed `IUnitOfWork` and call `SaveChangesAsync`.

**Alternative considered — keep two code paths, just fix each bug:** Rejected because it preserves unnecessary divergence. The only meaningful difference between commands and integration events in this pipeline is the handler interface type; everything else (scope lifetime, UoW commit) is identical.

### D2 — Module key from handler namespace, not from registry entry

For commands, `entry.ModuleName` equals the handler's module (command and its sole handler live in the same module). For integration events, `entry.ModuleName` is the *publisher's* module, which is wrong for commit purposes.

The solution: `MessageTypeRegistry.GetModuleKey(handler.GetType())` extracts the module name from segment 3 of the handler type's namespace (`Amolenk.Admitto.Core.<Module>.…`). This is the same convention already used by `MessageTypeRegistryBuilder` for message types; exposing it as an internal static on `MessageTypeRegistry` removes the duplication.

**Alternative considered — thread the module key through the DI registration:** Rejected because it would require annotating every handler registration with a key string, re-introducing the ceremony we want to eliminate.

### D3 — Assembly scanning without Scrutor

Scrutor is not worth adding as a dependency for one scanning use-case. Plain reflection suffices:

```csharp
assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract)
    .SelectMany(t => t.GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
        .Select(i => (Interface: i, Implementation: t)))
    .ForEach(pair => services.AddScoped(pair.Interface, pair.Implementation));
```

Two extension points:

- **`IServiceCollection.AddHandlersFromAssembly(Assembly)`** — scans for `ICommandHandler<>` and `IIntegrationEventHandler<>` implementations and registers them as non-keyed scoped services.
- **`MessageTypeRegistryBuilder.AddFromAssembly(Assembly)`** — scans for concrete `ICommand` and `IIntegrationEvent` implementations and adds them to the registry (equivalent to calling `AddCommand<T>()` / `AddIntegrationEvent<T>()` for each).

**Alternative considered — source generators:** Disproportionate complexity for the problem size.

### D4 — Per-handler UoW commit, not per-message

After each individual handler call, its module's `IUnitOfWork` is committed. This means:
- A failed handler throws before the commit, leaving the message unacknowledged for retry.
- If the same module has two handlers for the same integration event (unlikely but valid), each gets a discrete commit.
- Different subscriber modules get their UoWs committed sequentially inside one message processing scope.

**Alternative considered — single commit per message (all modules at once):** Rejected because it makes partial failures unrecoverable — if the second module's handler throws after the first module already did work, the whole message retries and the first module's work executes again. Per-handler commit limits blast radius and matches the current `IntegrationEventRouter` intention (even though that code never ran).

### D5 — Delete `IntegrationEventRouter` and `IntegrationEventSubscriber`

Both are dead code. Deleting them removes misleading infrastructure and avoids future confusion.

## Risks / Trade-offs

- **Reflection-based `HandleAsync` invocation** is slightly slower than the existing cached-delegate approach for commands. Acceptable: message processing is I/O-bound and the cache is per handler-interface type, so the reflection hit occurs only once per type.
- **`GetServices` returns an empty sequence** if a handler is not registered — the message is silently "handled" with no work done and no error. Mitigation: add a warning log when no handlers are found for a non-ignored message type.
- **`ICommandHandler<TCommand, TResult>` handlers** (returning a value) are not queue-dispatchable — `GetServices` for `ICommandHandler<>` will not find them. This is intentional: result-bearing commands are synchronous, request/response, not appropriate for async queue dispatch.

## Migration Plan

1. Add `MessageTypeRegistry.GetModuleKey(Type)` and update `MessageTypeRegistryBuilder` to use it (remove duplicate `ModuleNameFor`).
2. Rewrite `QueueMessageDispatcher` with the unified loop.
3. Remove `IntegrationEventRouter.cs` and `IntegrationEventSubscriber.cs`.
4. Update `DependencyInjection` to remove `IntegrationEventRouter` scoped registration; add `AddHandlersFromAssembly`.
5. Update each module's `Application/DependencyInjection` to use assembly scanning.
6. Update the Worker's message type registry builder to use `AddFromAssembly`.
7. Run architecture tests, then full test suite.

No database migrations. No deployment coordination needed — the worker can be restarted independently.

## Open Questions

*(none)*
