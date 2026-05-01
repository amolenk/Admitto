# reconfirm-sending Specification

## Purpose

The Email module drives recurring reconfirm emails to unconfirmed attendees of active events with a configured reconfirm policy. Sending is managed through per-event Quartz triggers that create `BulkEmailJob` records on each tick, integrating with the broader `bulk-email` capability for fan-out and logging.

## Requirements

### Requirement: Reconfirm sending targets only registered attendees who have not yet reconfirmed

The Email module SHALL drive recurring `reconfirm` emails to attendees of any `TicketedEvent` that has an active `TicketedEventReconfirmPolicy`. Reconfirm sending SHALL operate only when:

1. The event's status is `Active`.
2. `now` falls inside the policy's `Window` (`[OpensAt, ClosesAt]`).
3. The candidate recipient's registration status is `Registered` AND `HasReconfirmed = false`.

Eligibility SHALL be (re)evaluated against live Registrations data on every tick of the per-event Quartz trigger by calling `IRegistrationsFacade.QueryRegistrationsAsync(eventId, { Status: Registered, HasReconfirmed: false })`. The cadence is encoded entirely by the cron schedule of the per-event trigger; the resolver SHALL NOT additionally filter candidates against the `email_log` for prior `reconfirm` rows. Once an attendee reconfirms, they fall out of the candidate set on the next tick and receive no further reconfirm prompts.

Each tick of the reconfirm scheduler SHALL create one `BulkEmailJob` per event with an `AttendeeSource(status=Registered, hasReconfirmed=false)`. The job's `EmailType` SHALL be `reconfirm`. The trigger user SHALL be a system-user marker (no real user id).

#### Scenario: Reconfirmed attendees are excluded
- **WHEN** the scheduler ticks for an event with three registered attendees, one of whom has already reconfirmed
- **THEN** the created `BulkEmailJob` resolves to exactly the two who have not reconfirmed

#### Scenario: New registrations between ticks are picked up
- **WHEN** an attendee registers between two scheduled ticks
- **THEN** they are included in the next tick's bulk job (assuming `HasReconfirmed=false`)

#### Scenario: Attendee who reconfirms between ticks is excluded next time
- **WHEN** an attendee was prompted on tick N and reconfirms before tick N+1
- **THEN** they are NOT included in tick N+1's bulk job

#### Scenario: Cron schedule encodes cadence
- **WHEN** the policy `Cadence` is 7d, the trigger window is open, and an unreconfirmed attendee was prompted on the previous tick 7 days ago
- **THEN** the next tick fires (per cron) and the attendee is included again — eligibility is determined entirely by `HasReconfirmed=false`, the 7d gap is enforced by the cron schedule

#### Scenario: Outside window, no job created
- **WHEN** the scheduler ticks for an event whose `now` is before `OpensAt` or after `ClosesAt`
- **THEN** no `BulkEmailJob` is created (the trigger is bounded by the window)

#### Scenario: Cancelled or Archived event, no job created
- **WHEN** the scheduler ticks for an event whose status is `Cancelled` or `Archived`
- **THEN** no `BulkEmailJob` is created (the trigger is removed when the event leaves Active)

#### Scenario: Everyone has reconfirmed
- **WHEN** the scheduler ticks for an event with an open window where every registered attendee has `HasReconfirmed=true`
- **THEN** a `BulkEmailJob` is created (for audit) and completes immediately with `RecipientCount=0` and `Status=Completed`

---

### Requirement: A per-event Quartz trigger encodes the reconfirm cadence

For every `TicketedEvent` with an active `TicketedEventReconfirmPolicy` and status `Active`, the Email module SHALL register exactly one Quartz trigger keyed by `TicketedEventId` for the static `EvaluateReconfirmJob`. The trigger SHALL fire on a cron expression derived from the policy `Cadence`, evaluated **in the event's `TimeZone`** (so e.g. a daily cadence fires at the same local hour year-round, including across DST transitions). The trigger SHALL be bounded by `StartAt = Window.OpensAt` and `EndAt = Window.ClosesAt`. Trigger creation/replacement SHALL happen idempotently in response to:

- The `TicketedEventCreated` integration event (initial creation when a policy is set at creation).
- A new `TicketedEventReconfirmPolicyChanged` integration event (NEW — published by Registrations when the policy is set, updated, or cleared). The trigger SHALL be removed when the policy is cleared.
- The `TicketedEventTimeZoneChanged` integration event (NEW — published by Registrations when the event's time zone is changed). The trigger SHALL be replaced atomically with one keyed to the new zone.
- The `TicketedEventCancelled` and `TicketedEventArchived` integration events (trigger removed).

#### Scenario: Policy added → trigger registered in event time zone
- **WHEN** an event in `Active` status with `TimeZone="Europe/Amsterdam"` receives a new reconfirm policy with `Window=[2025-05-01, 2025-05-25]` and `Cadence=1d`
- **THEN** a Quartz trigger keyed to the event id is registered with start/end at the window bounds and a daily cron evaluated in `Europe/Amsterdam` (so it fires at the same local hour both before and after the spring-forward DST transition)

#### Scenario: Time zone change → trigger replaced
- **WHEN** an active event's time zone changes from `Europe/Amsterdam` to `America/Los_Angeles`
- **THEN** the existing trigger is unscheduled and a new trigger with the same cadence cron evaluated in `America/Los_Angeles` is scheduled atomically

#### Scenario: Policy cleared → trigger unregistered
- **WHEN** the reconfirm policy is removed from an active event
- **THEN** the corresponding Quartz trigger is removed and no further reconfirm jobs are created for that event

#### Scenario: Event cancelled → trigger unregistered
- **WHEN** an event's `TicketedEventCancelled` integration event is processed
- **THEN** any reconfirm trigger for that event is removed

#### Scenario: Policy updated → trigger replaced atomically
- **WHEN** an active event's policy cadence changes from 7d to 3d
- **THEN** the existing trigger is unscheduled and a new trigger with the 3d cron is scheduled, with no period during which two triggers exist for the event

---

### Requirement: Reconfirm job uses the resolved reconfirm template

The reconfirm `BulkEmailJob` SHALL NOT carry ad-hoc subject/body content; it SHALL rely entirely on the `email-templates` capability with `EmailType=reconfirm`, resolving via the standard event > team > built-in default precedence.

#### Scenario: Built-in default reconfirm template used when no override exists
- **WHEN** the reconfirm tick fires for an event whose team and event have no `reconfirm` template configured
- **THEN** the built-in default `reconfirm` template is used for every recipient
