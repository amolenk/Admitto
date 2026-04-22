## Why

The current Registrations module mixes three concerns into one `RegistrationPolicy` aggregate: the registration rules themselves (window, email domain), event lifecycle status (Active / Cancelled / Archived), and — by implication — every other rule that should only change while the event is Active. This makes it hard to add new policies (cancellation, reconfirmation) without each of them re-implementing the lifecycle check, and there is no shared mechanism that guarantees strong consistency between "the event is still Active" and "I am updating a policy".

We need a simple, reusable building block: a single `TicketedEventLifecycleGuard` aggregate per event in the Registrations module that owns the lifecycle status and acts as the synchronization point for every policy mutation. Combined with the existing optimistic concurrency in `AggregateRoot`, this gives us strong consistency between lifecycle transitions and policy edits with no extra plumbing per policy.

## What Changes

- **NEW** `TicketedEventLifecycleGuard` aggregate in the Registrations module. Owns:
  - `EventId`
  - `LifecycleStatus` (Active / Cancelled / Archived)
  - `PolicyMutationCount` — incremented on every successful policy mutation **and** on every lifecycle transition.
  - Standard optimistic-concurrency `Version` (from `AggregateRoot`).
- **NEW** Pattern: every policy command that mutates a policy first loads the guard, asserts `LifecycleStatus == Active`, increments `PolicyMutationCount`, and is committed in the same unit of work as the policy change. Concurrent lifecycle events therefore conflict on `Version` and one side is rejected — the same model used between `Team` and `TicketedEvent`.
- **BREAKING** Lifecycle status moves out of `RegistrationPolicy` and into the new guard aggregate. `RegistrationPolicy` no longer has a `LifecycleStatus` field. The `event-lifecycle-sync` capability is rewritten to target the guard.
- **BREAKING** `RegistrationPolicy` is reduced to its true policy concerns: registration window and optional email domain restriction. (Ticket types and capacity remain in `ticket-type-management` but participate in the guard pattern.)
- **BREAKING** Remove the explicit "Open registration" action (`OpenRegistration` command, handler, and admin endpoint). Whether registration is open is derived purely from the registration window: a registration is accepted when `now ∈ [window.opensAt, window.closesAt)` **and** the lifecycle guard is `Active`. There is no separate `RegistrationStatus` to toggle.
- **NEW** `CancellationPolicy` aggregate: holds the `LateCancellationCutoff` (the moment after which an attendee cancellation is considered "late"). One per event, optional.
- **NEW** `ReconfirmPolicy` aggregate: holds the reconfirmation `Window` (open/close) and `Cadence` (how often attendees are asked to reconfirm). One per event, optional.
- **NEW** Admin API endpoints + CLI commands + admin UI pages to view and manage cancellation and reconfirm policies, plus the existing registration policy.
- **MODIFIED** `attendee-registration` and any other consumer no longer reads lifecycle status from `RegistrationPolicy`; it reads from the guard.

## Capabilities

### New Capabilities
- `event-lifecycle-guard`: Per-event aggregate in the Registrations module that tracks lifecycle status and acts as the strong-consistency synchronization point for all policy mutations.
- `cancellation-policy`: Per-event policy describing when an attendee cancellation is considered late.
- `reconfirm-policy`: Per-event policy describing the reconfirmation window and cadence.
- `admin-ui-event-policies`: Admin UI pages for managing the registration, cancellation, and reconfirm policies of an event.

### Modified Capabilities
- `registration-policy`: Lifecycle status is removed; the spec is reduced to registration window and optional email-domain restriction, and policy mutations now go through the lifecycle guard.
- `event-lifecycle-sync`: Lifecycle events from the Organization module are now applied to the `TicketedEventLifecycleGuard` aggregate instead of `RegistrationPolicy`. Handling remains idempotent and creates the guard if missing.
- `attendee-registration`: Lifecycle checks read from the guard instead of `RegistrationPolicy.LifecycleStatus`.
- `ticket-type-management`: Ticket-type mutations participate in the guard pattern (lifecycle assertion + `PolicyMutationCount` bump in the same unit of work).
- `cli-admin-parity`: Add CLI commands matching the new policy management endpoints.

## Impact

- **Code**:
  - `src/Admitto.Module.Registrations/Domain/`: new `TicketedEventLifecycleGuard`, `CancellationPolicy`, `ReconfirmPolicy` aggregates; `RegistrationPolicy` slimmed down.
  - `src/Admitto.Module.Registrations/Application/`: new use cases and event handlers; existing handlers refactored to load the guard, assert Active, and bump `PolicyMutationCount`.
  - `src/Admitto.Module.Registrations/Infrastructure/`: EF mappings for the new aggregates; migration for the schema split (move lifecycle column out of `RegistrationPolicy`, add `EventLifecycleGuards`, `CancellationPolicies`, `ReconfirmPolicies` tables).
  - `src/Admitto.Api/`: new admin endpoints under `/admin/...` for cancellation and reconfirm policies; existing registration-policy endpoints adjusted.
  - `src/Admitto.Cli/Commands/`: new commands mirroring the admin endpoints.
  - `src/Admitto.UI.Admin/`: new pages under the event detail area for cancellation and reconfirm policies; updated registration policy page.
- **Data**: schema-per-module migration in the Registrations schema.
- **Cross-module contracts**: no new integration events; `TicketedEventCancelledModuleEvent` / `TicketedEventArchivedModuleEvent` payloads are unchanged.
- **Tests**: domain tests for the guard pattern and each new policy; integration tests for the guard ↔ policy concurrency behaviour; endpoint and CLI tests for the new commands.
- **Docs**: update arc42 chapters 5 (building blocks), 6 (runtime view — lifecycle sync flow), and 8 (crosscutting — guard pattern); add an ADR for the lifecycle-guard pattern.
