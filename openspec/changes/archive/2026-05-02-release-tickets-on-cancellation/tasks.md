## 1. Domain Layer

- [x] 1.1 Add `ReleaseCapacity()` method to `TicketType` that decrements `UsedCapacity`, clamped at zero
- [x] 1.2 Add `Release(IReadOnlyList<string> slugs)` method to `TicketCatalog` that calls `ReleaseCapacity()` for each matching ticket type slug, silently skipping unknown slugs

## 2. Shared Infrastructure — ProcessedMessage / IInboxDbContext

- [x] 2.1 Create `ProcessedMessage` entity in `Admitto.Module.Shared/Infrastructure/Persistence/Inbox/` with properties `Id` (Guid), `MessageKey` (string), `ProcessedAt` (DateTimeOffset) — mirroring the legacy `MessageLog` shape
- [x] 2.2 Create `IInboxDbContext` interface in `Admitto.Module.Shared/Infrastructure/Persistence/Inbox/` with `DbSet<ProcessedMessage> ProcessedMessages` — mirroring `IOutboxDbContext`
- [x] 2.3 Add EF Core `ProcessedMessageEntityConfiguration` with a unique index on `MessageKey`

## 3. Application — ReleaseTickets Use Case

- [x] 3.1 Create `ReleaseTicketsCommand` record in `Application/UseCases/Registrations/ReleaseTickets/` with properties `RegistrationId` and `TicketedEventId`
- [x] 3.2 Create `ReleaseTicketsHandler` implementing `ICommandHandler<ReleaseTicketsCommand>` that loads the `Registration`, loads the `TicketCatalog` (returning early if not found), and calls `catalog.Release(ticketSlugs)`

## 4. Application — Event Handler Wiring

- [x] 4.1 Create `EventHandlers/` subfolder under `ReleaseTickets/`
- [x] 4.2 Create `RegistrationCancelledDomainEventHandler` implementing `IDomainEventHandler<RegistrationCancelledDomainEvent>` that dispatches `ReleaseTicketsCommand` via `IMediator`

## 5. Tests

- [x] 5.1 Add domain unit tests for `TicketType.ReleaseCapacity()` (normal decrement, clamp at zero)
- [x] 5.2 Add domain unit tests for `TicketCatalog.Release()` (releases matching slugs, skips unknown slugs)
- [x] 5.3 Add integration test covering SC001: cancelling a registration decrements `UsedCapacity` on the matching ticket types
- [x] 5.4 Add integration test covering SC002: cancelling a coupon-only registration with no catalog completes without error
- [x] 5.5 Add integration test covering SC003: `UsedCapacity` does not go below zero
- [x] 5.6 Add integration test covering SC004: unknown ticket slugs are silently skipped
