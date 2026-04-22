## Context

Admitto is a modular monolith (ADR-001) with two primary business modules today:

- **Organization** owns the `Team` aggregate **and** the authoritative `TicketedEvent` aggregate (slug, name, dates, status).
- **Registrations** owns attendee registrations, the `TicketCatalog` aggregate (ticket types + capacity, keyed by `TicketedEventId`), three standalone policy aggregates (`EventRegistrationPolicy`, `EventCancellationPolicy`, `EventReconfirmPolicy`), and the `TicketedEventLifecycleGuard` aggregate that mirrors event status from Organization so that registration-time invariants can be enforced locally.

Cross-module communication happens through integration events on `IIntegrationEvent` (see `src/Admitto.Module.Shared/Application/Messaging/`) delivered via the outbox + Azure Storage Queue pipeline. In-module work uses domain events (same transaction) and `ModuleEvent`s (same module, async).

The current split creates two classes of problems:

1. **Weak consistency where attendees care.** Capacity and "no registrations after cancel/archive" live in Registrations, but the source of truth for event status lives in Organization. `TicketedEventLifecycleGuard` was introduced to paper over the sync gap, and policy rules are split across three aggregates — which makes "update registration window" touch one aggregate, but "register an attendee" consults three plus the guard.
2. **Strong consistency where we don't need it.** Event creation sits in Organization but needs to enforce invariants that only Registrations has (ticket catalog existence, registration policy validity). Team-archive needs to know whether any events are still active, but today that requires Registrations-side data to be in sync with Organization.

This design reverses the ownership: Organization keeps only the small Team-level invariants it can enforce locally and acts as the **gatekeeper** for event creation; Registrations becomes the **owner** of the event itself, its policies, and the registration gate.

Stakeholders: backend engineers (two modules touched), Admin UI (create-event UX becomes async), CLI (regenerated from the API).

Relevant arc42 references:
- Ch. 5 — Building block view (module boundaries)
- Ch. 6 — Runtime view (event creation, cancel, archive flows)
- Ch. 8 — Cross-cutting concepts (consistency model, messaging taxonomy)

## Goals / Non-Goals

**Goals:**

- Move the authoritative `TicketedEvent` aggregate into the Registrations module.
- Consolidate `RegistrationPolicy` / `CancellationPolicy` / `ReconfirmPolicy` into value objects on `TicketedEvent` (`TicketedEventRegistrationPolicy` / `TicketedEventCancellationPolicy` / `TicketedEventReconfirmPolicy`).
- Remove `TicketedEventLifecycleGuard`; replace it with a single `EventStatus` field on the existing `TicketCatalog`, projected from `TicketedEvent` via in-module domain events in the same unit of work as the lifecycle change.
- Keep Organization as the creation **gatekeeper**: `Team` validates team-level invariants and publishes `TicketedEventCreationRequested`; Registrations materialises the `TicketedEvent` asynchronously and reports back with `TicketedEventCreated` / `TicketedEventCreationRejected`.
- Give `Team` bounded integer counters (`ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, `PendingEventCount`) so team-archive becomes a strongly-consistent local check.
- Preserve all current attendee-visible guarantees: no registrations after cancel/archive, capacity enforcement, policy windows/rules.

**Non-Goals:**

- Cleaning up the orphaned legacy `Admitto.Application` / `Admitto.Domain` / `Admitto.Infrastructure` projects — separate change.
- Public attendee UX changes beyond what falls out of renamed contracts.
- Changes to `coupon-management`, `email-settings`, `team-membership` beyond the event-status projection or counter updates that affect them.
- Splitting Registrations into sub-modules.
- Replacing the outbox + Azure Storage Queue pipeline.

## Decisions

### Decision 1: `TicketedEvent` lives in Registrations; Organization is a gatekeeper only

**Choice:** Move the `TicketedEvent` aggregate — identity, slug, name, dates, lifecycle status, and all three policies — entirely to the Registrations module. Organization retains *only* a `Team` aggregate with four integer event counters.

**Rationale:** Every policy enforced on `TicketedEvent` (registration windows, capacity, reconfirm rules, cancellation refund rules) is read/enforced by the Registrations module. Having Registrations own the aggregate removes the need for a sync mirror and collapses three policy aggregates + one guard aggregate into one.

**Alternatives considered:**

- **A. Keep `TicketedEvent` in Organization, add more fidelity to the guard.** Rejected — doubles down on a sync problem we already have; does not address policy fragmentation.
- **B. Split `TicketedEvent` across both modules (Organization owns slug/name/dates; Registrations owns status + policies).** Rejected — forces every read to cross a module boundary, complicates migration, and leaves the consistency gap unchanged.
- **C. (Chosen) Move everything to Registrations; Organization keeps only Team-level counters.**

### Decision 2: `Team` tracks four bounded integer counters — no slug list

**Choice:** `Team` gains `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, `PendingEventCount`. No per-event rows are stored in Organization. Slug uniqueness is enforced in Registrations by a unique index on `(TeamId, Slug)`.

**Rationale:** Storing a slug list (or any per-event collection) on `Team` reintroduces an unbounded aggregate — exactly the growth problem that surfaced in proposal review. Four counters are bounded and enough for the only Organization-side invariants we care about:

- Team-archive requires `ActiveEventCount == 0 && PendingEventCount == 0`.
- Accepting a creation request requires the team to not be archived.

**Trade-off:** Slug conflicts during creation are detected asynchronously in Registrations, so the UI learns about them via the creation-status polling endpoint rather than as a synchronous 400. We accept this because (a) conflicts are rare in practice, and (b) the create flow is already async for other reasons.

**Alternatives considered:**

- **A. Keep a slug list on `Team`.** Rejected — unbounded.
- **B. Hybrid — track only Active + Cancelled slugs on `Team`, drop Archived.** Rejected — still unbounded in pathological cases and still duplicates naming state already owned by Registrations.
- **C. (Chosen) Counters only; slug uniqueness is a Registrations-side DB invariant.**

### Decision 3: `TicketCatalog` projects only `EventStatus`, not full policy state

**Choice:** Extend the existing `TicketCatalog` aggregate (in Registrations, keyed by `TicketedEventId`) with a single new field: `EventStatus` (Active / Cancelled / Archived). The field is updated by an in-module domain event handler when `TicketedEvent` transitions lifecycle state, in the same unit of work as the `TicketedEvent` change. All other event data stays on `TicketedEvent`.

**Rationale:** Lifecycle status is the *only* invariant where a stale read is unacceptable — a registration slipping in after archive is a business bug. Time-window policies (registration open/close, reconfirm deadline) tolerate the millisecond-level staleness of an application-layer load of `TicketedEvent` because they are themselves time-based. Duplicating everything on `TicketCatalog` would turn it into a full event mirror and reintroduce the sync complexity we are trying to remove.

**Registration-handler pattern:** application handlers load `TicketedEvent` *and* `TicketCatalog` in the same unit of work. Policy invariants are checked against `TicketedEvent`; the claim (`TicketCatalog.Claim(...)`) atomically re-checks `EventStatus` + capacity. Because a concurrent `TicketedEvent.Cancel()` or `Archive()` commits the status projection to `TicketCatalog` in the same transaction as the lifecycle change, EF's optimistic-concurrency token on `TicketCatalog` catches any in-flight registration whose claim would otherwise land after a lifecycle transition. No registration can reach Claim-success while the event is Cancelled/Archived.

**Alternatives considered:**

- **A. Project everything onto `TicketCatalog`.** Rejected — duplicates state, doubles the write-side surface, drifts over time.
- **B. Do nothing on `TicketCatalog`; have the registration handler just read `TicketedEvent` and rely on same-UoW loads.** Rejected — two independent aggregates loaded in one handler is fine for reads, but the *claim* must be atomic with status, and the cleanest way to express that is a single invariant on `TicketCatalog`.
- **C. (Chosen) Project status-only onto `TicketCatalog`.**

### Decision 4: Event creation is a two-phase async flow (202 Accepted + polling)

**Choice:** `POST /admin/teams/{teamSlug}/events` lives on Organization, does team-level validation, increments `PendingEventCount`, writes a creation request record (with a surrogate `CreationRequestId`), outboxes `TicketedEventCreationRequested`, and returns `202 Accepted` with a `creation-status` URL that embeds the `CreationRequestId`. Registrations consumes the event, attempts to create the `TicketedEvent` (which includes the slug-uniqueness index check), and outboxes either `TicketedEventCreated` or `TicketedEventCreationRejected`. Organization handles the response: on success, decrements pending and increments active; on rejection, decrements pending and records the reason on the creation-request record. The Admin UI polls the status endpoint; when `Created`, it follows a `Location` link to the new event.

**Rationale:** Asynchronous creation is forced by the module boundary: the data to enforce creation-time invariants (policy validity, slug uniqueness within Registrations) lives in Registrations. Making it explicit (202 + poll) is simpler than pretending it's synchronous. The `CreationRequestId` surrogate decouples the polling endpoint from the (potentially-not-yet-assigned) slug.

**Implications:**

- `Team` must have a bounded creation-request store or the request record must live elsewhere. We pick the second: a small `TeamEventCreationRequest` entity under the Team aggregate (bounded by `PendingEventCount`, cleaned up on terminal state).
- The CLI needs a `wait-for-creation` helper that abstracts the poll loop, so scripted usage feels synchronous.

**Alternatives considered:**

- **A. Synchronous creation via a cross-module RPC call.** Rejected — violates module boundaries; couples deployments.
- **B. Synchronous creation by moving the creation endpoint to Registrations and having Registrations call back into Organization for the team-archive check.** Rejected — flips the gatekeeper model; makes Registrations responsible for Organization invariants.
- **C. (Chosen) 202 + poll with a creation-request surrogate.**

### Decision 5: `TicketedEventLifecycleGuard` is deleted

**Choice:** Remove the aggregate and its persistence. Its two responsibilities disappear:

- "Mirror event status for local enforcement" → replaced by the `EventStatus` field on `TicketCatalog` (Decision 3).
- "Prevent policy mutations on cancelled/archived events" → now an invariant on `TicketedEvent` itself (the aggregate refuses to mutate its own policies in non-Active states).

**Rationale:** Once `TicketedEvent` lives in Registrations, there is nothing left to guard — the aggregate enforces its own invariants.

### Decision 6: `event-lifecycle-sync` capability is dissolved

**Choice:** The capability is removed. Its responsibilities are absorbed:

- **Event-side integration events** (publish `TicketedEventCreationRequested`, consume `TicketedEventCreated` / `TicketedEventCreationRejected` / `TicketedEventCancelled` / `TicketedEventArchived`) → move to `event-management`.
- **Team counter maintenance** (advance/rollback `ActiveEventCount` / `CancelledEventCount` / `ArchivedEventCount` / `PendingEventCount` in response to those events) → move to `team-management`.

**Rationale:** The old sync capability existed only because `TicketedEvent` lived in Organization while its rules ran in Registrations. After Decision 1, there is no separate "sync" surface; the integration events *are* the event-management and team-management flows.

### Decision 7: Naming — `TicketedEvent*` everywhere

**Choice:** All types introduced or renamed in this change use the `TicketedEvent*` prefix: `TicketedEventRegistrationPolicy`, `TicketedEventCancellationPolicy`, `TicketedEventReconfirmPolicy`, `TicketedEventCreationRequested`, `TicketedEventCreationRejected`, `TicketedEventCreated`, `TicketedEventCancelled`, `TicketedEventArchived`.

**Rationale:** "Event" is overloaded in this codebase (integration event, domain event, business event). `TicketedEvent*` is unambiguous and matches the aggregate name.

## Risks / Trade-offs

- [**Slug-conflict feedback is asynchronous**] → Surface conflicts on the creation-status polling endpoint with a structured error so the Admin UI can attach the message to the slug field. Document in the CLI help that `create-event` may fail asynchronously. Acceptable because slug clashes are rare in practice.

- [**`PendingEventCount` can drift if a `TicketedEventCreationRequested` is lost or permanently unprocessable**] → The creation-request record stores a `RequestedAt` timestamp; a Quartz job expires requests older than a configurable timeout (e.g. 24h), rolls back `PendingEventCount`, and marks the request `Expired`. Keeps the team from being stuck archivable-blocked forever.

- [**Optimistic-concurrency storms on `TicketCatalog` during high-volume registration**] → `TicketCatalog` already absorbs concurrent claims today; the status field is written rarely (once per lifecycle transition), so the additional contention is negligible. Registration handlers must still use retry-on-concurrency semantics, which they already do.

- [**Ordering of integration events**] → `TicketedEventCreated` must not be processed before `TicketedEventCreationRequested` has settled in Organization. The outbox preserves ordering per producer, but cross-module ordering depends on queue delivery. We use an idempotency key (`CreationRequestId`) on every response event and Organization's consumer handles out-of-order arrival by upserting the request record.

- [**Removing three capability specs is a large delta**] → `registration-policy`, `cancellation-policy`, `reconfirm-policy`, `event-lifecycle-guard`, and `event-lifecycle-sync` are all either deleted or heavily modified. Spec-driven tooling will catch missing references at archive time; we still need a careful review to update cross-references in `event-management`, `team-management`, `attendee-registration`, and `ticket-type-management`.

- [**Admin UI create-event UX regression**] → Polling feels slower than a synchronous create. Mitigation: show an inline spinner with ETA copy, keep the UI optimistic about the common case (typically sub-second), and fall back to toast + list-refresh if polling takes unusually long.

- [**Orphaned legacy `Admitto.Domain.TicketedEvent`**] → Out of scope. Explicitly documented in the proposal. A separate change will delete it.

## Migration Plan

Greenfield deployment — no production data to migrate. EF Core migrations are still required to bring the schema to the new shape, but they can be authored as a single new migration per module (no copy/backfill, no dual-write phase, no feature flags). Cutover is the deployment.

Steps:

1. Generate one EF Core migration in **Organization** (drops the legacy `TicketedEvent` and policy tables; adds the four counter columns and the `TeamEventCreationRequest` table).
2. Generate one EF Core migration in **Registrations** (adds the new `TicketedEvent` table with the `(TeamId, Slug)` unique index; adds the `EventStatus` column to `TicketCatalog`; drops `EventRegistrationPolicy` / `EventCancellationPolicy` / `EventReconfirmPolicy` / `TicketedEventLifecycleGuard` tables).
3. Deploy. The new endpoints, integration events, and aggregates are the only code path.

No rollback plan beyond standard `git revert` + redeploy.

## Open Questions

- **Policy endpoint shape:** do `/registration-policy`, `/cancellation-policy`, `/reconfirm-policy` remain as three PUT endpoints against the new `TicketedEvent`, or collapse into a single `PUT /policies` body? Leaning toward keeping three endpoints (minimal UI churn), to be settled in tasks.
- **Creation-request retention:** how long do we keep terminal (created/rejected/expired) `TeamEventCreationRequest` records before purging? 30 days is the default suggestion; confirm with product.
- **Admin UI polling cadence:** 500 ms initial, backing off to 2 s? Exact values are UX decisions, not architectural.
- **CLI `wait-for-creation` default timeout:** 30 s feels right, but scripted CI jobs may want higher. Make it configurable.
