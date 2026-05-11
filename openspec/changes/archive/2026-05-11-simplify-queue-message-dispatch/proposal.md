## Why

The `MessageQueueProcessor` currently uses two different dispatch mechanisms for commands and integration events. The integration event path (`IntegrationEventRouter` + `IntegrationEventSubscriber`) is also silently broken: handlers are registered as non-keyed services but the router looks for keyed services and iterates over `IntegrationEventSubscriber` instances that are never registered — meaning integration events dispatched from the queue are never actually handled. Commands reach their handlers but never commit their unit of work, so EF changes are silently dropped. Fixing this is the right moment to unify both paths and add lightweight assembly scanning so module DI stays concise.

## What Changes

- **Remove** `IntegrationEventRouter` and `IntegrationEventSubscriber` (dead code — router never finds any subscribers).
- **Unify** command and integration-event dispatch in `QueueMessageDispatcher`: resolve all handlers with `GetServices`, invoke each, and commit the handler's module `IUnitOfWork` keyed by the module extracted from the handler's namespace via a new `MessageTypeRegistry.GetModuleKey(Type)` helper.
- **Fix** the missing UoW commit for queue-dispatched commands (current bug: changes are lost).
- **Add** assembly-scanning registration helpers to `MessageTypeRegistryBuilder` and `IServiceCollection` extensions so each module can register its handlers and registry entries in one call rather than one `AddScoped` per type.

## Capabilities

### New Capabilities

- `queue-message-dispatch`: Internal infrastructure capability describing how the worker dispatches queued messages (commands and integration events) to handlers and commits the per-module unit of work. Covers the unified dispatch loop, module-key resolution, UoW commit, and assembly-scanning registration.

### Modified Capabilities

*(none — no user-facing or API-level requirement changes)*

## Impact

- `Admitto.Core/Shared/Infrastructure/Messaging/` — `QueueMessageDispatcher` rewritten; `IntegrationEventRouter` and `IntegrationEventSubscriber` deleted.
- `Admitto.Core/Shared/Infrastructure/Messaging/MessageTypeRegistry` — new `GetModuleKey(Type)` internal static helper.
- `Admitto.Core/Shared/Infrastructure/Messaging/MessageTypeRegistryBuilder` — duplicated `ModuleNameFor` removed (replaced by `GetModuleKey`); optional assembly-scanning overloads added.
- `Admitto.Core/Shared/Infrastructure/DependencyInjection` — `IntegrationEventRouter` scoped registration removed; new assembly-scanning extension methods added.
- Each module's `Application/DependencyInjection` — individual `AddScoped<ICommandHandler<…>>` / `AddScoped<IIntegrationEventHandler<…>>` calls replaced by single scan calls.
- No API contract changes, no database migrations, no breaking changes for callers outside the infrastructure layer.
