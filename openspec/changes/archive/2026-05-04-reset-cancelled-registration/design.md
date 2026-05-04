## Context

`Admitto.Module.Registrations` owns `Registration` and already stores one row per event/email through the unique `(event_id, email)` index. Cancellation transitions the aggregate to `Cancelled` and raises `RegistrationCancelledDomainEvent`; the release-tickets handler then releases held capacity. A later sign-up with the same event/email currently attempts to insert a second row and fails on the unique index.

The current registration implementation uses a single `RegisterAttendeeCommand` and `RegisterAttendeeHandler` with `RegistrationMode` values for self-service, coupon, and admin-add. The handler performs mode-specific gates, claims ticket capacity through `TicketCatalog`, creates a `Registration`, and relies on endpoint-owned unit-of-work commits.

## Goals / Non-Goals

**Goals:**
- Allow self-service, coupon, and admin-add registration requests to reuse a matching `Cancelled` registration for the same event/email.
- Preserve the existing `RegistrationId` when resetting so existing links, activity history, and external references remain stable.
- Re-run all existing path-specific gates before reset: email verification, coupon validation, registration window/domain checks, active event status, ticket selection validation, capacity claim behavior, and additional-detail validation.
- Reset the aggregate's attendee data, ticket snapshot, additional details, cancellation state, and reconfirmation state.
- Emit the same attendee-registered domain/integration behavior as a newly created registration.
- Keep endpoint-owned transaction boundaries unchanged.

**Non-Goals:**
- Allow duplicate active registrations for the same event/email.
- Delete and recreate registrations.
- Change the public or admin HTTP contract shape.
- Change legacy CLI code. Any existing CLI behavior will receive the backend semantics through the existing endpoint/client surface.
- Backfill or repair historical capacity if older cancellations did not release tickets.

## Decisions

### D1. Reset the existing aggregate instead of creating a second registration

The `Registration` aggregate will gain a reset/reactivation method, for example `Reset(...)`, that is valid only when `Status == Cancelled`.

The method updates:
- `Status = Registered`
- `CancellationReason = null`
- `HasReconfirmed = false`
- `ReconfirmedAt = null`
- `FirstName`, `LastName`, `Tickets`, and `AdditionalDetails` from the new request

It preserves `Id`, `TeamId`, `EventId`, and `Email`, then raises `AttendeeRegisteredDomainEvent` with the current attendee/ticket data.

Alternative considered: delete the cancelled row and insert a fresh registration. Rejected because it loses stable registration identity, makes activity/audit history harder to follow, and fights the existing unique event/email invariant.

### D2. Keep the unique event/email index

The existing unique index remains the durable invariant for one registration identity per event/email. The handler should explicitly load an existing registration for the command's `EventId` and `Email` before adding a new aggregate:
- If no existing row is found, create a new `Registration`.
- If the row is `Registered`, reject with the existing already-registered conflict semantics.
- If the row is `Cancelled`, reset that tracked aggregate.

The aggregate `Version` concurrency token remains the race safety net. Concurrent reset attempts for the same cancelled row will conflict at commit; concurrent inserts are still protected by the unique index.

### D3. Run duplicate/reset resolution after path-specific access gates

The handler must keep the self-service verification ordering from the current spec: missing or invalid verification tokens fail before event, catalog, coupon, or registration lookups. Coupon status and target-email checks should also remain before any reset. After those checks, the handler loads the event/catalog, validates the request, claims capacity, validates additional details, and only then creates or resets the registration.

This means a cancelled registration is not reactivated if a new request fails any existing gate. It also means reset consumes capacity exactly like a new registration; cancellation previously released capacity, so reactivation must claim it again.

### D4. Reuse the existing attendee-registered event

Reset is product-equivalent to a new successful registration for downstream consumers. The domain method should raise `AttendeeRegisteredDomainEvent`, which preserves current confirmation-email and activity-log behavior through `RegistrationsMessagePolicy` and `WriteActivityLog`.

Alternative considered: add a separate `RegistrationResetDomainEvent`. Rejected because no downstream consumer currently needs different behavior, and a new event would add branching without a clear business distinction.

## Risks / Trade-offs

- **Capacity claim succeeds but later reset conflicts** -> The unit of work rolls back both the catalog claim and registration update. Existing optimistic concurrency and transactional commit semantics cover this.
- **Coupon reset uses another single-use coupon** -> The new coupon is redeemed only after coupon gates pass and the registration is reset. Previously redeemed coupons remain historical facts; this mirrors normal coupon behavior.
- **Activity history contains Cancelled then Registered for the same registration id** -> This is intentional and gives a clear lifecycle trail. Existing detail/list surfaces already include registration status and activity log data.
- **Postgres mapping still handles rare unique violations** -> Explicit duplicate detection improves business errors, but the unique index remains the final guard for races.

## Migration Plan

No database schema migration is required. Deploy the backend change with tests. Rollback is code-only: reverted code will again reject re-registration for cancelled rows while preserving data already reset to `Registered`.

## Open Questions

None.
