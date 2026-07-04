# Queue Message Dispatch

## Purpose

This capability covers how the worker dispatches queue messages (commands and integration events) to registered handlers, how module unit-of-work commits are keyed, and how handlers and message types are discovered via assembly scanning.

## Requirements

### Requirement: Unified handler dispatch for commands and integration events
The worker's `QueueMessageDispatcher` SHALL use a single dispatch loop for both commands and integration events. For a given message, it SHALL resolve all registered handlers for that message type via `GetServices`, invoke each handler's `HandleAsync` method, and commit the handler's module unit of work after each successful invocation. If no handlers are registered for a recognised message type, the dispatcher SHALL log a warning and complete without error.

#### Scenario: Command is dispatched and UoW committed
- **WHEN** a command message is received from the queue
- **THEN** the dispatcher resolves the registered `ICommandHandler<TCommand>`, calls `HandleAsync`, and calls `IUnitOfWork.SaveChangesAsync` keyed to the command handler's module

#### Scenario: Integration event is dispatched to all subscriber handlers
- **WHEN** an integration event message is received from the queue
- **THEN** the dispatcher resolves all registered `IIntegrationEventHandler<TEvent>` handlers, calls `HandleAsync` on each, and calls `IUnitOfWork.SaveChangesAsync` keyed to each handler's module in sequence

#### Scenario: No handlers registered for a message
- **WHEN** a message is received and no handlers are registered for its type
- **THEN** the dispatcher logs a warning and acknowledges the message without error

### Requirement: Module key resolved from handler namespace
The system SHALL derive the module key for UoW commit from the handler type's namespace using the convention `Amolenk.Admitto.Core.<Module>.*`. This SHALL be centralised in `MessageTypeRegistry.GetModuleKey(Type)` and reused by both the dispatcher and the registry builder.

#### Scenario: Module key extracted correctly for a handler in a subscriber module
- **WHEN** `MessageTypeRegistry.GetModuleKey` is called with a handler type whose namespace is `Amolenk.Admitto.Core.Email.Application.UseCases.*`
- **THEN** it returns `"Email"`

#### Scenario: Module key extracted correctly for a command handler
- **WHEN** `MessageTypeRegistry.GetModuleKey` is called with a handler type whose namespace is `Amolenk.Admitto.Core.Organization.Application.UseCases.*`
- **THEN** it returns `"Organization"`

#### Scenario: Exception for types outside the module namespace convention
- **WHEN** `MessageTypeRegistry.GetModuleKey` is called with a type that does not follow the `Amolenk.Admitto.Core.<Module>` convention
- **THEN** it throws `InvalidOperationException`

### Requirement: Assembly scanning for handler registration
The system SHALL provide an `AddHandlersFromAssembly(Assembly)` extension method on `IServiceCollection` that scans the given assembly for all non-abstract implementations of `ICommandHandler<>` and `IIntegrationEventHandler<>` and registers each as a scoped service without requiring Scrutor or other third-party scanning libraries.

#### Scenario: All command handlers in an assembly are registered
- **WHEN** `AddHandlersFromAssembly` is called with an assembly containing multiple `ICommandHandler<>` implementations
- **THEN** each implementation is registered as a scoped service for its closed `ICommandHandler<T>` interface

#### Scenario: All integration event handlers in an assembly are registered
- **WHEN** `AddHandlersFromAssembly` is called with an assembly containing multiple `IIntegrationEventHandler<>` implementations
- **THEN** each implementation is registered as a scoped service for its closed `IIntegrationEventHandler<T>` interface

#### Scenario: Abstract classes and interfaces are not registered
- **WHEN** `AddHandlersFromAssembly` is called with an assembly containing abstract `ICommandHandler<>` implementations
- **THEN** those abstract types are not registered

### Requirement: At-least-once message delivery with explicit settlement
The queue consumer SHALL use a push-based delivery mechanism. Messages SHALL be explicitly completed after successful dispatch, and abandoned (left for retry) on failure. The system SHALL guarantee at-least-once delivery: if the worker crashes after dispatch but before completion, the message will be redelivered.

#### Scenario: Message is completed after successful dispatch
- **WHEN** a message is received and all handlers dispatch without error
- **THEN** the message is explicitly completed (removed from the queue)

#### Scenario: Message is abandoned on dispatch failure
- **WHEN** a message is received and processing throws an exception
- **THEN** the message is abandoned and becomes available for redelivery

### Requirement: Pending outbox rows are retried after an age gate
Every module DbContext that implements `IOutboxDbContext` SHALL be registered for Worker-owned outbox retry processing. The retry scanner SHALL read bounded batches of `Pending` outbox rows older than the configured retry minimum age, send each row to the queue, and mark it `Sent` after successful queue send. The minimum age SHALL prevent the retry scanner from racing the unit-of-work's immediate post-commit outbox dispatch for newly committed rows.

#### Scenario: Old pending outbox row is dispatched
- **WHEN** a module outbox contains a `Pending` row older than the configured retry minimum age
- **THEN** the Worker retry scanner sends the message to the queue and marks the row `Sent`

#### Scenario: Recent pending outbox row is skipped
- **WHEN** a module outbox contains a `Pending` row newer than the configured retry minimum age
- **THEN** the Worker retry scanner leaves it `Pending` for a later scan

#### Scenario: Duplicate scanner race is tolerated
- **WHEN** two Worker instances race while dispatching the same eligible pending outbox row
- **THEN** at least one queue send succeeds, the row eventually becomes `Sent`, and downstream idempotency handles any duplicate delivery

### Requirement: Assembly scanning for message type registry
The system SHALL provide an `AddFromAssembly(Assembly)` method on `MessageTypeRegistryBuilder` that scans the given assembly for concrete implementations of `ICommand` and `IIntegrationEvent` and registers each in the registry, equivalent to calling `AddCommand<T>()` or `AddIntegrationEvent<T>()` for each discovered type individually.

#### Scenario: All commands in an assembly are added to the registry
- **WHEN** `AddFromAssembly` is called with an assembly containing multiple `ICommand` implementations
- **THEN** each concrete command type is registered in the message type registry

#### Scenario: All integration events in an assembly are added to the registry
- **WHEN** `AddFromAssembly` is called with an assembly containing multiple `IIntegrationEvent` implementations
- **THEN** each concrete integration event type is registered in the message type registry
