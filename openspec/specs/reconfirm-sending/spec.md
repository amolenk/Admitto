# reconfirm-sending Specification

## Purpose

The Email module drives recurring reconfirm emails to unconfirmed attendees of active events with a configured reconfirm policy. Sending is managed through per-event Quartz triggers that create `BulkEmailJob` records on each tick, integrating with the broader `bulk-email` capability for fan-out and logging.

## Requirements

### Requirement: Reconfirm sending targets only registered attendees who have not yet reconfirmed

The Email module SHALL drive recurring `reconfirm` emails to attendees of any `TicketedEvent` that has an active `TicketedEventReconfirmPolicy`. Reconfirm sending SHALL operate only when:

1. The event's status is `Active`.
2. `now` falls inside the policy's `Window` (`[OpensAt, ClosesAt]`).
3. The candidate recipient's registration status is `Registered` AND `HasReconfirmed = false`.
4. The time elapsed since the later of (the attendee's `RegisteredAt`, the last `reconfirm` email sent to the attendee as recorded in `email_log`) is at least `MinEmailInterval` hours.

Eligibility SHALL be (re)evaluated against live Registrations and email-log data on every tick of the per-event Quartz trigger by calling:
- `IRegistrationsFacade.GetRegistrationsAsync(eventId, { Status: Registered, HasReconfirmed: false })` to get candidate attendees, and
- querying the `email_log` for the most recent `reconfirm` email sent to each candidate, to filter out those who received one within the last `MinEmailInterval` hours.

Once an attendee reconfirms, they fall out of the candidate set. Attendees whose last email is within the interval are skipped for that tick and retried on the next tick.

Each tick of the reconfirm scheduler SHALL create one `BulkEmailJob` per event with an `AttendeeSource(status=Registered, hasReconfirmed=false, minEmailIntervalHours=N)`. The job's `EmailType` SHALL be `reconfirm`. The trigger user SHALL be a system-user marker (no real user id).

#### Scenario: Reconfirmed attendees are excluded
- **WHEN** the scheduler ticks for an event with three registered attendees, one of whom has already reconfirmed
- **THEN** the created `BulkEmailJob` resolves to exactly the two who have not reconfirmed

#### Scenario: New registrations between ticks are picked up
- **WHEN** an attendee registers between two scheduled ticks
- **THEN** they are included in the next tick's bulk job (assuming `HasReconfirmed=false`)

#### Scenario: Attendee who reconfirms between ticks is excluded next time
- **WHEN** an attendee was prompted on tick N and reconfirms before tick N+1
- **THEN** they are NOT included in tick N+1's bulk job

#### Scenario: Attendee within MinEmailInterval is skipped
- **WHEN** the scheduler ticks for an event with `MinEmailInterval=24h` and attendee "alice" received a `reconfirm` email 12 hours ago
- **THEN** the `BulkEmailJob` does NOT include "alice" in the recipient set for this tick

#### Scenario: Attendee past MinEmailInterval is included
- **WHEN** the scheduler ticks for an event with `MinEmailInterval=24h` and attendee "bob" received a `reconfirm` email 25 hours ago and has `HasReconfirmed=false`
- **THEN** the `BulkEmailJob` includes "bob" in the recipient set

#### Scenario: New registrant is always eligible on first tick
- **WHEN** an attendee registers and the next tick fires within the same hour
- **THEN** the attendee is included in the `BulkEmailJob` for that tick (no prior email means the interval guard uses `RegisteredAt`)

#### Scenario: Cron schedule encodes cadence; MinEmailInterval throttles per attendee
- **WHEN** the policy `Cadence` is 7d and `MinEmailInterval` is 24h, the trigger window is open, and an unreconfirmed attendee last received a `reconfirm` email 8 days ago
- **THEN** the next tick fires (per cron) and the attendee is included — the cron fires every 7d and MinEmailInterval (24h) is satisfied

#### Scenario: Outside window, no job created
- **WHEN** the scheduler ticks for an event whose `now` is before `OpensAt` or after `ClosesAt`
- **THEN** no `BulkEmailJob` is created (the trigger is bounded by the window)

#### Scenario: Archived event, no job created
- **WHEN** the scheduler ticks for an event whose status is `Archived`
- **THEN** no `BulkEmailJob` is created (the trigger is removed when the event leaves Active)

#### Scenario: Everyone has reconfirmed
- **WHEN** the scheduler ticks for an event with an open window where every registered attendee has `HasReconfirmed=true`
- **THEN** a `BulkEmailJob` is created (for audit) and completes immediately with `RecipientCount=0` and `Status=Completed`

---

### Requirement: A per-event Quartz trigger encodes the reconfirm cadence

For every `TicketedEvent` with an active `TicketedEventReconfirmPolicy` and status `Active`, the Email module SHALL register exactly one Quartz trigger keyed by `TicketedEventId` for the static `EvaluateReconfirmJob`. The trigger SHALL fire on a cron expression derived from the policy `Cadence`, evaluated **in the event's `TimeZone`**. The trigger SHALL be bounded by `StartAt = Window.OpensAt` and `EndAt = Window.ClosesAt`. The trigger job data SHALL carry `MinEmailInterval`, `AutoCancelEnabled`, and `MaxReconfirmAttempts` so the tick can evaluate them without additional reads. Trigger creation/replacement SHALL happen idempotently in response to:

- The `TicketedEventCreated` integration event (initial creation when a policy is set at creation).
- A `TicketedEventReconfirmPolicyChanged` integration event (published by Registrations when the policy is set, updated, or cleared — `MinEmailInterval`, `AutoCancelEnabled`, and `MaxReconfirmAttempts` are all included in the payload). The trigger SHALL be removed when the policy is cleared.
- A `TicketedEventDetailsChanged` integration event carrying the event's `TimeZone`. The trigger SHALL be replaced atomically with one keyed to the current zone when the projected details update applies.
- The `TicketedEventArchived` integration event (trigger removed).

#### Scenario: Policy added → trigger registered in event time zone
- **WHEN** an event in `Active` status with `TimeZone="Europe/Amsterdam"` receives a new reconfirm policy with `Window=[2025-05-01, 2025-05-25]`, `Cadence=1d`, and `MinEmailInterval=24h`
- **THEN** a Quartz trigger keyed to the event id is registered with start/end at the window bounds and a daily cron evaluated in `Europe/Amsterdam` (so it fires at the same local hour both before and after the spring-forward DST transition)

#### Scenario: Policy added → trigger registered with auto-cancel job data

- **WHEN** an event receives a new reconfirm policy with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`
- **THEN** the Quartz trigger job data carries `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`

#### Scenario: Policy updated with new MaxReconfirmAttempts → trigger job data updated

- **WHEN** an active event's reconfirm policy `MaxReconfirmAttempts` changes from 3 to 5
- **THEN** the trigger job data is updated so the next tick uses `MaxReconfirmAttempts=5`

#### Scenario: AutoCancelEnabled disabled → trigger job data updated

- **WHEN** an organizer sets `AutoCancelEnabled=false` on a policy that previously had it enabled
- **THEN** the trigger job data is updated with `AutoCancelEnabled=false` and the tick no longer auto-cancels

#### Scenario: Policy MinEmailInterval updated → trigger payload updated
- **WHEN** an active event's reconfirm policy `MinEmailInterval` changes from 24h to 48h
- **THEN** the updated policy (including new MinEmailInterval) is stored so the next tick uses the new value; the Quartz trigger schedule is unchanged if only MinEmailInterval changed

#### Scenario: Time zone change in details → trigger replaced
- **WHEN** an active event's details-changed integration event updates the time zone from `Europe/Amsterdam` to `America/Los_Angeles`
- **THEN** the existing trigger is unscheduled and a new trigger with the same cadence cron evaluated in `America/Los_Angeles` is scheduled atomically

#### Scenario: Policy cleared → trigger unregistered
- **WHEN** the reconfirm policy is removed from an active event
- **THEN** the corresponding Quartz trigger is removed and no further reconfirm jobs are created for that event

#### Scenario: Event archived → trigger unregistered
- **WHEN** an event's `TicketedEventArchived` integration event is processed
- **THEN** any reconfirm trigger for that event is removed

#### Scenario: Policy updated → trigger replaced atomically
- **WHEN** an active event's policy cadence changes from 7d to 3d
- **THEN** the existing trigger is unscheduled and a new trigger with the 3d cron is scheduled, with no period during which two triggers exist for the event

---

### Requirement: Reconfirm scheduling uses Email-owned event context

The Email module SHALL use its Email-owned event rendering/scheduling context projection to register, replace, or remove per-event reconfirm Quartz triggers. The projection SHALL be synchronized from Registrations integration events that describe event creation, event archive, event detail changes including time zone, and reconfirm policy changes.

Email SHALL continue to evaluate reconfirm candidates against live Registrations data when a trigger fires.

#### Scenario: Policy change updates projected trigger context

- **WHEN** Registrations publishes a reconfirm-policy-changed integration event with a non-null policy snapshot
- **THEN** Email updates the event context projection and upserts the per-event reconfirm trigger from projected policy and time-zone context

#### Scenario: Time zone change updates scheduling context

- **WHEN** Registrations publishes a details-changed integration event carrying a new time zone for an event with an active reconfirm policy
- **THEN** Email updates the event context projection and replaces the per-event trigger so future ticks use the new IANA time zone

#### Scenario: Candidate selection remains live

- **WHEN** a reconfirm trigger fires
- **THEN** Email queries Registrations for currently registered, unreconfirmed attendees and does not use the event context projection as an attendee source

#### Scenario: Archived event removes trigger

- **WHEN** Registrations publishes an event-archived integration event
- **THEN** Email marks or removes the active scheduling context for that event and removes the corresponding reconfirm trigger

### Requirement: Reconfirm scheduling reconciliation rebuilds from Email context

On worker startup or scheduling reconciliation, Email SHALL rebuild per-event reconfirm triggers from active Email event context projection rows that have an active reconfirm policy. Reconciliation SHALL NOT require a synchronous enumeration of active reconfirm trigger specs from Registrations.

#### Scenario: Worker restart restores trigger from projection

- **WHEN** the worker starts and the Email projection contains an active event with a reconfirm policy
- **THEN** reconciliation registers the corresponding Quartz trigger from the projection row

#### Scenario: Event without policy is ignored

- **WHEN** the worker starts and the Email projection contains an active event without a reconfirm policy
- **THEN** reconciliation does not register a reconfirm trigger for that event

---

### Requirement: Scheduler tick splits candidates into reconfirm-email and auto-cancel sets

When `EvaluateReconfirmJob` fires for an event whose trigger job data has `AutoCancelEnabled=true`, the tick SHALL extend its existing email log query to also return the **total count** of `reconfirm` emails sent to each candidate (not just the most recent timestamp). The tick then partitions the eligible `Registered, HasReconfirmed=false` candidate set (after applying the `MinEmailInterval` filter) into two disjoint sets:

- **Reconfirm set** — candidates where `email_log_count < MaxReconfirmAttempts`: included in the `BulkEmailJob` as before.
- **Auto-cancel set** — candidates where `email_log_count >= MaxReconfirmAttempts`: excluded from the `BulkEmailJob`; the Email module SHALL enqueue a `ReconfirmAutoExpiredIntegrationEvent { TeamId, TicketedEventId, RegistrationIds[] }` on its outbox in the same DB transaction.

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

---

### Requirement: Reconfirm job uses the resolved reconfirm template

The reconfirm `BulkEmailJob` SHALL NOT carry ad-hoc subject/body content; it SHALL rely entirely on the `email-templates` capability with `EmailType=reconfirm`, resolving via the standard event > team > built-in default precedence.

The reconfirm email's primary call-to-action SHALL be a `reconfirm_link` built from the public event link and the recipient's registration id (`{publicEventLink}/reconfirm/{registrationId}`), so the attendee is routed through the Admitto public reconfirm redirect to the event website, which then records the reconfirmation via the public reconfirm endpoint (see `public-event-links`). The `register_link` SHALL NOT be used as the reconfirm CTA.

#### Scenario: Built-in default reconfirm template used when no override exists
- **WHEN** the reconfirm tick fires for an event whose team and event have no `reconfirm` template configured
- **THEN** the built-in default `reconfirm` template is used for every recipient

#### Scenario: Reconfirm CTA targets the reconfirm link
- **WHEN** a reconfirm email is prepared for registration `R1` on event slug `azure-fest-2026`
- **THEN** the primary confirm-attendance link is the `reconfirm_link` ending in `/reconfirm/{R1}`, not the generic `register_link`
