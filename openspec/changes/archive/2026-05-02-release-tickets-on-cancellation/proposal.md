## Why

When a registration is cancelled, the tickets held by that registration are never released back to the `TicketCatalog`. This means the `UsedCapacity` on ticket types is permanently inflated after a cancellation, eventually preventing new valid registrations even when capacity is technically available.

## What Changes

- Add a `Release(slugs)` method to `TicketCatalog` (and a corresponding `ReleaseCapacity()` method on `TicketType`) that decrements `UsedCapacity` for each released ticket type slug.
- Add a `ReleaseTickets` use case inside `Admitto.Module.Registrations` with a `ReleaseTicketsCommand` and `ReleaseTicketsHandler`. The handler loads the `Registration` to retrieve its ticket slugs, loads the `TicketCatalog`, and calls `Release`.
- Add a domain event handler `RegistrationCancelledDomainEventHandler` (under `Registrations/ReleaseTickets/EventHandlers/`) that reacts to `RegistrationCancelledDomainEvent` by dispatching `ReleaseTicketsCommand` via the mediator.

## Capabilities

### New Capabilities

- `release-tickets`: Automatic ticket capacity release whenever a registration is cancelled. Covers the domain method, the command/handler, and the event-handler wiring.

### Modified Capabilities

<!-- No existing spec-level behavior changes. The admin-cancel-registration spec is unaffected;
     ticket release is an internal side-effect, not an API behavior change. -->

## Impact

- **Domain**: `TicketCatalog` and `TicketType` entities gain a `Release` method.
- **Application**: New use case folder `Registrations/ReleaseTickets/` with `ReleaseTicketsCommand`, `ReleaseTicketsHandler`, and `EventHandlers/RegistrationCancelledDomainEventHandler`.
- **Shared infrastructure**: `ProcessedMessage` entity, `IInboxDbContext` interface, and EF entity configuration added to `Admitto.Module.Shared`, following the same pattern as the legacy `Admitto.Infrastructure` `MessageLog`. No module opts in yet; it is introduced as ready-to-use infrastructure for future integration/module event handlers that need exactly-once processing.
- **No API changes**: ticket release is fully internal and event-driven.
- **No cross-module changes**: operates entirely within `Admitto.Module.Registrations`.
