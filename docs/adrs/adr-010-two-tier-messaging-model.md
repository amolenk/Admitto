# ADR-010: Drop ModuleEvent / MessagePolicy — two-tier messaging model

## Status
Accepted.

## Context
The original messaging model had three tiers:

1. **Domain event** — synchronous, in-transaction, dispatched via `IMediator`.
2. **Module event** — async, written to the outbox by `MessagePolicy`; consumed within a module or between modules by `IModuleEventHandler<T>`. Defined in `Application/ModuleEvents/`.
3. **Integration event** — async, public, versioned cross-module contract. Defined in `*.Contracts/IntegrationEvents/`.

Module events existed to give internal workflows an abstraction layer that could evolve independently of the public integration contract. In practice:

- Every `IModuleEventHandler<T>` either dispatched a command to the same module's mediator or simply re-emitted an integration event to the outbox. No module event ever contained richer data than the domain event that triggered it.
- Module events are value-object–rich (they carry VO instances), which is fine inside the module, but the `OutboxWriter` serialised them to JSON anyway — VOs crossed the wire as serialised primitives inside a JSON envelope, making the "internal / no-schema" advantage illusory.
- `MessagePolicy` was a bespoke mapping DSL (`AddRule<TDomainEvent, TModuleEvent>()`) that duplicated what a standard `IDomainEventHandler<T>` class already expresses, but with less type safety and no Scrutor auto-discovery.
- `DomainEventsInterceptor` was responsible for both dispatching domain events *and* invoking `OutboxWriter.TryEnqueue` — two unrelated concerns mixed in one place.

## Decision

Collapse the three-tier model to **two tiers**:

| Tier | How dispatched | When to use |
| :--- | :------------- | :---------- |
| Domain event | Synchronous, via `IMediator` in `DomainEventsInterceptor` | All in-process, in-transaction side effects |
| Command / Integration event on the outbox | Async, via `IOutbox` injected into an `IDomainEventHandler<T>` | Any deferred or cross-module work |

Concretely:

- `IMessagePolicy`, `MessagePolicy`, `MessagePolicyRule`, `MessagePolicyRuleBuilder<T>` — deleted.
- `IModuleEvent`, `ModuleEvent`, `IModuleEventHandler<T>`, `ModuleEventRouter` — deleted.
- `OutboxWriter` — deleted; outbox writes are now performed directly by `IDomainEventHandler<T>` implementations that inject `[FromKeyedServices(…)] IOutbox`.
- `IIntegrationEventOutbox` — renamed to `IOutbox` and extended with an `Enqueue(ICommand command)` overload, so internal async work (previously a module event → module event handler chain) becomes a plain `ICommand` on the same outbox.
- `DomainEventsInterceptor` — simplified to pure domain event dispatch only; outbox writes happen inside domain event handler implementations.

### Replacement pattern for former module-event wiring

**Before** (MessagePolicy + module event handler):
```csharp
// OrganizationMessagePolicy
AddRule<UserCreatedDomainEvent, UserCreatedModuleEvent>();

// UserCreatedModuleEventHandler
outboxWriter.Enqueue(new RegisterExternalUserCommand(…));
```

**After** (one domain event handler):
```csharp
internal sealed class UserCreatedDomainEventHandler(
    [FromKeyedServices(OrganizationModuleKey.Value)] IOutbox outbox)
    : IDomainEventHandler<UserCreatedDomainEvent>
{
    public ValueTask HandleAsync(UserCreatedDomainEvent e, CancellationToken ct)
    {
        outbox.Enqueue(new RegisterExternalUserCommand(e.UserId.Value)
        {
            CommandId = DeterministicCommandId<RegisterExternalUserCommand>.Create(e.EventId.Value)
        });
        return ValueTask.CompletedTask;
    }
}
```

## Consequences

**Positive**
- Fewer concepts and files; the indirection through `ModuleEvent` + `IModuleEventHandler` is gone.
- `DomainEventsInterceptor` is now a pure dispatcher — no policy lookup, no `OutboxWriter` construction.
- Domain event handlers are plain classes discoverable by Scrutor; no separate `AddModuleEventHandlersFromAssembly` call needed.
- Commands on the outbox are idiomatic: command ID can be derived deterministically from the domain event ID, giving safe at-least-once semantics with no extra infrastructure.
- `IOutbox` is a clean interface with two overloads — `Enqueue(ICommand)` and `Enqueue(IIntegrationEvent)` — easier to mock in tests than the former `OutboxWriter` + `IMessagePolicy` pair.

**Negative / tradeoffs**
- Domain event handler classes must inject `IOutbox` and know the outbox type string convention indirectly (the `Outbox` implementation validates namespace/suffix). This is a minor leaky abstraction accepted for simplicity.
- The "`IModuleEvent` as an evolution buffer" argument no longer holds — any breaking change to an internal command type now requires a migration. In practice this was always the case for JSON-serialised module events as well.

## Alternatives considered

**Keep module events, drop `MessagePolicy` only** — replace the policy DSL with conventional `IDomainEventHandler<T>` classes that `outboxWriter.Enqueue(new FooModuleEvent(…))`. Rejected because `IModuleEventHandler<T>` and `ModuleEventRouter` would still exist with no benefit over a second `IDomainEventHandler<T>` on the module.

**Keep all three tiers, replace only the DSL** — same as above but keeping the concept. Rejected for the same reason.

**Use MediatR pipeline behaviour instead of explicit outbox injection** — route outbox writes through a domain event pipeline behaviour. Rejected because it hides the outbox dependency and makes deterministic command IDs harder to attach.
