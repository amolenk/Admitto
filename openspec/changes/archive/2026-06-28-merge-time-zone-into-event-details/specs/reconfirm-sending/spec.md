## MODIFIED Requirements

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
