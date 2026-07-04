## Context

The Registrations module contains three structural issues identified during a code review:

1. **Duplicate registration-listing logic**: `GetRegistrations` (admin HTTP endpoint) and `QueryRegistrations` (cross-module facade) implement nearly identical EF Core queries with slightly different DTOs, no shared filtering, and different null-safety strategies.
2. **Materialize bypass**: `TicketedEventCreationRequestedIntegrationEventHandler` directly creates the `TicketedEvent` aggregate, saves it, and enqueues the `TicketedEventCreatedIntegrationEvent` all in a single handler — bypassing the established command/domain-event/integration-event pipeline that every other aggregate mutation follows.
3. **Arbitrary folder split**: Use cases dealing with `TicketedEvent` are split between `TicketedEventManagement/` (admin commands + some queries) and `TicketedEvents/` (internal queries + event handlers) with no clear rule distinguishing them.

These are pure internal refactors. Public API contracts, the OpenAPI spec, cross-module interfaces, and database schema are unchanged.

## Goals / Non-Goals

**Goals:**
- Consolidate registration-listing into one handler that serves both the admin HTTP endpoint and the cross-module facade.
- Make `MaterializeTicketedEvent` follow the same command → domain event → integration event chain used everywhere else in the module.
- Merge `TicketedEvents/` into `TicketedEventManagement/`.

**Non-Goals:**
- Changing any public API response shape or HTTP contract.
- Changing `IRegistrationsFacade` or any Contracts type.
- Adding new filtering capabilities to the admin endpoint.
- Changing the `TicketedEvent` aggregate state machine or persistence model.

## Decisions

### Decision 1: Single `GetRegistrations` handler, two call sites

The merged handler will accept an `EventId`, an optional `TeamId` (for ownership guard), and an optional `QueryRegistrationsDto` filter. The admin HTTP endpoint passes `TeamId` (returns `null` / `404` when the event is not found on the team) and no filter. The facade implementation passes no `TeamId` (event existence is assumed by the caller) and a filter.

The internal admin DTO (`RegistrationListItemDto` in `GetRegistrations/`) currently includes resolved ticket-type *names* (looked up from `TicketCatalog`) whereas the Contracts DTO exposes only slugs. The two call sites will continue to project to their respective DTOs; the shared handler will return the raw `Registration` entities and each caller performs its own projection — or the handler is split into a shared query-builder layer and two thin projection handlers. The simpler approach (shared EF query builder, two projections) is preferred to avoid forcing the admin response shape to conform to the Contracts DTO.

**Alternative considered**: Unify the DTOs by enriching the Contracts DTO with ticket names. Rejected because it would widen the cross-module contract and require the Email module (and any future consumer) to handle data it does not need.

### Decision 2: `MaterializeTicketedEvent` follows the command → domain event → integration event chain

**New flow:**
1. `TicketedEventCreationRequestedIntegrationEventHandler` dispatches a `MaterializeTicketedEventCommand` (carrying all fields from the integration event including `CreationRequestId`).
2. `MaterializeTicketedEventHandler` (command handler) creates the `TicketedEvent` aggregate. `TicketedEvent.Create(...)` raises a `TicketedEventCreatedDomainEvent` carrying `CreationRequestId`, `TeamId`, `TicketedEventId`, and `TimeZone`. The handler also creates and persists `TicketCatalog`.
3. `RegistrationsIntegrationEventPublisher` gains `IDomainEventHandler<TicketedEventCreatedDomainEvent>` and enqueues `TicketedEventCreatedIntegrationEvent`.

The `CreationRequestId` must be threaded from integration event → command → domain event → integration event publisher. This is the only state not naturally on the aggregate, so it is carried in the domain event (not persisted on `TicketedEvent`).

**Alternative considered**: Store `CreationRequestId` as a field on `TicketedEvent`. Rejected: it is only needed once, during the creation acknowledgement handshake, and does not represent persistent domain state.

### Decision 3: Merge into `TicketedEventManagement/`

All existing contents of `TicketedEvents/` move into `TicketedEventManagement/`. Namespaces and `using` directives are updated accordingly. No logical changes.

## Risks / Trade-offs

- [Risk] Namespace changes cascade into test projects → Mitigation: Update namespace references as part of the same change; run ArchTests and unit/integration tests to confirm.
- [Risk] `CreationRequestId` threading via domain event is slightly unorthodox (domain events normally carry only aggregate state) → Mitigation: The `CreationRequestId` is part of the creation context and is documented in the domain event. This is an acceptable pragmatic choice given it avoids polluting the aggregate.

## Open Questions

- None — all decisions above are resolved.
