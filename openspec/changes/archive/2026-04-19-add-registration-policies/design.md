## Context

The Registrations module currently has a single `EventRegistrationPolicy` aggregate per event that holds three unrelated things: the registration window + email-domain restriction (true policy data), the lifecycle status synced from the Organization module, and a `RegistrationStatus` (Draft/Open/Closed) toggled by an explicit `OpenRegistration` action. As the module gains more policies (cancellation, reconfirmation), each one will need to repeat the lifecycle check, and there is no shared concurrency anchor that prevents a policy edit from racing with a `TicketedEventCancelledModuleEvent`.

The Organization module already demonstrates a clean pattern for this kind of coordination: `Team` exposes a `TicketedEventScopeVersion` that is incremented by the `TicketedEventCreated` handler. Because `Aggregate<TId>.Version` is an EF row-version `[Timestamp]`, any concurrent `ArchiveTeam` on the same `Team` row conflicts and one side is rejected by the database. We will reuse this pattern verbatim inside the Registrations module via a new `TicketedEventLifecycleGuard` aggregate.

This change also takes the opportunity to remove the explicit "Open registration" action: since the registration window has clear semantics, openness is fully derivable from `now ∈ [opensAt, closesAt)` plus an Active lifecycle. There is no product reason to also model a discrete `RegistrationStatus`.

## Goals / Non-Goals

**Goals:**
- One small, reusable mechanism (`TicketedEventLifecycleGuard`) that gives every Registrations-module policy strong consistency with the event's lifecycle, with no per-policy plumbing.
- Slim policies that each express exactly one product concept: `RegistrationPolicy`, `CancellationPolicy`, `ReconfirmPolicy`.
- Replace the explicit "Open/Close registration" toggle with window-derived behaviour.
- Keep the Organization ↔ Registrations contract unchanged: same `TicketedEventCancelledModuleEvent` / `TicketedEventArchivedModuleEvent`, same publication points.
- Match the existing aggregate, use-case, endpoint, CLI, and admin-UI conventions exactly — no new architectural style.

**Non-Goals:**
- Introducing a generic "policy" framework or base class. The guard is shared; each policy is its own aggregate with its own table and its own use cases.
- Modelling cancellation fees or refund flows — `CancellationPolicy` only models the *late* cutoff.
- Modelling a reconfirmation engine — `ReconfirmPolicy` only stores the configured window and cadence; sending reconfirmation messages is out of scope.
- Cross-module changes in the Organization module (its lifecycle commands, events, and payloads stay as they are).
- Changing the `attendee-registration` flow beyond the lifecycle/window check source-swap.

## Decisions

### D1. Introduce `TicketedEventLifecycleGuard` as a dedicated aggregate (not a field on each policy)

Each event gets exactly one `TicketedEventLifecycleGuard` row in the Registrations schema, keyed by `TicketedEventId`. It owns:

- `LifecycleStatus : EventLifecycleStatus` (Active / Cancelled / Archived)
- `PolicyMutationCount : long`
- inherited `Version` (EF `[Timestamp]` row-version)

**Why a separate aggregate?** Putting lifecycle into one of the policies (today's setup) means every other policy still has to look it up, and the policy that owns it has an artificially privileged role. A dedicated guard:

- removes a hidden coupling: `RegistrationPolicy` no longer "speaks for" the event;
- gives every policy the same access pattern (load guard → assert Active → bump);
- means a bare event with no configured policies can still receive lifecycle events (the guard is auto-created on first lifecycle event, just like the policy is today).

**Alternatives considered:**
- *Keep lifecycle on `RegistrationPolicy`.* Rejected: forces every other policy to depend on `RegistrationPolicy`'s existence and concurrency token.
- *Stateless check against the Organization module via a facade.* Rejected: turns every write into a cross-module call and gives no concurrency guarantee against in-flight lifecycle events.
- *Project lifecycle into a read-side table.* Rejected: read models cannot serve as concurrency tokens; we want strong, not eventual, consistency for write operations.

### D2. The guard pattern: load + assert + increment, in the same UoW as the policy write

Every command that mutates any policy (registration, cancellation, reconfirm, ticket types) follows the same shape inside its handler:

1. Load the `TicketedEventLifecycleGuard` for the event (create it on the fly only inside lifecycle-event handlers — see D5).
2. Call `guard.AssertActiveAndRegisterPolicyMutation()` which throws if status is not Active and otherwise does `PolicyMutationCount++`.
3. Mutate the policy aggregate.
4. Commit one unit of work (the existing endpoint-owned UoW per project conventions).

Because `PolicyMutationCount++` writes the guard row, EF advances `Version`. A concurrent lifecycle handler that loaded the same guard will fail `SaveChangesAsync` with a `DbUpdateConcurrencyException`. Either:

- the policy edit wins and the lifecycle handler retries (and on retry sees Active is no longer true → it now applies the lifecycle change to the (newly bumped) guard), **or**
- the lifecycle handler wins and the policy edit fails with concurrency — the API surfaces this as the existing concurrency conflict response and the operator retries (and sees a non-Active guard → request is rejected with a clear lifecycle error).

**Why this is "simple but powerful":** zero new infrastructure. We reuse `Aggregate<TId>.Version` exactly as `Team`/`TicketedEvent` already do.

**Alternative considered:** advisory locks or a saga. Rejected — we already have row-version optimistic concurrency that solves this with one column.

### D3. Removed: explicit `OpenRegistration` action and `RegistrationStatus` field

`OpenRegistrationCommand`, `OpenRegistrationHandler`, `OpenRegistrationHttpEndpoint`, the `GetRegistrationOpenStatus` use case, and `RegistrationPolicy.RegistrationStatus` are all deleted. The `EventRegistrationPolicy.IsRegistrationOpen(now)` check (window-based) becomes the single source of truth, combined with `guard.LifecycleStatus == Active`.

A read-side query `GetRegistrationOpenStatus` is still useful for the UI / public site — it stays, but its handler simply returns `(now in window) && guard.IsActive`. There is no stored "open/closed" bit.

**Why:** the explicit toggle adds operational burden (organizers must remember to flip it), risks the bit drifting out of sync with the window, and gives no behaviour the window doesn't already give. Removing it is a small simplification with a real footprint reduction.

**Migration impact:** see Migration Plan. Any event currently in `RegistrationStatus.Draft` or `Closed` is unaffected at runtime once the column is dropped — registration acceptance is now purely window+lifecycle driven.

### D4. Three slim policy aggregates

| Aggregate | Key | Fields | Notes |
|---|---|---|---|
| `RegistrationPolicy` | `TicketedEventId` | `Window` (opensAt, closesAt), optional `AllowedEmailDomain` | Reduced from today's aggregate. `LifecycleStatus` and `RegistrationStatus` removed. |
| `CancellationPolicy` | `TicketedEventId` | `LateCancellationCutoff : DateTimeOffset?` | Optional aggregate; absence means no policy configured (caller decides default). |
| `ReconfirmPolicy` | `TicketedEventId` | `Window` (opensAt, closesAt), `Cadence : Duration` | Optional aggregate. Spec defines whether absence means "never reconfirm" (yes — see specs). |

Each is a regular `Aggregate<TicketedEventId>`, lives in `Registrations/Domain/Entities/`, has its own EF mapping in the registrations schema, and its own use-cases folder under `Application/UseCases/<Policy>/`.

**Why three aggregates instead of one big "EventPolicies" aggregate?** They have independent lifecycles and independent admin pages. Bundling them would force unrelated invariants and concurrency tokens together. Three small aggregates fit the existing module style and the guard pattern works the same for all of them.

### D5. Lifecycle event handling

The `TicketedEventCancelled` and `TicketedEventArchived` module-event handlers are rewritten to act on the guard instead of `RegistrationPolicy`:

- Load (or create) the `TicketedEventLifecycleGuard` for the event.
- Set status to `Cancelled` / `Archived`. Idempotent — no-op if already in the target state.
- `PolicyMutationCount++` (so a policy edit racing this event will conflict).
- Commit.

If the guard is created here, `PolicyMutationCount` starts at 0 and is bumped to 1 by this same operation.

The `RegistrationPolicy` aggregate is **not** auto-created by lifecycle events anymore. There is no behavioural reason to: when a registration arrives and there is no policy, we already reject it (no window configured). The guard is the only thing that must exist regardless of operator setup.

### D6. Ticket-type management opts into the guard pattern

`ticket-type-management` use cases (create / update / etc.) gain the same load-guard / assert-Active / bump step. This is a small refactor of existing handlers. Lifecycle status was previously read from `RegistrationPolicy.EventLifecycleStatus`; after this change it comes from the guard. No public contract changes.

### D7. Endpoints, CLI, UI follow existing patterns

- **Endpoints**: feature-sliced under `Application/UseCases/<Policy>/<Action>/AdminApi/<Action>HttpEndpoint.cs`. Registered through the module's endpoint registration entry point per `AGENTS.md`. Endpoint filters apply FluentValidation; endpoints own the unit of work and commit. Handlers do not commit.
- **CLI**: every new admin endpoint gets a corresponding command in `src/Admitto.Cli/Commands/` per `src/Admitto.Cli/AGENTS.md`.
- **UI**: new pages under the event detail area in `src/Admitto.UI.Admin` for cancellation policy and reconfirm policy; the existing registration policy page is updated to drop the Open/Close controls and the lifecycle badge (the latter moves to a higher-level event header).

## Risks / Trade-offs

- **[Risk] Migration drops `RegistrationStatus` column.** → Mitigation: ship the column drop only after the code path is gone in the same release; staging deploy verifies no consumer reads the column. Add a migration step (see Migration Plan) that backfills the new guard table.
- **[Risk] Lifecycle handler vs. policy edit ping-pong on retry.** → Mitigation: outbox-driven handlers already retry on concurrency conflict; the worst case is one retry. Lifecycle is a terminal transition (Active → Cancelled → Archived) so retries converge.
- **[Risk] Operators may rely on the explicit "Open" toggle today.** → Mitigation: removing it is called out as **BREAKING** in the proposal. Window semantics are equivalent to today's "Open" once the window is set. We will surface a clear UI message for events without a window: "Registration window not configured — registrations are closed."
- **[Risk] `PolicyMutationCount` grows unbounded over an event's lifetime.** → Trade-off: it is a `long` and cannot realistically overflow. Not a concern.
- **[Trade-off] One extra row read per policy command.** → Acceptable: it is a single PK lookup in the same connection; it is the cost of strong consistency without distributed locks.
- **[Trade-off] `RegistrationPolicy`, `CancellationPolicy`, `ReconfirmPolicy` are separate tables.** → Acceptable: matches their independent lifecycles and admin surfaces; joining is not required on hot paths.

## Migration Plan

1. **Schema (Registrations module)**
   - Add `EventLifecycleGuards` table (`EventId PK`, `LifecycleStatus`, `PolicyMutationCount`, audit + version columns).
   - Backfill: for each existing `EventRegistrationPolicy`, insert a guard row with the policy's current `EventLifecycleStatus` and `PolicyMutationCount = 0`.
   - Drop `EventLifecycleStatus` and `RegistrationStatus` columns from `EventRegistrationPolicies`.
   - Add `CancellationPolicies` and `ReconfirmPolicies` tables (empty).
   - All in one EF migration generated via the `ef-migrations` skill — no manual edits.
2. **Code**
   - Introduce the new aggregates and refactor existing handlers to use the guard.
   - Delete `OpenRegistration*` and `GetRegistrationOpenStatus` write paths; keep the read query backed by window + guard.
   - Add new policy use cases, endpoints, CLI commands, and UI pages.
3. **Tests**
   - Domain tests for the guard (Active assertion, idempotent lifecycle transitions, mutation count bump).
   - Integration tests for guard ↔ policy concurrency (one wins, the other gets concurrency conflict).
   - Endpoint and CLI tests per the acceptance scenarios in the specs.
4. **Docs**
   - Update arc42 ch. 5 (building blocks: new aggregates), ch. 6 (runtime view: lifecycle sync flow targets the guard), ch. 8 (crosscutting: document the guard pattern).
   - Add an ADR: "Lifecycle Guard pattern in the Registrations module".
5. **Rollback**
   - Single deployable unit; rollback is a single schema revert + previous container image. The backfill is non-destructive in the forward direction (guards inserted, columns dropped last); rollback recreates the columns and reads back from the guard table before dropping it.

## Open Questions

- **OQ1.** Does absence of a `ReconfirmPolicy` mean "never reconfirm" or "use a system default"? *Default proposed: never reconfirm (no implicit defaults). Confirm in specs.*
- **OQ2.** Does absence of a `CancellationPolicy` mean "no cancellations are ever late" or "all attendee-initiated cancellations are late"? *Default proposed: no cutoff configured ⇒ no cancellation is ever considered late. Confirm in specs.*
- **OQ3.** Should the guard's `PolicyMutationCount` be exposed on any read API (e.g. as an ETag for the event-policies page)? *Default proposed: no — `Version` is already exposed where needed via the standard optimistic-concurrency surface.*
