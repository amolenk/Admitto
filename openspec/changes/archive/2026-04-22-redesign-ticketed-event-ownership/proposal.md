## Why

The current design splits the lifecycle of a ticketed event between two modules: the Organization module owns the canonical `TicketedEvent` aggregate (slug, name, dates, status), while the Registrations module owns parallel artifacts (ticket types, registrations, policies) wired together via integration events and a `TicketedEventLifecycleGuard` shadow aggregate. This produces two recurring problems:

1. **Weak consistency where it matters most.** Capacity, "no registrations after cancel", reconfirm and cancellation policy enforcement live in Registrations, but the source of truth for event status lives in Organization. Synchronisation lags through integration events have to be papered over with `TicketedEventLifecycleGuard`, and policy logic is scattered across `RegistrationPolicy`, `CancellationPolicy`, and `ReconfirmPolicy` in different aggregates.
2. **Strong consistency where we don't need it.** Event creation is owned by Organization, but Organization doesn't have the data to enforce the registration-side invariants. Conversely, archive-team-only-when-no-active-events should be a simple Organization-side check, but today it depends on Registrations state being in sync.

Moving authoritative event ownership to Registrations and consolidating policies onto the `TicketedEvent` aggregate reverses this: Organization keeps the simple Team-level invariants it can enforce locally, and Registrations gets strong consistency for everything that protects attendees.

## What Changes

### Event ownership

- **BREAKING**: The authoritative `TicketedEvent` aggregate moves to the Registrations module. The Organization module no longer stores event details.
- The `Team` aggregate in Organization gains three integer counters — `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount` — plus a small `PendingEventCount` for in-flight creation requests. **No per-event rows are stored in Organization**; the storage footprint per team is bounded.
- **Slug uniqueness moves to Registrations**, enforced by a unique index on the `TicketedEvent` table (`TeamId + Slug`). Organization does not need to store slugs.
- Organization remains the **gatekeeper** for event creation: clients post creation requests to Organization, which validates team state (e.g. team not archived), increments `PendingEventCount`, and emits a `TicketedEventCreationRequested` integration event.
- Registrations consumes the integration event and asynchronously creates the `TicketedEvent` aggregate. On success it emits `TicketedEventCreated` (Organization decrements pending, increments active); on slug conflict or other validation failure it emits `TicketedEventCreationRejected` (Organization decrements pending and records the failure for the polling endpoint).
- The Admin UI submits the create request and **polls a creation-status endpoint** showing a spinner until the Registrations-side event is materialised — and surfaces slug conflicts via that same endpoint.

### Policy consolidation

- **BREAKING**: `RegistrationPolicy`, `CancellationPolicy`, and `ReconfirmPolicy` collapse into the `TicketedEvent` aggregate as cohesive policy value objects/state, named `TicketedEventRegistrationPolicy`, `TicketedEventCancellationPolicy`, and `TicketedEventReconfirmPolicy` respectively.
- **BREAKING**: `TicketedEventLifecycleGuard` is removed. The `TicketedEvent` aggregate enforces lifecycle rules directly.
- Existing legacy `Admitto.Domain.TicketedEvent` (orphaned legacy project) is **out of scope** — it remains untouched and is left for a separate cleanup change.

### TicketCatalog extension (status-only projection)

- The existing `TicketCatalog` aggregate (in Registrations, keyed by `TicketedEventId`) is **extended** with one piece of state: a read-only projection of event status (Active / Cancelled / Archived) copied from `TicketedEvent` via in-module domain events.
- **`TicketCatalog` is intentionally not a full mirror of `TicketedEvent`.** It only owns what must be checked atomically with the ticket claim: capacity (already there) and event status (new). Policy details (registration windows, cancellation windows, reconfirm rules, etc.) stay on `TicketedEvent` and are not duplicated.
- Status is the only field that **must** be strongly consistent with the claim — otherwise we'd have a grey area where a registration sneaks in after archive/cancel. All other policy invariants are time-window or rule-based and tolerate the small staleness window of an application-layer load.
- Because both aggregates live in the same module, the status projection is kept in sync through in-module domain events; the projection update and the source-of-truth status change commit in the same unit of work.

### Registration / cancellation flows (handler-side policy checks)

- Application-layer handlers for registration and registration cancellation load the full `TicketedEvent` aggregate to evaluate `TicketedEventRegistrationPolicy` / `TicketedEventCancellationPolicy` / `TicketedEventReconfirmPolicy` invariants (windows, attendee limits, refund rules, etc.).
- After policy invariants pass, the same handler loads `TicketCatalog` and calls `Claim(...)` (or the cancel equivalent), which atomically re-checks status + capacity. Both aggregates are loaded and saved in the same unit of work, so a concurrent cancel/archive on `TicketedEvent` — which propagates a status update to `TicketCatalog` in the same transaction as the lifecycle change — is caught by optimistic concurrency at save time.
- Net effect: policies live on `TicketedEvent` (single source of truth, no duplication), but the lifecycle gate stays atomic.

### Cancel & archive flows

- Cancel and archive operations on a `TicketedEvent` are owned by Registrations. They emit in-module domain events that update the local `TicketCatalog` event-status projection, and integration events back to Organization to update the team's counter (decrement active, increment cancelled/archived).
- Team archive remains an Organization-only operation, gated by `ActiveEventCount == 0` and `PendingEventCount == 0` on the `Team` aggregate — a strongly-consistent local check.

## Capabilities

### New Capabilities

_None._ (`TicketCatalog` already exists as an aggregate in Registrations; its responsibilities are extended under the existing `ticket-type-management` capability — see below.)

### Modified Capabilities

- `event-management`: Event creation becomes a two-phase async flow (Organization gatekeeper → Registrations materialisation). Event update, cancel, and archive operations move to Registrations. The `TicketedEvent` aggregate (in Registrations) gains consolidated policy state and replaces lifecycle-guard enforcement. Slug uniqueness within a team is enforced by Registrations. Absorbs the Org → Reg side of the former `event-lifecycle-sync` capability: publishing `TicketedEventCreationRequested` on create, and consuming `TicketedEventCreated` / `TicketedEventCreationRejected` / `TicketedEventCancelled` / `TicketedEventArchived` integration events from Registrations.
- `team-management`: Team archive guard is reframed as a local invariant on the team's event counters (zero active and zero pending events) instead of relying on cross-module synchronisation. Adds the `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, and `PendingEventCount` fields to the `Team` aggregate. Absorbs the team-counter-maintenance side of the former `event-lifecycle-sync` capability: the counters are advanced/rolled back by handlers reacting to the `TicketedEvent*` integration events listed above.
- `event-lifecycle-sync`: **Removed.** Responsibilities are split between `event-management` (event-side integration events) and `team-management` (team counter maintenance); this delta deletes the spec.
- `event-lifecycle-guard`: **Removed.** The `TicketedEvent` aggregate in Registrations subsumes its role; this delta deletes the requirements.
- `registration-policy`: Requirements move onto the `TicketedEvent` aggregate as `TicketedEventRegistrationPolicy`; the standalone `EventRegistrationPolicy` aggregate is removed. Enforcement semantics remain the same.
- `cancellation-policy`: Same — consolidated into `TicketedEvent` as `TicketedEventCancellationPolicy`.
- `reconfirm-policy`: Same — consolidated into `TicketedEvent` as `TicketedEventReconfirmPolicy`.
- `attendee-registration`: Registration and cancellation handlers load `TicketedEvent` for policy invariants (windows, limits, refund rules) and `TicketCatalog` for the atomic status + capacity gate. Both aggregates load/save in the same unit of work; optimistic concurrency on `TicketCatalog` (whose status updates commit in the same transaction as a `TicketedEvent` cancel/archive) prevents stale-policy registrations from sneaking past archive/cancel.
- `ticket-type-management`: Extends the existing `TicketCatalog` aggregate with a single read-only event-status field (Active/Cancelled/Archived) kept in sync with `TicketedEvent` via in-module domain events. `TicketCatalog` remains the strongly-consistent gate for capacity and lifecycle status only — full policy state stays on `TicketedEvent` and is not duplicated.
- `admin-ui-event-management`: Create-event UX changes to submit + poll. Update/cancel/archive endpoints relocate (UI consumes the same paths but they are served by Registrations).
- `admin-ui-event-policies`: Single consolidated policy editor surface aligned with the unified `TicketedEvent` aggregate.

## Impact

- **Modules**:
  - `Admitto.Module.Organization`: Removes `TicketedEvent` aggregate and its persistence; adds four integer counter columns to the `Team` aggregate (`ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, `PendingEventCount`); rewrites event create endpoint as a request-publish gateway.
  - `Admitto.Module.Registrations`: Adds `TicketedEvent` aggregate (with consolidated `TicketedEventRegistrationPolicy` / `TicketedEventCancellationPolicy` / `TicketedEventReconfirmPolicy` and the team-scoped slug uniqueness index); extends the existing `TicketCatalog` aggregate with the event-status projection; removes `EventRegistrationPolicy`, `EventCancellationPolicy`, `EventReconfirmPolicy`, and `TicketedEventLifecycleGuard`.
  - Both `Contracts` projects: New integration events (`TicketedEventCreationRequested`, `TicketedEventCreated`, `TicketedEventCreationRejected`, `TicketedEventCancelled`, `TicketedEventArchived`); removed/renamed events that used to flow Org → Reg.

- **Persistence**: Major schema changes in both modules. EF Core migrations required (organization schema loses event tables and gains four counter columns on `Teams`; registrations schema gains `TicketedEvent` table with a `(TeamId, Slug)` unique index, drops three policy tables and the lifecycle guard table; existing `TicketCatalog` table gains a single `EventStatus` column). Data migration script needed to copy existing event rows from organization → registrations and consolidate policy rows into the new aggregate.

- **HTTP API**:
  - `POST /admin/teams/{teamSlug}/events` keeps its URL but returns `202 Accepted` with a creation-status link. Slug conflicts are no longer returned synchronously — they surface via the polling endpoint.
  - New `GET /admin/teams/{teamSlug}/events/creation-requests/{requestId}` (or similar) for UI polling, returning `Pending` / `Created` / `Rejected{reason}`.
  - `PUT/DELETE/cancel/archive` event endpoints move from Organization to Registrations (URLs unchanged).
  - Policy endpoints (`/registration-policy`, `/cancellation-policy`, `/reconfirm-policy`) consolidate behind the event endpoints or a single policy endpoint (TBD in design).

- **CLI**: `Admitto.Cli` event commands need regeneration after API changes.

- **Admin UI**: Create-event form switches to async submit + polling; existing events list and detail pages keep working but pull data from the relocated endpoints (transparent through the route move). Policy editor surface consolidates.

- **Tests**: Integration tests for both modules need substantial updates. End-to-end API tests verify the new async create flow.

- **Docs**: arc42 chapters 5 (building blocks), 6 (runtime view — event creation, cancel, archive flows), 8 (cross-cutting — module boundaries & consistency model) require updates. New ADR documenting the ownership move and consistency rationale.
