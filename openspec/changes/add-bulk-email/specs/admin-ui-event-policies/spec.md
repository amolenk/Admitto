## ADDED Requirements

### Requirement: Policy date-time pickers honour the event time zone

All date-time pickers on the event policy pages (cancellation cutoff, registration window open/close, reconfirm window open/close) SHALL interpret and display values in the event's `TimeZone` per the rules in `admin-ui-event-management` (entered local time = wall clock in event TZ; display = UTC instants converted to event TZ; zone caption visible on every input).

Validation rules that compare datetimes (e.g. "close after open", "cutoff before event start") SHALL be performed in the event's zone for user-facing error messages, while the values submitted to the API SHALL still be UTC instants.

#### Scenario: Reconfirm window opens at local 09:00 in event zone
- **WHEN** an event has `TimeZone="Europe/Amsterdam"` and the organizer enters `2025-05-01T09:00` for the reconfirm window opens-at
- **THEN** the API receives the UTC instant for `2025-05-01T09:00 Europe/Amsterdam` (e.g. `2025-05-01T07:00Z`)

#### Scenario: Cancellation cutoff displayed in event zone
- **WHEN** the cancellation policy returned by the API has cutoff `2026-05-25T22:00Z` and the event's zone is `America/Los_Angeles`
- **THEN** the picker displays `2026-05-25T15:00` with the zone caption "America/Los_Angeles"

#### Scenario: "Close after open" validation message uses event zone
- **WHEN** an organizer enters a registration window with close before open
- **THEN** the inline validation error references the values shown in the event's zone, not the browser's
