## Why

Organizers can currently set a registration window or reconfirmation window with dates that extend beyond the event itself — e.g. a registration window that closes a week after the conference ends, or a reconfirm window that opens after the conference has already started. This produces nonsensical configurations that are silently accepted. The constraints need to be made explicit so the domain rejects them.

## What Changes

- The registration policy's `ClosesAt` must be on or before the event's `EndsAt` (registrations can overlap with the event, but cannot close after the event has ended).
- The reconfirmation policy's `ClosesAt` must be strictly before the event's `StartsAt` (reconfirmation must finish before the event begins).
- Both constraints are enforced in the domain aggregate (`TicketedEvent`) at policy-configuration time.
- Both constraints are also validated server-side in the corresponding FluentValidation command validators (fast-fail before hitting the domain).
- Corresponding UI validation is added to the admin policy forms to surface these errors client-side.
- New rejection scenarios are added to the `event-management` spec for each constraint.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `event-management`: Adding two new rejection rules — registration window must close on or before event end; reconfirm window must close before event start.

## Impact

- **Domain**: `TicketedEvent.ConfigureRegistrationPolicy` and `ConfigureReconfirmPolicy` gain cross-aggregate date guards.
- **Application**: `SetRegistrationPolicyCommandValidator` and `SetReconfirmPolicyCommandValidator` gain new rules.
- **UI**: Registration policy form and reconfirm policy form gain client-side validation against event dates (already available via the loaded event query).
- **Tests**: New domain unit tests and API integration test scenarios for the two rejection cases.
