## 1. Merge GetRegistrations and QueryRegistrations

- [x] 1.1 Add optional filter (`QueryRegistrationsDto?`) and optional team-ownership guard (`TeamId?`) parameters to `GetRegistrationsQuery`; remove `QueryRegistrationsQuery`
- [x] 1.2 Merge the EF Core filtering logic from `QueryRegistrationsHandler` into `GetRegistrationsHandler` (apply filter when present, apply team guard when `TeamId` is supplied)
- [x] 1.3 Update `GetRegistrationsHttpEndpoint` to continue passing `TeamId` and no filter (behaviour unchanged)
- [x] 1.4 Update `RegistrationsFacade.QueryRegistrationsAsync` to dispatch the merged `GetRegistrationsQuery` with the filter and no `TeamId`
- [x] 1.5 Delete the `QueryRegistrations/` folder and all its files
- [x] 1.6 Update `RegistrationsModule` wiring if any explicit handler registration referenced `QueryRegistrationsHandler`

## 2. Fix MaterializeTicketedEvent Flow

- [x] 2.1 Add `TicketedEventCreatedDomainEvent` to `Registrations/Domain/DomainEvents/`, carrying `CreationRequestId`, `TeamId`, `TicketedEventId`, and `TimeZone`
- [x] 2.2 Update `TicketedEvent.Create(...)` to raise `TicketedEventCreatedDomainEvent` before returning
- [x] 2.3 Create `MaterializeTicketedEventCommand` record (carrying all fields from the integration event including `CreationRequestId`)
- [x] 2.4 Create `MaterializeTicketedEventHandler` (command handler) that creates `TicketedEvent` and `TicketCatalog` and persists them via `IRegistrationsWriteStore`; remove outbox enqueue from this handler
- [x] 2.5 Update `TicketedEventCreationRequestedIntegrationEventHandler` to dispatch `MaterializeTicketedEventCommand` via `IMediator` instead of directly creating the aggregate; remove `IOutbox` and `IRegistrationsWriteStore` dependencies from this handler
- [x] 2.6 Add `IDomainEventHandler<TicketedEventCreatedDomainEvent>` to `RegistrationsIntegrationEventPublisher` that enqueues `TicketedEventCreatedIntegrationEvent`
- [x] 2.7 Remove `IOutbox` dependency from `TicketedEventCreationRequestedIntegrationEventHandler` (now handled by the publisher)

## 3. Consolidate TicketedEvents into TicketedEventManagement

- [x] 3.1 Move `TicketedEvents/ProjectEventStatus/` into `TicketedEventManagement/ProjectEventStatus/`
- [x] 3.2 Move `TicketedEvents/GetActiveReconfirmTriggerSpecs/` into `TicketedEventManagement/GetActiveReconfirmTriggerSpecs/`
- [x] 3.3 Move `TicketedEvents/GetReconfirmTriggerSpec/` into `TicketedEventManagement/GetReconfirmTriggerSpec/`
- [x] 3.4 Move `TicketedEvents/GetTicketedEventEmailContext/` into `TicketedEventManagement/GetTicketedEventEmailContext/`
- [x] 3.5 Update all namespaces in moved files from `TicketedEvents` to `TicketedEventManagement`
- [x] 3.6 Update all `using` directives referencing the old `TicketedEvents` namespace
- [x] 3.7 Delete the now-empty `TicketedEvents/` folder

## 4. Verify

- [x] 4.1 Run `dotnet test tests/Admitto.Core.ArchTests/` — no violations
- [x] 4.2 Run the full test suite — all tests pass
