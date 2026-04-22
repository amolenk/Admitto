# ADR-008: TicketedEvent ownership moved to Registrations; EventStatus projected onto TicketCatalog

## Status
Accepted. Supersedes [ADR-007](adr-007-lifecycle-guard-pattern.md).

## Context
The previous design split a ticketed event across two modules:

- **Organization** owned the authoritative `TicketedEvent` aggregate (slug, name, dates, status).
- **Registrations** owned the `TicketCatalog`, attendee registrations, three independent policy aggregates (`EventRegistrationPolicy`, `CancellationPolicy`, `ReconfirmPolicy`), and a `TicketedEventLifecycleGuard` that mirrored event status from Organization so local registration invariants could be enforced.

This split produced two recurring problems:

1. **Weak consistency where it matters most.** "No registrations after cancel" and "no policy edits on a cancelled event" lived in Registrations, but the source of truth for event status lived in Organization. `TicketedEventLifecycleGuard` papered over the sync gap, and policy logic fragmented across three aggregates.
2. **Strong consistency where we do not need it.** Event creation sat in Organization, but Organization did not own the data to enforce registration-side invariants. Team-archive-only-when-no-active-events ought to be a local check, but depended on Registrations-side state being in sync.

## Decision
Move the authoritative `TicketedEvent` aggregate — identity, slug, name, dates, lifecycle status, and all three policies — entirely into the Registrations module. Consolidate the three standalone policy aggregates into value objects on `TicketedEvent` (`TicketedEventRegistrationPolicy`, `TicketedEventCancellationPolicy`, `TicketedEventReconfirmPolicy`). Delete `TicketedEventLifecycleGuard`. Extend the existing `TicketCatalog` aggregate with a single `EventStatus` field (Active / Cancelled / Archived) projected from `TicketedEvent` in the same unit of work as any lifecycle transition.

Organization keeps only a `Team` aggregate with four integer counters — `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, `PendingEventCount` — plus a bounded `TeamEventCreationRequest` child entity for in-flight creation requests. No per-event rows are stored in Organization. Slug uniqueness is enforced in Registrations via a unique index on `(TeamId, Slug)`.

Event creation becomes a two-phase async flow:

1. `POST /admin/teams/{teamSlug}/events` on Organization validates team-level invariants, increments `PendingEventCount`, persists a `TeamEventCreationRequest`, outboxes `TicketedEventCreationRequested`, and returns `202 Accepted` with a creation-status link.
2. Registrations consumes `TicketedEventCreationRequested`, attempts to insert `TicketedEvent` (catching `(TeamId, Slug)` unique-violation as `duplicate_slug`), creates `TicketCatalog` in the same UoW, and outboxes either `TicketedEventCreated` or `TicketedEventCreationRejected`.
3. Organization's integration-event handler decrements `PendingEventCount` and advances the matching active/rejected state on the creation-request record. A Quartz job (`ExpireStaleEventCreationRequestsJob`) expires stale `Pending` requests after a configurable timeout and rolls back the counter.

Cancel and archive live on Registrations; the aggregate raises an in-module `TicketedEventStatusChanged` domain event that projects onto `TicketCatalog.EventStatus` in the same UoW, and the same UoW outboxes `TicketedEventCancelled` / `TicketedEventArchived` so Organization can adjust its counters.

## Rationale
- **Single source of truth for event data.** Every policy read and enforcement happens inside Registrations. The aggregate owns its own invariants; no sync mirror needed.
- **Atomic lifecycle gate for registration.** `TicketCatalog.EventStatus` is updated in the same transaction as the `TicketedEvent` status change, so optimistic concurrency on `TicketCatalog` catches any in-flight registration whose claim would otherwise land after a lifecycle transition.
- **Bounded Organization footprint.** Four integer counters plus a bounded creation-request child replace an unbounded event list. Team-archive becomes a strongly-consistent local check.
- **Collapsed policy fragmentation.** One aggregate carries three cohesive policy value objects. Adding a fourth policy no longer requires a new aggregate, table, or guard.

## Consequences
### Positive
- Strong consistency for all attendee-visible invariants (no registrations after cancel/archive, capacity, policy windows).
- Team-archive is a local Organization check against bounded counters.
- `TicketedEventLifecycleGuard`, three standalone policy aggregates, and the Organization-side `TicketedEvent` aggregate are all deleted — substantial simplification.
- Slug uniqueness is a single DB invariant (`(TeamId, Slug)` unique index) rather than a cross-module coordination problem.

### Negative
- Event creation is asynchronous from the client's perspective. Slug conflicts surface via the creation-status polling endpoint rather than as a synchronous 400. Mitigation: the Admin UI polls and attaches the error to the slug field; the CLI offers a `--wait` helper that polls until terminal state.
- Two aggregates (`TicketedEvent` + `TicketCatalog`) must be loaded together on the registration hot path. Both live in the same module and DbContext, so this is a single round-trip; the commit still goes through one UoW.
- Integration-event handlers must be idempotent on `CreationRequestId` (create flow) and on `TicketedEventId` + observed transition (cancel/archive). This is already the module default.
- A stale or lost `TicketedEventCreationRequested` can inflate `PendingEventCount` and block team-archive. Mitigation: `ExpireStaleEventCreationRequestsJob` rolls back pending requests older than a configurable timeout.

## References
- arc42 chapter 5 — building-block view (Organization and Registrations module responsibilities).
- arc42 chapter 6 — runtime view (event creation, cancel/archive, attendee registration, policy mutation flows).
- arc42 chapter 8 — cross-cutting concepts (messaging taxonomy, write-amplifier pattern, in-aggregate lifecycle invariants).
- Change: `openspec/changes/redesign-ticketed-event-ownership/` (proposal and design).
