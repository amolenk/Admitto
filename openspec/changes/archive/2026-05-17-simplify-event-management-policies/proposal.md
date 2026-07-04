## Why

The current event management model carries policy configuration that is either unused in practice (late-cancellation classification that no business logic acts on), insufficiently specified (reconfirmation email throttling per attendee), or too complex for the actual workflow (a "Cancelled" event state that is never communicated directly — organizers prefer a personal touch via bulk email before archiving). Removing and simplifying these reduces the surface area while covering the real operational needs.

## What Changes

- **BREAKING** Remove `TicketedEventCancellationPolicy` (late-cancellation cutoff) entirely from the domain, API, and Admin UI; replace with a single hard-coded rule: attendee-initiated cancellation is rejected once the event has started (i.e. `now >= event.StartsAt`).
- **BREAKING** Remove the ability for organizers to cancel a `TicketedEvent`; the `Cancelled` lifecycle status is eliminated. Events transition directly from `Active` to `Archived`. All references to the `Cancelled` status (API responses, UI banners, ticket-catalog projection) are removed.
- **BREAKING** Remove the ability for organizers to cancel a ticket type. Ticket types remain `Active` until the event is archived.
- Add a new `MinEmailInterval` setting (working name: **minimum email interval**) to `TicketedEventReconfirmPolicy`. This is a duration (in hours) expressing the minimum time that must elapse since the *last reconfirmation email sent to a specific attendee* before the system will send them another one. This prevents both the "just-registered attendee gets an immediate prompt" scenario and the "daily cadence is too aggressive for a given attendee" scenario.
- Update `reconfirm-sending` eligibility to honour `MinEmailInterval`: the scheduler SHALL consult the email log to skip any attendee who received a `reconfirm` email within the last `MinEmailInterval` hours.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `event-management`: Remove `TicketedEventCancellationPolicy` and all related requirements/scenarios; remove "Organizer can cancel an event" requirement; add `MinEmailInterval` field to `TicketedEventReconfirmPolicy`; hard-code the post-start cancellation guard on the registration side.
- `admin-ui-event-policies`: Remove the "Cancellation Policy" page; add `MinEmailInterval` input to the "Reconfirmation Policy" form.
- `admin-ui-event-management`: Remove the "Cancel event" action from the event management UI; remove read-only / cancelled-event banner logic.
- `ticket-type-management`: Remove the "Organizer can cancel a ticket type" requirement and related scenarios; remove `Cancelled` from ticket-catalog `EventStatus` transitions (only `Active → Archived` remains); update list-ticket-types response (no `cancellationStatus` field).
- `self-service-cancel-registration`: Replace any policy-based late-cancellation guard with the hard-coded rule (reject if `now >= event.StartsAt`).
- `reconfirm-sending`: Update eligibility logic to filter out attendees whose last `reconfirm` email was sent within `MinEmailInterval` hours; update trigger lifecycle (no `TicketedEventCancelled` event to react to since Cancelled status is gone — only `TicketedEventArchived`).

## Impact

- **Domain**: `TicketedEvent` aggregate — remove `CancellationPolicy`, remove `Cancel()` method, remove `ReconfirmPolicy.Cadence`-only constructor (add `MinEmailInterval`); `TicketCatalog` aggregate — remove `CancelTicketType()`, simplify `EventStatus` to `Active | Archived`.
- **API**: Remove `PUT /admin/…/cancellation-policy`, remove `DELETE /admin/…/cancellation-policy`, remove `POST /admin/…/cancel` (event cancel endpoint), remove `POST /admin/…/ticket-types/{id}/cancel`; update reconfirm policy endpoint to accept `MinEmailInterval`.
- **Admin UI**: Remove cancellation policy page route; update reconfirmation policy form; remove cancel-event button.
- **Email module**: `reconfirm-sending` job must query the email log for per-attendee last-send timestamps.
- **Integration events**: `TicketedEventCancelled` integration event is no longer published (remove handlers in Email and other modules); `TicketedEventReconfirmPolicyChanged` gains `MinEmailInterval` in its payload.
- **Tests**: All tests covering cancellation policy, cancel-event, cancel-ticket-type must be removed or updated; new tests for `MinEmailInterval` throttling logic.
