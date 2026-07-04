## 1. Remove Non-Generic Handler Interfaces

- [x] 1.1 Delete `ICommandHandler` (non-generic) — remove the interface and its `HandleAsync(ICommand, CancellationToken)` method entirely from `ICommandHandler.cs`
- [x] 1.2 Remove `: ICommandHandler` base and the `ICommandHandler.HandleAsync` bridge DIM from `ICommandHandler<TCommand>`
- [x] 1.3 Remove `: ICommandHandler` base and the `ICommandHandler.HandleAsync` bridge DIM from `ICommandHandler<TCommand, TResult>`
- [x] 1.4 Delete `IDomainEventHandler` (non-generic) — remove the interface and its `HandleAsync(IDomainEvent, CancellationToken)` method entirely from `IDomainEventHandler.cs`
- [x] 1.5 Remove `: IDomainEventHandler` base and the `IDomainEventHandler.HandleAsync` bridge DIM from `IDomainEventHandler<TDomainEvent>`

## 2. Update Mediator — Cached Delegate Dispatch

- [x] 2.1 Add a `ConcurrentDictionary<Type, Func<IServiceProvider, ICommand, CancellationToken, ValueTask>>` field to `Mediator` for command dispatch caching
- [x] 2.2 Add a `ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>>` field to `Mediator` for domain event dispatch caching
- [x] 2.3 Add private static generic method `DispatchCommandAsync<TCommand>(IServiceProvider, ICommand, CancellationToken)` that resolves `ICommandHandler<TCommand>` from DI and calls the typed `HandleAsync`
- [x] 2.4 Add private static generic method `DispatchDomainEventAsync<TDomainEvent>(IServiceProvider, IDomainEvent, CancellationToken)` that resolves all `IDomainEventHandler<TDomainEvent>` from DI and calls each
- [x] 2.5 Update `Mediator.SendCommandAsync(ICommand)` to use the command dispatch cache (look up or create delegate via `Delegate.CreateDelegate` on `DispatchCommandAsync<T>`, then invoke)
- [x] 2.6 Update `Mediator.PublishDomainEventAsync(IDomainEvent)` to use the domain event dispatch cache (look up or create delegate via `Delegate.CreateDelegate` on `DispatchDomainEventAsync<T>`, then invoke)

## 3. Verify & Test

- [x] 3.1 Confirm build succeeds — no remaining references to the deleted non-generic interfaces
- [x] 3.2 Run architecture tests: `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`
- [x] 3.3 Run full test suite to confirm no regressions
