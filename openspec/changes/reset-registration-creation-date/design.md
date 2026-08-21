## Context

The admin-add, self-service, and coupon registration flows reuse a cancelled `Registration` aggregate for the same event and attendee email.
The reset retains the registration ID while restoring the aggregate to the same effective state as a newly created registration.
`CreatedAt` is audit metadata populated by the persistence audit interceptor, so the domain reset method must explicitly signal that the aggregate is newly created again without changing the semantics of unrelated audit fields.

## Goals / Non-Goals

**Goals:**
- Make a reset cancelled registration report the timestamp at which it was reset.
- Preserve the existing registration ID and all current reset, capacity-claim, and side-effect behavior across every registration channel.
- Verify the aggregate invariant without depending on a live database clock.

**Non-Goals:**
- Do not alter the public registration, cancellation, reconfirmation, or ticket-change flows.
- Do not add an API field, database migration, or change any cross-module event contract.
- Do not refresh `LastChangedAt` independently of the established audit interceptor.

## Decisions

### Reset `CreatedAt` inside the aggregate reset operation

The existing reset method will assign `CreatedAt` to the supplied current time as it restores the cancelled aggregate to `Registered`.
The aggregate owns the lifecycle transition, which keeps the invariant true for every caller and makes it directly unit-testable.

Each existing handler already passes its clock-derived registration time to the aggregate reset operation.
The aggregate will use that supplied value for both the reset timestamp and the emitted attendee-registered event, preserving deterministic domain tests and avoiding handler-specific behavior.

Alternative: set `CreatedAt` in the handler or persistence interceptor.
This was rejected because either option would leave the aggregate reset operation capable of producing an incomplete reset and would couple reset semantics to a particular persistence path.

### Retain the registration identity and all other reset semantics

Only `CreatedAt` changes in addition to the already-defined reset fields.
Keeping the ID maintains existing references, attendee-held links, and side-effect idempotency behavior while making creation time represent the current registration lifecycle.

Alternative: create a new aggregate row.
This was rejected because the established admin-registration contract explicitly preserves the registration ID when resetting a cancelled record.

## Risks / Trade-offs

- [Clock precision or test flakiness] → Inject or pass an explicit reset timestamp in tests and assert the exact value.
- [Unintended audit behavior] → Limit the change to `CreatedAt` in the reset path and retain the existing audit interceptor ownership for other audit fields.
- [Incomplete behavior coverage] → Add a regression case to the aggregate domain tests and cover the shared reset contract in both registration capability specifications.
