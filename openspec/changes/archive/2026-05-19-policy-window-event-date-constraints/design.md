## Context

The `TicketedEvent` aggregate in the Registrations module owns two policy value objects: `TicketedEventRegistrationPolicy` (window + optional email-domain restriction) and `TicketedEventReconfirmPolicy` (window + cadence + min-email-interval). Both policies carry an `OpensAt`/`ClosesAt` window.

Currently, the only window constraint is `ClosesAt > OpensAt`. The aggregate already holds `StartsAt` and `EndsAt` but does not cross-validate policy windows against them. This allows nonsensical configurations such as a registration window closing after the event has ended, or a reconfirmation window closing after the event has already started.

## Goals / Non-Goals

**Goals:**
- Enforce that `RegistrationPolicy.ClosesAt ≤ event.EndsAt` (registrations may overlap with the event but cannot close after it ends).
- Enforce that `ReconfirmPolicy.ClosesAt < event.StartsAt` (reconfirmation must complete before the event begins).
- Surface both violations as domain errors consistent with the existing error taxonomy.
- Add client-side validation in the admin UI forms (event start/end dates are already available in the loaded event query).

**Non-Goals:**
- Validating that `OpensAt` is in the future — the domain has no concept of "now" at configuration time.
- Validating that the registration window opens before the event ends — an intentionally permissive choice (e.g. accepting late registrations on the day).
- Changes to the attendee-facing self-service registration paths.

## Decisions

### Guard lives in the domain aggregate, not in the command validator

The two new constraints compare a policy window against the aggregate's own `StartsAt`/`EndsAt`. The natural home is `TicketedEvent.ConfigureRegistrationPolicy` and `ConfigureReconfirmPolicy`, which already hold both sides of the comparison.

FluentValidation command validators run as endpoint filters **before** the handler loads the aggregate. Adding the check there would require an extra repository read just for validation, duplicating work the handler will do anyway. Keeping the rule in the domain avoids this, and the domain is the authoritative source of truth regardless.

The FluentValidation validators retain their existing structural checks (`ClosesAt > OpensAt`, etc.). The new cross-field guards are domain-only.

### Strict inequality for reconfirm (`< StartsAt`), non-strict for registration (`≤ EndsAt`)

The reconfirmation window must finish *before* the event starts — an open window on the day of the event is meaningless noise. Strict inequality (`<`) is therefore appropriate.

The registration window closing *exactly at* event end is an edge case that should be allowed (e.g. closing registration at the moment the event starts), so non-strict (`≤`) is used.

## Risks / Trade-offs

**Existing data may violate the new constraints**
Organizers who have already saved a registration or reconfirm window with post-event dates will not be affected at read time, but will get a validation error the next time they attempt to update the policy. This is acceptable — the domain should refuse to create *new* invalid state.

**Tightening a guard could surprise organizers**
An organizer editing an existing valid policy (e.g. changing the email domain) would not encounter this error unless they also change the window dates. The validator only fires on the full replace (PUT semantics), so partial-field edits are not a concern.
