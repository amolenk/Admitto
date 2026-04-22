# Event Lifecycle Guard Specification

### Requirement: Lifecycle guard aggregate exists per event

The Registrations module SHALL maintain exactly one `TicketedEventLifecycleGuard` aggregate per ticketed event. The guard SHALL be keyed by `TicketedEventId` and SHALL expose the event's `LifecycleStatus` (Active, Cancelled, or Archived) and a `PolicyMutationCount`. The guard SHALL use the shared `Aggregate<TId>` base and therefore participate in optimistic concurrency via the standard row-version token.

A guard SHALL NOT be created preemptively. The guard is created on demand: either when a lifecycle module event arrives for an event that has none, or when the first policy mutation for an event requires one (see lifecycle-sync and policy-mutation requirements below). On creation the guard's `LifecycleStatus` defaults to `Active` and `PolicyMutationCount` defaults to `0`.

#### Scenario: Guard does not exist until first needed
- **WHEN** event "conf-2026" exists in the Organization module but no lifecycle event or policy mutation has occurred
- **THEN** no `TicketedEventLifecycleGuard` exists for "conf-2026" in the Registrations module

#### Scenario: Guard is created on first policy mutation
- **WHEN** no guard exists for event "conf-2026" and an organizer successfully configures the registration window
- **THEN** a guard is created for "conf-2026" with `LifecycleStatus = Active` and `PolicyMutationCount = 1`

#### Scenario: Guard is created on first lifecycle event
- **WHEN** no guard exists for event "conf-2026" and a `TicketedEventCancelledModuleEvent` is processed for it
- **THEN** a guard is created for "conf-2026" with `LifecycleStatus = Cancelled` and `PolicyMutationCount = 1`

---

### Requirement: Policy mutations assert Active and bump the mutation count

Every command that mutates any policy aggregate belonging to a ticketed event (registration policy, cancellation policy, reconfirm policy, ticket-type catalog) SHALL, within the same unit of work as the policy write:

1. Load the event's `TicketedEventLifecycleGuard`, creating it if it does not exist.
2. Reject the mutation with a domain error if `LifecycleStatus != Active`.
3. Increment `PolicyMutationCount` by exactly one.
4. Persist both the guard change and the policy change atomically.

Because incrementing `PolicyMutationCount` writes the guard row and therefore advances its row-version concurrency token, any concurrent lifecycle-event handler operating on the same guard SHALL receive a concurrency conflict and retry.

#### Scenario: Policy mutation on an Active guard increments the count
- **WHEN** the guard for event "conf-2026" has `LifecycleStatus = Active` and `PolicyMutationCount = 3` and an organizer configures the cancellation policy
- **THEN** the mutation succeeds, the cancellation policy is persisted, and the guard's `PolicyMutationCount` is 4

#### Scenario: Policy mutation rejected on a Cancelled guard
- **WHEN** the guard for event "conf-2026" has `LifecycleStatus = Cancelled` and an organizer attempts to configure the cancellation policy
- **THEN** the mutation is rejected with reason "event not active" and no state changes

#### Scenario: Policy mutation rejected on an Archived guard
- **WHEN** the guard for event "conf-2026" has `LifecycleStatus = Archived` and an organizer attempts to configure the reconfirm policy
- **THEN** the mutation is rejected with reason "event not active" and no state changes

#### Scenario: Concurrent policy edit vs. lifecycle event
- **WHEN** a policy mutation and a `TicketedEventCancelledModuleEvent` both load the same guard at `Version = N` and both attempt to save
- **THEN** one write succeeds and the other fails with a concurrency conflict; the losing side is retried or rejected per its normal handling

---

### Requirement: Lifecycle transitions bump the mutation count

When a lifecycle module event is processed and causes the guard's `LifecycleStatus` to change, the handler SHALL also increment `PolicyMutationCount` by exactly one in the same unit of work, so that any concurrent policy edit on the same guard sees a concurrency conflict.

When the lifecycle event is idempotent (status is already in the target state), the handler SHALL NOT increment `PolicyMutationCount`.

#### Scenario: Status change bumps the count
- **WHEN** the guard for event "conf-2026" has `LifecycleStatus = Active` and `PolicyMutationCount = 5` and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** the guard has `LifecycleStatus = Cancelled` and `PolicyMutationCount = 6`

#### Scenario: Idempotent lifecycle event does not bump the count
- **WHEN** the guard for event "conf-2026" already has `LifecycleStatus = Cancelled` and `PolicyMutationCount = 6` and another `TicketedEventCancelledModuleEvent` is processed
- **THEN** the guard remains at `LifecycleStatus = Cancelled` and `PolicyMutationCount = 6`

---

### Requirement: PolicyMutationCount is not exposed on any read API

The Registrations module SHALL NOT expose `PolicyMutationCount` on any public or admin read API. It is an internal concurrency-coordination field only. Optimistic-concurrency tokens required by external callers SHALL continue to be the individual aggregate `Version` fields already exposed by those aggregates.

#### Scenario: Event detail response omits the mutation count
- **WHEN** an admin reads the event's policies via any admin API
- **THEN** the response does not include `PolicyMutationCount`
