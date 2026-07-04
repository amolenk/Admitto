## ADDED Requirements

### Requirement: TicketedEvent carries an IANA time zone

The `TicketedEvent` aggregate SHALL carry a required `TimeZone` field (IANA zone id, e.g. `Europe/Amsterdam`, `America/Los_Angeles`). The value SHALL be validated against the IANA TZ database at write time. Once persisted, the field MAY be updated by an admin command but the new value SHALL still validate against the IANA database.

The time zone determines the local-clock interpretation of any wall-clock-relative scheduling derived from the event — most notably the cron schedule used to drive reconfirm sending (see `reconfirm-sending`). All other event datetimes (`StartsAt`, `EndsAt`, reconfirm `Window.OpensAt`/`ClosesAt`) continue to be persisted as UTC `DateTimeOffset` values; the `TimeZone` field is the authoritative *display and scheduling* zone for the event, not a reinterpretation of stored instants.

#### Scenario: Create event with time zone
- **WHEN** an organizer posts a creation request including `timeZone: "Europe/Amsterdam"`
- **THEN** the materialised `TicketedEvent` carries `TimeZone="Europe/Amsterdam"`

#### Scenario: Reject creation with unknown time zone
- **WHEN** the creation request carries `timeZone: "Mars/Olympus_Mons"`
- **THEN** Organization (sync) rejects the request with a `400` validation error and `PendingEventCount` is not incremented

#### Scenario: Update event time zone
- **WHEN** an organizer updates the event time zone from `Europe/Amsterdam` to `Europe/London`
- **THEN** the `TicketedEvent.TimeZone` is updated, a `TicketedEventTimeZoneChanged` integration event is outboxed, and any time-zone-dependent scheduling (e.g. the per-event reconfirm cron trigger) is rebuilt against the new zone

#### Scenario: Time zone is required
- **WHEN** a creation request omits `timeZone`
- **THEN** Organization rejects the request with a `400` validation error
