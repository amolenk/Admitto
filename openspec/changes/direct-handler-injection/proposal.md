## Why

The current messaging layer uses a custom `IMediator` + Scrutor assembly scanning to discover and dispatch handlers, combined with a `[RequiresCapability]` attribute to filter registrations per host. This is unnecessary indirection for a small application: it hides dependencies, prevents IDE navigation (F12), and adds scanning complexity that should not exist in a simple codebase. Removing it in favour of direct constructor injection eliminates an entire layer of magic with no loss of functionality.

## What Changes

- **Remove `IMediator` and `Mediator`**: Endpoints, facades, and event handlers inject concrete handler classes directly instead of routing through a mediator.
- **Remove Scrutor**: Assembly scanning for handlers is replaced with explicit per-module DI registration lists.
- **Remove `[RequiresCapability]` / `HostCapability`**: Capability gating via attributes is replaced by a clean DI method split per module.
- **Introduce `AddXModule()` / `AddXModuleWorker()` pattern**: Each module exposes two DI methods. `AddXModule()` collapses the current Application + Infrastructure registration into a single call (used by both API and Worker). `AddXModuleWorker()` internally calls `AddXModule()` and adds Worker-only registrations (integration event handlers, queue-dispatched command handlers, Quartz jobs).
- **Explicit `MessageTypeRegistry`**: Assembly scanning in `MessageTypeRegistry` is replaced with an explicit builder populated by each module's Worker DI method.
- **`DomainEventsInterceptor` uses `IServiceProvider` directly**: The interceptor already holds `IServiceProvider`; the Mediator indirection is removed.
- **`ICommandHandler<T>` kept only for queue-dispatched commands**: Only commands that are serialized and sent through the Azure Storage Queue are registered behind this interface. Today that is `RegisterExternalUserCommand` and `TriggerBulkEmailJobCommand`.
- **`IDomainEventHandler<T>` and `IIntegrationEventHandler<T>` kept**: These remain interface-registered because `DomainEventsInterceptor` and `IntegrationEventRouter` resolve them type-erased at runtime.

## Capabilities

### New Capabilities

None — this is a pure infrastructure refactor with no user-facing behaviour changes.

### Modified Capabilities

None — no specification-level requirements change.

## Impact

- **`Admitto.Core`**: All three modules (Organization, Registrations, Email) — DI files rewritten, handler constructors updated.
- **`Admitto.Api`**: `Program.cs` simplified to `AddXModule()` calls; endpoint handlers switch from `IMediator` to concrete handler injection.
- **`Admitto.Worker`**: `Program.cs` simplified to `AddXModuleWorker()` calls.
- **`Admitto.Core.Shared`**: `IMediator`, `Mediator`, `RequiresCapabilityAttribute`, `HostCapability`, and all scan-based DI helpers removed. `DomainEventsInterceptor` trimmed.
- **Dependencies**: Scrutor package removed from `Admitto.Core`.
- **Tests**: Architecture tests and unit tests updated to reflect removed types; no behaviour changes expected.
