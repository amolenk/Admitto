## Why

Waitlist quiet hours are currently stored and edited as general event settings even though they only affect waitlist offer claim windows. Moving them into an explicit waitlist policy makes the domain model and Admin UI match the behavior organizers are configuring.

## What Changes

- Introduce a `WaitlistPolicy` value object on `TicketedEvent` containing `QuietHoursStart` and `QuietHoursEnd`.
- Keep quiet hours event-wide: the policy applies to all ticket-type waitlists for the event, while each ticket type continues to own `ClaimWindowHours`.
- Move quiet-hours editing from the Admin UI General settings form to the event Policies tab in a dedicated Waitlist policy section.
- Expose and update waitlist policy through policy-oriented event contracts/endpoints instead of the general event details update payload.
- Preserve the existing behavior: waitlist emails are still sent immediately, and quiet hours extend coupon expiry rather than delaying notification.
- Preserve persisted values and defaults (`22:00` to `08:00`) during the refactor.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `waitlist`: Model event-level quiet hours as a waitlist policy while preserving claim-window behavior.
- `admin-ui-event-policies`: Add waitlist policy management to the Policies tab and remove quiet-hours controls from General event settings.

## Impact

- Registrations domain: `TicketedEvent`, waitlist coupon expiry calculation call sites, EF configuration, and migrations if the persisted shape changes.
- Registrations API: event details DTO, general event update contract, and a new or updated waitlist-policy command/endpoint.
- Admin UI: event settings General form, Policies page, generated API types/SDK if backend contracts change.
- Tests: domain tests for waitlist policy/defaults, handler/API contract tests for policy updates, waitlist notification expiry tests, and UI/form tests where present.
- Documentation/specs: waitlist and Admin UI event policies requirements; arc42 building-block/runtime notes if the policy model or endpoint flow changes materially.
