## MODIFIED Requirements

### Requirement: A per-event Quartz trigger encodes the reconfirm cadence

For every `TicketedEvent` with an active `TicketedEventReconfirmPolicy` and status `Active`, the Email module SHALL register exactly one Quartz trigger keyed by `TicketedEventId` for the static `EvaluateReconfirmJob`. The trigger SHALL fire on a cron expression derived from the policy `Cadence`, evaluated **in the event's `TimeZone`**. The trigger SHALL be bounded by `StartAt = Window.OpensAt` and `EndAt = Window.ClosesAt`. The trigger job data SHALL carry `MinEmailInterval`, `AutoCancelEnabled`, and `MaxReconfirmAttempts` so the tick can evaluate them without additional reads. Trigger creation/replacement SHALL happen idempotently in response to:

- The `TicketedEventCreated` integration event (initial creation when a policy is set at creation).
- A `TicketedEventReconfirmPolicyChanged` integration event (published by Registrations when the policy is set, updated, or cleared — `MinEmailInterval`, `AutoCancelEnabled`, and `MaxReconfirmAttempts` are all included in the payload). The trigger SHALL be removed when the policy is cleared.
- The `TicketedEventTimeZoneChanged` integration event. The trigger SHALL be replaced atomically with one keyed to the new zone.
- The `TicketedEventArchived` integration event (trigger removed).

#### Scenario: Policy added → trigger registered with auto-cancel job data

- **WHEN** an event receives a new reconfirm policy with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`
- **THEN** the Quartz trigger job data carries `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`

#### Scenario: Policy updated with new MaxReconfirmAttempts → trigger job data updated

- **WHEN** an active event's reconfirm policy `MaxReconfirmAttempts` changes from 3 to 5
- **THEN** the trigger job data is updated so the next tick uses `MaxReconfirmAttempts=5`

#### Scenario: AutoCancelEnabled disabled → trigger job data updated

- **WHEN** an organizer sets `AutoCancelEnabled=false` on a policy that previously had it enabled
- **THEN** the trigger job data is updated with `AutoCancelEnabled=false` and the tick no longer auto-cancels

---

## ADDED Requirements

### Requirement: Scheduler tick splits candidates into reconfirm-email and auto-cancel sets

When `EvaluateReconfirmJob` fires for an event whose trigger job data has `AutoCancelEnabled=true`, the tick SHALL extend its existing email log query to also return the **total count** of `reconfirm` emails sent to each candidate (not just the most recent timestamp). The tick then partitions the eligible `Registered, HasReconfirmed=false` candidate set (after applying the `MinEmailInterval` filter) into two disjoint sets:

- **Reconfirm set** — candidates where `email_log_count < MaxReconfirmAttempts`: included in the `BulkEmailJob` as before.
- **Auto-cancel set** — candidates where `email_log_count >= MaxReconfirmAttempts`: excluded from the `BulkEmailJob`; the Email module SHALL enqueue a `ReconfirmAutoExpiredIntegrationEvent { TicketedEventId, RegistrationIds[] }` on its outbox in the same DB transaction.

When `AutoCancelEnabled=false`, all candidates remain in the reconfirm set and no `ReconfirmAutoExpiredIntegrationEvent` is published (existing behaviour).

#### Scenario: Candidate below the limit receives a reconfirm email

- **WHEN** an event has `AutoCancelEnabled=true`, `MaxReconfirmAttempts=2`, and a candidate has received 1 reconfirm email in the log
- **THEN** the candidate is included in the `BulkEmailJob` and is NOT in the auto-cancel set

#### Scenario: Candidate at the limit is auto-cancelled instead of emailed

- **WHEN** an event has `AutoCancelEnabled=true`, `MaxReconfirmAttempts=2`, and a candidate has received 2 reconfirm emails in the log
- **THEN** the candidate is NOT included in the `BulkEmailJob` and IS listed in the `ReconfirmAutoExpiredIntegrationEvent`

#### Scenario: Auto-cancel set and reconfirm set are disjoint in the same tick

- **WHEN** an event tick evaluates three candidates with email log counts 0, 1, and 2 (`MaxReconfirmAttempts=2`, `AutoCancelEnabled=true`)
- **THEN** candidates with counts 0 and 1 are in the `BulkEmailJob`; the candidate with count 2 is in the `ReconfirmAutoExpiredIntegrationEvent`

#### Scenario: Auto-cancel disabled — all candidates receive reconfirm email regardless of log count

- **WHEN** `AutoCancelEnabled=false` and a candidate has received 99 reconfirm emails
- **THEN** the candidate is included in the `BulkEmailJob` (no auto-cancel set produced)

#### Scenario: No ReconfirmAutoExpiredIntegrationEvent published when auto-cancel set is empty

- **WHEN** all candidates are below the attempt threshold
- **THEN** no `ReconfirmAutoExpiredIntegrationEvent` is enqueued for that tick
