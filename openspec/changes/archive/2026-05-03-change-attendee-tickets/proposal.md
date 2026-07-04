## Why

Admins currently have no way to change the ticket types on an existing registration — the only options are cancel and re-register, which loses history and forces the attendee through the full flow again. A first-class "change tickets" operation keeps the registration intact while updating the ticket selection, releasing freed capacity, and re-sending a confirmation email.

## What Changes

- Remove the "Change ticket types" button at the top of the attendee detail page; keep the "Change" button inside the Tickets card as the single entry point.
- Add a new `ChangeAttendeeTickets` admin command + handler that:
  - Validates the new ticket selection against the same ticket-type rules (no duplicates, no unknown/cancelled types, no overlapping time slots).
  - Releases capacity for tickets being removed; claims capacity only for tickets being added (net delta, not gross reclaim — so a fully sold-out event can still change tickets already held by the attendee).
  - For the admin path, new-ticket capacity claims are unenforced (matching the pattern for admin-add).
  - Raises a new `TicketsChangedDomainEvent` → integration event consumed by the Email module.
- Extend the `ticket` email template (default HTML and text) with a `{{ ticket_types }}` variable listing the registered ticket types. The variable is also passed by the existing `AttendeeRegisteredIntegrationEventHandler`, so all future registration confirmation emails include ticket types.
- Add a `TicketsChanged` activity type to the `activity-log` capability so the change appears in the registration timeline.
- Wire a new admin HTTP endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/tickets`.

## Capabilities

### New Capabilities
- `change-attendee-tickets`: Admin can change the ticket-type selection on an existing registration, with capacity delta management, activity log entry, and confirmation email.

### Modified Capabilities
- `activity-log`: Add `TicketsChanged` as a supported `activity_type`, projected from `TicketsChangedDomainEvent`.
- `email-templates`: Extend the built-in `ticket` template with a `{{ ticket_types }}` variable (list of registered ticket type names). The `ticket_types` parameter is added to `AttendeeRegisteredIntegrationEvent` (and the new `AttendeeTicketsChangedIntegrationEvent`) so that all confirmation emails show what the attendee is registered for.

## Impact

- `Admitto.Module.Registrations` domain: new `TicketsChangedDomainEvent`, new `Registration.ChangeTickets(...)` method, new `ChangeAttendeeTicketsCommand/Handler`, new admin HTTP endpoint.
- `Admitto.Module.Registrations.Contracts`: new `AttendeeTicketsChangedIntegrationEvent`.
- `Admitto.Module.Email`: new `AttendeeTicketsChangedIntegrationEventHandler`; the `AttendeeRegisteredIntegrationEventHandler` is updated to pass `ticket_types`; both built-in default templates (HTML + text) are updated.
- `Admitto.UI.Admin`: remove top "Change ticket types" button; the existing "Change" button in the Tickets card drives the new endpoint.
- No schema migrations needed; the new `TicketsChanged` activity type is stored as a string in the existing `activity_log.activity_type` column.
