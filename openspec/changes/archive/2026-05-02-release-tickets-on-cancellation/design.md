## Context

When an attendee registers, `RegisterAttendeeHandler` calls `TicketCatalog.Claim(slugs, enforce)` to increment `UsedCapacity` on each selected `TicketType`. There is no symmetric release path. When a registration is cancelled (via `CancelRegistrationHandler`), the `RegistrationCancelledDomainEvent` is raised, but nothing decrements `UsedCapacity`. Over time this permanently inflates capacity counts, blocking legitimate future registrations even when real spots are available.

The existing `WriteActivityLog` use case demonstrates the established pattern for reacting to `RegistrationCancelledDomainEvent`: a `IDomainEventHandler<RegistrationCancelledDomainEvent>` dispatches a command via `IMediator`.

## Goals / Non-Goals

**Goals:**
- Add a `Release(slugs)` method to `TicketCatalog` (and `ReleaseCapacity()` to `TicketType`) that decrements `UsedCapacity` for each slug.
- Add a `ReleaseTickets` use case (`ReleaseTicketsCommand` + `ReleaseTicketsHandler`) that loads the `Registration` to retrieve its ticket slugs, loads the `TicketCatalog`, and calls `Release`.
- Add a domain event handler that reacts to `RegistrationCancelledDomainEvent` and dispatches `ReleaseTicketsCommand`.
- Guard against underflow: `UsedCapacity` must not go below zero.

**Non-Goals:**
- Correcting historical `UsedCapacity` values for previously cancelled registrations (out of scope; a data migration would be required separately).
- Releasing capacity for coupon-based registrations where no `TicketCatalog` exists — the handler will skip gracefully if the catalog is not found.
- Any API surface changes; this is a fully internal, event-driven behavior.

## Decisions

### D1 — Symmetric `Release` on `TicketCatalog` / `TicketType`

**Decision:** Add `Release(IReadOnlyList<string> slugs)` to `TicketCatalog` and `ReleaseCapacity()` to `TicketType` that decrements `UsedCapacity`, clamped at zero.

**Rationale:** Symmetric to `Claim`. Clamping at zero prevents underflow in unlikely edge cases (e.g. double-processing of the same event).

**Alternatives considered:** Throwing an exception on underflow — rejected because idempotent, at-least-once delivery means double processing must not produce errors.

**Skipping unknown slugs:** If a ticket-type slug no longer exists in the catalog (e.g. the ticket type was subsequently cancelled/removed), the release is silently skipped for that slug. This matches the defensive posture already used in the domain.

### D2 — Load `Registration` in the command handler to obtain slugs

**Decision:** `ReleaseTicketsHandler` receives `RegistrationId` + `TicketedEventId`, loads the `Registration` to read its `Tickets` slugs, then loads the `TicketCatalog` and calls `Release`.

**Rationale:** The domain event already carries `RegistrationId` and `TicketedEventId`. Re-loading the Registration inside the command handler keeps the domain event lean and avoids denormalising slug data into the event (which is already available on the aggregate in the same unit of work).

**Alternatives considered:** Including slugs directly in `RegistrationCancelledDomainEvent` — rejected to keep domain events minimal and avoid coupling event shape to application concerns.

### D3 — Event-handler → command dispatch pattern

**Decision:** Follow the existing `WriteActivityLog` pattern: `RegistrationCancelledDomainEventHandler` implements `IDomainEventHandler<RegistrationCancelledDomainEvent>` and dispatches `ReleaseTicketsCommand` via `IMediator`.

**Rationale:** Consistent with the established pattern in this module. Auto-registered by `AddModuleEventHandlersFromAssembly`. No explicit handler registration needed.

### D4 — Skip gracefully when catalog is absent

**Decision:** If `TicketCatalog` for the event is not found, the handler returns without error.

**Rationale:** Coupon-only registrations can exist without a catalog. This is a valid state and should not produce errors on cancellation.

### D5 — Exactly-once processing via `ProcessedMessage` table

**Decision:** Add a `ProcessedMessage` entity and `processed_messages` table to the Registrations module, following the `MessageLog` pattern established in the legacy `Admitto.Infrastructure` project. `ReleaseTicketsHandler` checks for an existing record keyed by `RegistrationId + handler type name` before processing, and writes the record atomically on success (within the same transaction).

**Rationale:** Although domain event handlers run within the same transaction as the originating command (making double-processing unlikely in the normal path), defensive exactly-once semantics prevent issues under retry scenarios, future refactors that move processing outside the transaction boundary, or any infrastructure-level replays. The legacy `MessageLog` table already demonstrates this pattern is valued in this codebase.

**Alternatives considered:** Relying solely on zero-clamping of `UsedCapacity` (D1) — rejected because it provides capacity-level idempotency but not handler-level idempotency; concurrent retries could still produce intermediate incorrect `UsedCapacity` values before clamping kicks in. Sharing the legacy `Admitto.Infrastructure` `MessageLog` table — rejected because modules must not depend on legacy infrastructure; each module owns its persistence.

## Risks / Trade-offs

- **Concurrency / double-release** → Clamping `UsedCapacity` at zero (D1) prevents underflow. Domain event handlers run in-process within the same transaction so replay risk is minimal; the `ProcessedMessage` infrastructure (D5) is available for future integration/module event handlers on the bus.
- **Cancelled ticket types** → A ticket type may be cancelled after registration but before cancellation. `Release` will skip unknown/removed slugs silently (D1), so no error; UsedCapacity on a cancelled TicketType remains unaffected (already inaccessible for new claims).
