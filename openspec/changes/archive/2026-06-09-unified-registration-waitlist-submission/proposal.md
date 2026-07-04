## Why

Partner event websites currently need to orchestrate separate calls for attendee registration and waitlist joins. That breaks down when a verified attendee wants available tickets now while also expressing interest in sold-out waitlistable tickets, and it exposes a bug where later waitlist coupon redemption cannot update an attendee's existing registration.

## What Changes

- Extend public registration creation so a single explicit-intent submission can request tickets to register now and ticket waitlists to join, persisted atomically.
- Keep the submission deterministic: the request separately names `registerTicketTypeIds` and `waitlistTicketTypeIds`; if current ticket state no longer matches the requested action, the system rejects the whole request instead of reclassifying tickets.
- Allow waitlist entries to overlap with registered tickets and with other waitlist entries; overlap is enforced only for actual persisted registration ticket sets.
- Extend self-service ticket change so a waitlist coupon can be used as a capacity grant for the offered ticket type when changing an existing registration.
- Fix the waitlist coupon path so an attendee who already has an active registration can claim a waitlist offer by changing tickets rather than being rejected as already registered.
- Keep all changed write flows under the existing Registrations module transaction boundary: endpoint owns `SaveChangesAsync`; handlers mutate aggregates only.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `attendee-registration`: Public registration creation accepts explicit register/waitlist ticket sets, returns a mixed outcome, and rejects stale ticket-state expectations atomically.
- `waitlist`: Waitlist entries are independent per ticket type, may overlap with current or other waitlist intents, and waitlist coupon claims can be applied to existing registrations through ticket-change semantics.
- `self-service-change-tickets`: Self-service ticket change accepts a waitlist coupon grant that bypasses capacity and waitlist-mode only for the offered ticket type while validating the final registration ticket set normally.

## Impact

- Public API contract for `POST /api/teams/{teamId}/events/{eventId}/registrations` changes from a single ticket set to explicit registration and waitlist ticket sets.
- Public API contract for self-service ticket change is extended with optional waitlist coupon information.
- Registrations application handlers need to classify and validate explicit requested actions without best-effort reclassification.
- Waitlist coupon redemption needs to support both first registration creation and existing registration ticket changes.
- Integration/API tests need coverage for mixed registration + waitlist submissions, stale ticket-state rejection, overlap allowance for waitlist intents, and waitlist coupon claims against existing registrations.
