## Why

The Registrations module has accumulated several structural inconsistencies that make the codebase harder to navigate and violate established patterns: two overlapping registration-listing use cases, a materialize flow that bypasses the domain-event/integration-event separation, and an arbitrary two-folder split for all things `TicketedEvent`. Cleaning these up now keeps the module coherent before new functionality is added.

## What Changes

- **Merge `GetRegistrations` and `QueryRegistrations`** into a single `GetRegistrations` use case. The admin HTTP endpoint and the cross-module facade query (`IRegistrationsFacade.QueryRegistrationsAsync`) will both be served by one handler that supports optional filtering and an optional team-ownership guard.
- **Fix `MaterializeTicketedEvent` flow**:
  - The `TicketedEventCreationRequestedIntegrationEventHandler` will dispatch a `MaterializeTicketedEventCommand` instead of directly creating the aggregate and publishing an integration event.
  - The command handler will create the `TicketedEvent` aggregate (and its `TicketCatalog`), after which the aggregate raises a `TicketedEventCreatedDomainEvent`.
  - `RegistrationsIntegrationEventPublisher` gains a handler for `TicketedEventCreatedDomainEvent` that enqueues `TicketedEventCreatedIntegrationEvent` (and the rejection event when creation fails).
- **Consolidate `TicketedEventManagement` and `TicketedEvents` folders** into a single `TicketedEventManagement` folder. The internal query and event-handler use cases currently split across both folders will all live under `TicketedEventManagement`.

## Capabilities

### New Capabilities
<!-- None — all changes are internal refactors -->

### Modified Capabilities
<!-- Behaviour and API contracts are unchanged; only internal implementation structure changes.
     No spec-level requirement changes in any capability. -->

## Impact

- `Admitto.Core` — Registrations module use-case folder structure, handler implementations, `RegistrationsIntegrationEventPublisher`, `TicketedEvent` aggregate (new domain event), `RegistrationsModule` wiring.
- No changes to public API contracts, OpenAPI spec, or cross-module interfaces (`IRegistrationsFacade`).
- No database schema changes.
- All existing tests should pass without modification to their assertions; some namespace/type references in tests may need updating.
