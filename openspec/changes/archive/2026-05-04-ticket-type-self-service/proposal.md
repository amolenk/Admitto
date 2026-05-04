## Purpose

Add an explicit `SelfServiceEnabled` flag to ticket types so that external websites can know which ticket types are available for self-service registration. Also expose a new public API endpoint for listing self-service-available ticket types, enforce the flag in all self-service flows, and fix the capacity UX in the Admin UI so organizers can toggle capacity on/off cleanly.

## Problem

The current design implicitly gates self-service availability by whether a ticket type has a capacity set: `MaxCapacity == null` is treated as "not available for self-service" inside `ClaimWithEnforcement()`. This conflates two separate concerns — capacity limits and self-service availability — and makes it impossible to have an unlimited self-service ticket type. External websites have no way to discover which ticket types are available for self-service without attempting a registration. There is also no public endpoint for listing ticket types.

Additionally, the Admin UI has a bug where, once a capacity is set on a ticket type, the organizer cannot remove it (clearing the input does not clear the backend value reliably), making the UX confusing.

## Approach

1. Add `SelfServiceEnabled: bool` to the `TicketType` entity (default `true` for all new and existing ticket types — greenfield deployment, no migration complexity needed).
2. Update `ClaimWithEnforcement()` to check the flag instead of `MaxCapacity is null`.
3. Add `selfServiceEnabled` to the `AddTicketType` and `UpdateTicketType` commands.
4. Expose a new public endpoint `GET /events/{teamSlug}/{eventSlug}/ticket-types` returning only active, self-service-enabled ticket types (no auth required).
5. Update `TicketTypeDto` (admin) to include `selfServiceEnabled`.
6. Update self-service validation in both the `RegisterAttendee` and `ChangeAttendeeTickets` handlers to explicitly reject ticket types that are not self-service enabled.
7. Fix the Admin UI capacity field with a toggle pattern: a "Limit capacity" checkbox reveals a numeric input; unchecking it clears the limit. Add a "Enable self-service" checkbox to the add/edit forms.

## Scope

**In scope:**
- `SelfServiceEnabled` flag on `TicketType` (domain, EF, API, UI)
- New public `GET /events/{teamSlug}/{eventSlug}/ticket-types` endpoint
- Self-service validation enforcement in `RegisterAttendee` and `ChangeAttendeeTickets`
- Admin UI: self-service toggle + capacity toggle (Option C) on add/edit forms
- Updated `TicketTypeDto` (admin) includes `selfServiceEnabled`
- Coupon-based registrations bypass the self-service flag (unchanged behavior)
- Admin registrations bypass the self-service flag (unchanged behavior)

**Out of scope:**
- Time-slot management UI
- Price fields
- Changes to coupon or admin registration flows

## Affected Specs

- `ticket-type-management` — `SelfServiceEnabled` field, public listing requirement
- `attendee-registration` — self-service-disabled rejection scenario
- `self-service-change-tickets` — self-service-disabled rejection scenario
- `admin-ui-event-management` — Registration tab UI changes (toggles)
