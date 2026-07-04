## MODIFIED Requirements

### Requirement: Reconfirm sending targets only registered attendees who have not yet reconfirmed

The Email module SHALL drive recurring `reconfirm` emails to attendees of any `TicketedEvent` that has an active `TicketedEventReconfirmPolicy`. Reconfirm sending SHALL operate only when:

1. The event's status is `Active`.
2. `now` falls inside the policy's `Window` (`[OpensAt, ClosesAt]`).
3. The candidate recipient's registration status is `Registered` AND `HasReconfirmed = false`.
4. The later of (the attendee's `CreatedAt` from the registration facade DTO, the `SentAt` of the most recent `reconfirm` email log row for that attendee and event) is more than `policy.MinEmailInterval` hours in the past.

Eligibility SHALL be (re)evaluated against live Registrations data on every tick of the per-event Quartz trigger. The `EvaluateReconfirmJob` uses the existing `IRegistrationsFacade.QueryRegistrationsAsync` call (which already returns `RegistrationListItemDto` per attendee including `CreatedAt`) and additionally queries the Email module's own `email_log` in bulk (one query per tick) for per-attendee last `reconfirm` send times. Once an attendee reconfirms, they fall out of the candidate set on the next tick and receive no further reconfirm prompts.

Each tick of the reconfirm scheduler SHALL create one `BulkEmailJob` per event with an `AttendeeSource(status=Registered, hasReconfirmed=false)`. The job's `EmailType` SHALL be `reconfirm`. The trigger user SHALL be a system-user marker (no real user id).

#### Scenario: Reconfirmed attendees are excluded
- **WHEN** the scheduler ticks for an event with three registered attendees, one of whom has already reconfirmed
- **THEN** the created `BulkEmailJob` resolves to exactly the two who have not reconfirmed

#### Scenario: New registrations between ticks are picked up
- **WHEN** an attendee registers between two scheduled ticks and their registration is older than MinEmailInterval
- **THEN** they are included in the next tick's bulk job (assuming `HasReconfirmed=false`)

#### Scenario: Attendee who reconfirms between ticks is excluded next time
- **WHEN** an attendee was prompted on tick N and reconfirms before tick N+1
- **THEN** they are NOT included in tick N+1's bulk job

#### Scenario: Cron schedule encodes cadence; MinEmailInterval throttles per attendee
- **WHEN** the policy has `Cadence=1d` and `MinEmailInterval=48h`, the window is open, and an attendee received a reconfirm email 24 hours ago
- **THEN** the next daily tick fires but the attendee is excluded because their last email was only 24 hours ago (less than the 48h MinEmailInterval)

#### Scenario: Attendee included once MinEmailInterval has elapsed
- **WHEN** the policy has `MinEmailInterval=48h` and an attendee's last reconfirm email was 49 hours ago
- **THEN** the attendee is included in the next tick's bulk job

#### Scenario: Recently registered attendee excluded until MinEmailInterval elapses
- **WHEN** an attendee registered 2 hours ago and the policy has `MinEmailInterval=24h`, and the window has just opened
- **THEN** the attendee is excluded on this tick because their registration is less than 24 hours old

#### Scenario: Recently registered attendee included after MinEmailInterval
- **WHEN** an attendee registered 25 hours ago and the policy has `MinEmailInterval=24h`, and they have `HasReconfirmed=false`
- **THEN** the attendee is included in the tick's bulk job

#### Scenario: Outside window, no job created
- **WHEN** the scheduler ticks for an event whose `now` is before `OpensAt` or after `ClosesAt`
- **THEN** no `BulkEmailJob` is created

#### Scenario: Archived event, no job created
- **WHEN** the scheduler ticks for an event whose status is `Archived`
- **THEN** no `BulkEmailJob` is created (the trigger is removed when the event is archived)

#### Scenario: Everyone has reconfirmed
- **WHEN** the scheduler ticks for an event with an open window where every registered attendee has `HasReconfirmed=true`
- **THEN** a `BulkEmailJob` is created (for audit) and completes immediately with `RecipientCount=0` and `Status=Completed`

---

### Requirement: A per-event Quartz trigger encodes the reconfirm cadence

For every `TicketedEvent` with an active `TicketedEventReconfirmPolicy` and status `Active`, the Email module SHALL register exactly one Quartz trigger keyed by `TicketedEventId` for the static `EvaluateReconfirmJob`. The trigger SHALL fire on a cron expression derived from the policy `Cadence`, evaluated **in the event's `TimeZone``. The trigger SHALL be bounded by `StartAt = Window.OpensAt` and `EndAt = Window.ClosesAt`. Trigger creation/replacement SHALL happen idempotently in response to:

- The `TicketedEventCreated` integration event (initial creation when a policy is set at creation).
- A `TicketedEventReconfirmPolicyChanged` integration event (published by Registrations when the policy is set, updated, or cleared — the `MinEmailInterval` is included in the payload). The trigger SHALL be removed when the policy is cleared.
- The `TicketedEventTimeZoneChanged` integration event (the trigger SHALL be replaced atomically with one keyed to the new zone).
- The `TicketedEventArchived` integration event (trigger removed). Note: `TicketedEventCancelled` is no longer published.

#### Scenario: Policy added → trigger registered in event time zone
- **WHEN** an event in `Active` status with `TimeZone="Europe/Amsterdam"` receives a new reconfirm policy with `Window=[2025-05-01, 2025-05-25]`, `Cadence=1d`, and `MinEmailInterval=24h`
- **THEN** a Quartz trigger keyed to the event id is registered with start/end at the window bounds and a daily cron evaluated in `Europe/Amsterdam`

#### Scenario: Policy MinEmailInterval updated → trigger payload updated
- **WHEN** an active event's reconfirm policy `MinEmailInterval` changes from 24h to 48h
- **THEN** the updated policy (including new MinEmailInterval) is stored so the next tick uses the new value; the Quartz trigger schedule is unchanged if only MinEmailInterval changed

#### Scenario: Policy cleared → trigger unregistered
- **WHEN** the reconfirm policy is removed from an active event
- **THEN** the corresponding Quartz trigger is removed and no further reconfirm jobs are created for that event

#### Scenario: Event archived → trigger unregistered
- **WHEN** an event's `TicketedEventArchived` integration event is processed
- **THEN** any reconfirm trigger for that event is removed
