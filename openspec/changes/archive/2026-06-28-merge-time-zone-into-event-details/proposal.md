## Why

Time zone is an event detail, but the current API, Admin UI, and integration-event model treat it as a separate mutation path. This creates unnecessary sequential saves in the UI, a dedicated endpoint and use case on the backend, and a distinct time-zone-changed event even though downstream consumers already handle general event details changes.

## What Changes

- Include `TimeZone` in the general ticketed-event details update contract.
- Update the Admin UI General settings form to save event details and time zone in a single request.
- Remove the dedicated time-zone update API route and backend use case. **BREAKING**: `PUT /admin/teams/{teamId}/events/{eventId}/time-zone` is removed.
- Remove the distinct `TicketedEventTimeZoneChanged` domain and integration events. **BREAKING**: integration consumers must use `TicketedEventDetailsChanged` for time-zone updates.
- Extend `TicketedEventDetailsChanged` domain and integration events to carry the updated time zone.
- Update Email's event context projection and reconfirm scheduling synchronization so a details-changed event updates the projected time zone and refreshes time-zone-dependent reconfirm triggers.
- Regenerate the Admin UI SDK after the OpenAPI contract changes and update proxy/UI callers to use the generated details-update client only.
- Update architecture and ADR documentation that currently names the separate time-zone-changed event.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `event-management`: Event time-zone updates move from a dedicated admin command/event to the general event details update command and details-changed event.
- `admin-ui-event-management`: The General tab submits time zone through the general event details update endpoint instead of a separate time-zone endpoint.
- `reconfirm-sending`: Reconfirm trigger replacement for time-zone changes is driven by `TicketedEventDetailsChanged` carrying `TimeZone`, not by `TicketedEventTimeZoneChanged`.

## Impact

- Affected backend module: Registrations, especially `TicketedEvent`, `UpdateTicketedEventDetails`, endpoint registration, and integration-event publishing.
- Affected consumer module: Email, especially `EventEmailContextProjector`, `EventEmailContextView`, and reconfirm trigger rescheduling from projected event context.
- Affected UI: Admin UI General settings form, BFF proxy routes, and generated OpenAPI SDK.
- Affected contracts: `UpdateTicketedEventDetailsHttpRequest`, `TicketedEventDetailsChangedDomainEvent`, and `TicketedEventDetailsChangedIntegrationEvent` gain `TimeZone`; `UpdateTicketedEventTimeZone*` and `TicketedEventTimeZoneChanged*` are removed.
- Affected docs: arc42 building-block/runtime/cross-cutting docs and ADR-009 references to the separate time-zone-changed integration event.
- No database schema migration is expected because `TicketedEvent.TimeZone` and Email's projected `TimeZone` already exist.
