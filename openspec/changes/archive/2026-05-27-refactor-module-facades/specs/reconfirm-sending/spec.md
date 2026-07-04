## MODIFIED Requirements

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
