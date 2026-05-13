## Why

`ICommandHandler` and `IDomainEventHandler` expose non-generic base interfaces whose sole purpose is to enable type-erased dispatch inside the mediator. This leaks an infrastructure concern into the handler abstraction — handler authors and the handler interfaces themselves should have no awareness of how the mediator resolves and calls them at runtime.

## What Changes

- **BREAKING (internal)** Delete `ICommandHandler` (non-generic base interface).
- **BREAKING (internal)** Delete `IDomainEventHandler` (non-generic base interface).
- `ICommandHandler<TCommand>` becomes a standalone interface (no base).
- `IDomainEventHandler<TDomainEvent>` becomes a standalone interface (no base).
- Remove the default interface bridge methods (`ICommandHandler.HandleAsync` and `IDomainEventHandler.HandleAsync`) from the generic interfaces — they only existed to satisfy the non-generic base.
- `Mediator.SendCommandAsync(ICommand)` and `Mediator.PublishDomainEventAsync(IDomainEvent)` absorb the type-erasure concern internally, using cached generic delegates (one reflection pass per message type, cached in a `ConcurrentDictionary`).

## Capabilities

### New Capabilities

_(none — this is a pure internal refactoring with no externally visible behaviour change)_

### Modified Capabilities

_(none — no spec-level requirements change)_

## Impact

- **`Admitto.Core/Shared/Application/Messaging/`** — `ICommandHandler.cs` and `IDomainEventHandler.cs` are simplified; `Mediator.cs` gains private cached-delegate dispatch logic.
- No changes to DI registrations, API contracts, or module handler implementations.
- The change is breaking only in the sense that any code that directly references `ICommandHandler` (non-generic) or `IDomainEventHandler` (non-generic) will fail to compile — a search confirms only the mediator itself does this.
