# ADR-007: Lifecycle Guard Pattern in the Registrations Module

## Status
Accepted

## Context
The Registrations module manages multiple policy aggregates per ticketed event — registration policy, cancellation policy, and reconfirmation policy. Each policy can be independently configured by organizers via admin endpoints. The event's lifecycle (Active → Cancelled → Archived) is managed by the Organization module and synced to Registrations via module events.

Two problems arise:

1. **Lifecycle coupling**: Previously, `EventRegistrationPolicy` owned the event's lifecycle status, forcing every other policy to depend on it. As new policy types are added, this creates an artificial privilege for one aggregate.
2. **Concurrency gap**: A policy edit and a lifecycle transition (e.g. event cancellation) can race. Without a shared concurrency anchor, an organizer could update a policy after the event is cancelled, or a cancellation event could be applied mid-edit, leading to inconsistent state.

The Organization module already solves an analogous problem: `Team.TicketedEventScopeVersion` is a write-amplifier counter that prevents concurrent event creation during team archiving (see [§8.9 Write-amplifier pattern](../arc42/08-crosscutting-concepts.md#write-amplifier-pattern)).

## Decision
Introduce a `TicketedEventLifecycleGuard` aggregate in the Registrations module — one row per ticketed event — that owns the synced lifecycle status and a monotonically-increasing `PolicyMutationCount`. Every command that mutates any policy aggregate follows a standard protocol:

1. Load the guard (create on first access).
2. Assert the event is Active and increment `PolicyMutationCount`.
3. Mutate the policy aggregate.
4. Commit in the endpoint-owned unit of work.

The `PolicyMutationCount++` write advances the guard's EF row-version (`[Timestamp] Version`). A concurrent lifecycle handler that loaded the same guard at a prior version will fail with `DbUpdateConcurrencyException`, and vice versa.

Lifecycle event handlers (Created, Cancelled, Archived) load-or-create the guard and set the status. They bump `PolicyMutationCount` only on real state transitions (idempotent on re-delivery).

As part of this change, the explicit "Open/Close Registration" toggle and `RegistrationStatus` field are removed. Registration openness is now derived from `now ∈ [opensAt, closesAt)` combined with `guard.IsActive`.

## Rationale
- **No per-policy plumbing**: Every policy uses the same guard with zero additional infrastructure. Adding a fourth policy requires no guard changes.
- **Reuses existing concurrency mechanism**: EF row-version optimistic concurrency is already proven in the codebase (`Team.TicketedEventScopeVersion`). No advisory locks, sagas, or distributed transactions needed.
- **Removes hidden coupling**: `EventRegistrationPolicy` no longer "speaks for" the event. All policies have equal access to lifecycle state.
- **Simplifies the model**: Removing the explicit Open/Close toggle eliminates a toggleable bit that could drift out of sync with the registration window.

## Consequences
### Positive
- Strong consistency between policy edits and lifecycle transitions without distributed locks.
- Three independent, slim policy aggregates (`EventRegistrationPolicy`, `CancellationPolicy`, `ReconfirmPolicy`) with their own tables and lifecycle.
- The guard auto-creates on first lifecycle event or first policy mutation — bare events with no configured policies still receive lifecycle events correctly.
- One extra PK lookup per policy mutation (the guard) — acceptable cost for strong consistency.

### Negative
- `PolicyMutationCount` grows unbounded over an event's lifetime. As a `long`, this is not a practical concern.
- The guard pattern is unfamiliar to contributors who haven't seen the write-amplifier pattern. Mitigated by documenting it in [§8.14](../arc42/08-crosscutting-concepts.md#814-lifecycle-guard-pattern).
- Removing the Open/Close toggle is a breaking change for operators who relied on it. Mitigated by the fact that window-derived openness is functionally equivalent once a window is configured.
