## Why

TicketType is currently a value object on the `TicketedEvent` aggregate in the Organization module, but its primary consumers are in the Registrations module (capacity tracking, registration validation, coupon validation). This creates two coupling paths: async module events to sync capacity data, and synchronous facade calls back to Organization during registration. Moving TicketType ownership to Registrations eliminates cross-module data sync, removes runtime coupling during registration flows, and makes capacity a native concern rather than a mirrored copy.

## What Changes

- **BREAKING**: TicketType CRUD (add, update, cancel, get) moves from Organization module to Registrations module.
- **BREAKING**: `IOrganizationFacade` loses `GetTicketTypesAsync` and `IsEventActiveAsync` methods. `TicketTypeDto` removed from Organization.Contracts.
- New `EventTicketConfiguration` aggregate in Registrations replaces both the Organization `TicketType` value object and the Registrations `EventCapacity` aggregate, unifying ticket definition with capacity tracking.
- Organization publishes new lifecycle module events (`TicketedEventCancelled`, `TicketedEventArchived`) so Registrations can react to event status changes.
- `TicketTypeAddedModuleEvent` and `TicketTypeCapacityChangedModuleEvent` are removed — no more capacity sync.
- Registration handlers (`SelfRegisterAttendee`, `RegisterWithCoupon`, `CreateCoupon`) query ticket types locally instead of calling the Organization facade.
- Admin API endpoints for ticket types keep the same URL pattern (`/admin/teams/{teamSlug}/events/{eventSlug}/ticket-types/...`) but are served by the Registrations module.
- CLI ticket type commands route to the Registrations module.

## Capabilities

### New Capabilities
- `ticket-type-management`: CRUD operations for ticket types (add, update, cancel, list) as a Registrations-module concern, including the unified `EventTicketConfiguration` aggregate that combines ticket definition with capacity tracking.
- `event-lifecycle-sync`: Organization publishes event lifecycle signals (cancelled, archived) that Registrations consumes to deactivate ticket types and block registrations.

### Modified Capabilities
- `event-management`: TicketType-related requirements move out. The `TicketedEvent` aggregate no longer owns ticket types. Capacity-change module events are removed. Event cancel/archive now publishes lifecycle module events.
- `attendee-registration`: Registration handlers no longer call `IOrganizationFacade` for ticket type data or event-active checks. Validation and capacity enforcement use local `EventTicketConfiguration` data. `EventLifecycleStatus` replaces the facade-based active check.
- `coupon-management`: `CreateCoupon` handler loads ticket type info locally instead of via Organization facade.
- `registration-policy`: No requirement changes, but the active-event check shifts from Organization facade to local lifecycle status.

## Impact

- **Organization module**: `TicketedEvent` aggregate shrinks (loses `_ticketTypes` collection, `AddTicketType`/`UpdateTicketType`/`CancelTicketType` methods, cancel-cascade logic). Domain events, module events, message policy, facade, EF configuration, endpoints, and CLI commands for ticket types are removed. New domain events and module events for cancel/archive are added.
- **Organization.Contracts**: `TicketTypeAddedModuleEvent`, `TicketTypeCapacityChangedModuleEvent`, `TicketTypeDto` removed. `TicketedEventCancelledModuleEvent`, `TicketedEventArchivedModuleEvent` added. `IOrganizationFacade` loses two methods.
- **Registrations module**: New `EventTicketConfiguration` aggregate with `TicketType` child entities. New CRUD use cases and admin endpoints. Registration handlers simplified (all-local queries). `EventCapacity`/`TicketCapacity` entities replaced. Capacity sync handlers (`InitializeTicketCapacity`, `UpdateTicketCapacity`) removed. New lifecycle event handlers added.
- **Registrations.Contracts**: May gain a `TicketTypeDto` if needed for cross-module exposure (unlikely for now).
- **API routing**: Ticket type endpoints move from `OrganizationApiEndpoints` to `RegistrationsModule.MapRegistrationsAdminEndpoints()`. URL patterns unchanged.
- **CLI**: Ticket type commands update to target Registrations API.
- **Database**: Organization schema loses `ticket_types` JSON column on `ticketed_events` table. Registrations schema gains new ticket configuration table (replaces `event_capacity`). Greenfield deployment — no data migration needed.
- **Architecture docs**: arc42 building block view, crosscutting concepts, and ADRs need updates. OpenSpec specs for `event-management` and `attendee-registration` need delta specs.
- **Tests**: Organization ticket type domain tests and integration tests move to Registrations. Registration integration tests simplify (no facade mocking for ticket types). New tests for lifecycle event handling.
