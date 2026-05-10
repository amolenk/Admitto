## Why

The current three-tier event model (DomainEvent → ModuleEvent → IntegrationEvent) introduces
an intermediate `ModuleEvent` type whose only real job is to carry primitives from a domain
event to a deferred command dispatch — a roundabout way to say "run this command later, outside
the current transaction." This is hard to explain and harder to justify: every `IModuleEventHandler<T>`
in the codebase does nothing but call `mediator.SendAsync(command)`. The `MessagePolicy` concept
adds a centralised mapping layer on top, which obscures intent and duplicates the VO → primitive
extraction that should happen once, in an explicit handler.

The cleaner model: from a `IDomainEventHandler<T>` (which runs in-transaction), you either call
the mediator directly (for in-tx work) or enqueue a `ICommand` or `IIntegrationEvent` via `IOutbox`
(for deferred/cross-module work). Domain events stay inside the process. Commands and integration
events already speak primitives and are the natural bus-safe types. No intermediate event layer needed.

## What Changes

- **BREAKING** Remove `IModuleEvent`, `ModuleEvent`, `IModuleEventHandler<T>`, `ModuleEventRouter`
- **BREAKING** Remove `IMessagePolicy`, `MessagePolicy`, and all per-module policy classes (`OrganizationMessagePolicy`, `EmailMessagePolicy`, `RegistrationsMessagePolicy`)
- **BREAKING** Remove `OutboxWriter` (outbox writes move into domain event handlers)
- Simplify `DomainEventsInterceptor`: drop `MessagePolicy`/`OutboxWriter` logic; become a pure domain event dispatcher
- Rename `IIntegrationEventOutbox` → `IOutbox`; add `Enqueue(ICommand)` overload alongside existing `Enqueue(IIntegrationEvent)`
- Rename `IntegrationEventOutbox` → `Outbox`; implement command serialization with `command.{module}.{name}` type convention
- Extend `MessageTypeRegistry` with `MessageKind.Command`
- Extend `QueueMessageDispatcher` with a `Command` routing branch → `mediator.SendAsync(command)`
- Convert each existing module event + handler pair into a `IDomainEventHandler<T>` that calls `IOutbox.Enqueue(command)`
- Convert each `MessagePolicy` integration event mapping into a `IDomainEventHandler<T>` that calls `IOutbox.Enqueue(integrationEvent)`
- Update `AddModuleDatabaseServices` to register `IOutbox` instead of `IIntegrationEventOutbox`
- Write an ADR capturing this decision and the reasoning behind the three-type taxonomy (DomainEvent / Command / IntegrationEvent)

## Capabilities

### New Capabilities

*(none — this is a pure architectural refactor with no user-facing behaviour change)*

### Modified Capabilities

*(none — existing async workflows retain identical observable behaviour; only the internal plumbing changes)*

## Impact

- **`Admitto.Core/Shared`**: `IOutbox`, `Outbox`, `DomainEventsInterceptor`, `QueueMessageDispatcher`, `MessageTypeRegistry`, `OutboxWriter` (deleted)
- **`Admitto.Core/Organization`**: `OrganizationMessagePolicy` deleted; `UserCreatedModuleEvent` + handler replaced by `UserCreatedDomainEventHandler`; `TicketedEventCreationRequestedDomainEvent` mapping replaced by a domain event handler
- **`Admitto.Core/Email`**: `EmailMessagePolicy` deleted; `BulkEmailJobRequestedModuleEvent` + handler replaced by `BulkEmailJobRequestedDomainEventHandler`
- **`Admitto.Core/Registrations`**: `RegistrationsMessagePolicy` deleted; any module events converted
- **`docs/arc42/08-crosscutting-concepts.md`**: §8.6 Messaging section rewritten to reflect the new two-tier outbox model and the ADR added
- **No API surface changes**, no database schema changes, no integration event contract changes
