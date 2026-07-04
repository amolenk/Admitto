## Context

`ICommandHandler` and `IDomainEventHandler` currently use non-generic base interfaces as a dispatch bridge. The mediator resolves the concrete generic handler type from DI via `MakeGenericType`, casts it to the non-generic base, and calls the non-generic `HandleAsync`. The generic interfaces implement the non-generic method as a default interface method that casts back to the typed overload.

This means the handler interfaces carry infrastructure plumbing that has nothing to do with the handler's responsibility.

```
Current shape:

  ICommandHandler                     ← non-generic base (infrastructure concern)
    HandleAsync(ICommand, ct)

  ICommandHandler<TCommand> : ICommandHandler
    HandleAsync(TCommand, ct)         ← what the handler author cares about
    ICommandHandler.HandleAsync(...)  ← bridge DIM (auto-generated boilerplate)

Mediator.SendCommandAsync(ICommand):
  1. MakeGenericType → ICommandHandler<Concrete>
  2. DI resolve → cast to ICommandHandler  ← relies on inheritance
  3. handler.HandleAsync(command, ct)       ← calls non-generic method → bridge → typed
```

## Goals / Non-Goals

**Goals:**
- Remove `ICommandHandler` (non-generic) and `IDomainEventHandler` (non-generic) entirely.
- Make `ICommandHandler<TCommand>` and `IDomainEventHandler<TDomainEvent>` standalone interfaces with no base.
- Absorb the type-erasure dispatch concern entirely inside `Mediator`.
- Zero performance regression at steady state.

**Non-Goals:**
- Changing the `IMediator` public API — `SendCommandAsync(ICommand)` and `PublishDomainEventAsync(IDomainEvent)` remain as-is.
- Changing DI registration patterns.
- Touching `IQueryHandler` — it is already clean (fully typed path only).
- Modifying handler implementations — they have no visible change.

## Decisions

### Decision 1: Cached generic delegate dispatch

**Chosen approach**: `ConcurrentDictionary<Type, Func<...>>` caching delegates built once per message type via reflection.

```
First call for a given command type T:
  1. Build a MethodInfo for private static DispatchCommandAsync<T>(IServiceProvider, ICommand, CancellationToken)
  2. Wrap with Delegate.CreateDelegate → Func<IServiceProvider, ICommand, CancellationToken, ValueTask>
  3. Store in _commandDispatchers[T]

Subsequent calls:
  → dictionary lookup (lock-free read) → call delegate directly
  → zero reflection, zero extra allocation
```

**Alternatives considered:**

| Approach | Allocation | Complexity | Chosen? |
|---|---|---|---|
| Cached delegate (static generic method) | Zero at steady state | Low | ✅ |
| Internal adapter object `CommandDispatcher<T>` | One per dispatch | Very low | ❌ |
| `MethodInfo.Invoke` per call | Boxes `ValueTask` | High (perf) | ❌ |
| Keep non-generic base interface | Zero | Zero | ❌ (design goal) |

**Rationale**: The cached delegate pattern is the same approach used by MediatR. It eliminates all reflection cost after the first dispatch per type and keeps the handler interfaces clean.

### Decision 2: Private static generic dispatch methods

The delegate targets are **private static** methods on `Mediator`:

```csharp
private static ValueTask DispatchCommandAsync<TCommand>(
    IServiceProvider sp, ICommand cmd, CancellationToken ct)
    where TCommand : ICommand
{
    var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
    return handler.HandleAsync((TCommand)cmd, ct);
}

private static async ValueTask DispatchDomainEventAsync<TDomainEvent>(
    IServiceProvider sp, IDomainEvent evt, CancellationToken ct)
    where TDomainEvent : IDomainEvent
{
    var handlers = sp.GetServices<IDomainEventHandler<TDomainEvent>>();
    foreach (var handler in handlers)
        await handler.HandleAsync((TDomainEvent)evt, ct);
}
```

Using static methods (not instance) simplifies `CreateDelegate` — no bound instance needed.

**Note**: The activity/logging wrappers (`HandleWithActivityAsync`) still apply. The cached delegate calls the private dispatch method which then calls the handler; the activity wrapper sits at the `SendCommandAsync`/`PublishDomainEventAsync` level as today.

## Risks / Trade-offs

- **[One reflection pass per type on first dispatch]** → Mitigated by DI warm-up at application start. In practice all types are dispatched during startup or early requests.
- **[`ConcurrentDictionary` lookup overhead]** → Negligible; lock-free read path for existing keys.
- **[Cast `(TCommand)cmd` could throw if message type doesn't match registered handler]** → Same risk as today (the cast exists in the bridge method). The registry and DI ensure correctness.

## Migration Plan

1. Remove `ICommandHandler` (non-generic) and `IDomainEventHandler` (non-generic).
2. Strip the `: ICommandHandler` / `: IDomainEventHandler` base and bridge DIM from the generic interfaces.
3. Add private static dispatch methods and `ConcurrentDictionary` caches to `Mediator`.
4. Update `SendCommandAsync` and `PublishDomainEventAsync` on `Mediator` to use the cache.
5. Verify build — no other callers reference the non-generic interfaces.
6. Run architecture tests and full test suite.

No runtime migration or rollback complexity — this is a compile-time-only change with no data or API surface impact.

## Open Questions

_(none)_
