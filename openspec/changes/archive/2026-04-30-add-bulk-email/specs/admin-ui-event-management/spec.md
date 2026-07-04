## ADDED Requirements

### Requirement: Event create/edit forms include a required time zone

The "Create Event" form and the General tab of the event editor SHALL include a required `TimeZone` selector populated from the IANA zone database (e.g. via a searchable combobox of common zones plus free-text fallback for less common ones). The selected value SHALL be submitted to the create endpoint and to the (new) `PUT /admin/teams/{teamSlug}/events/{eventSlug}/time-zone` update endpoint.

When creating a new event the selector SHALL default to the browser's detected zone (`Intl.DateTimeFormat().resolvedOptions().timeZone`) but the organizer SHALL be required to confirm it explicitly.

#### Scenario: Create form requires time zone
- **WHEN** an organizer opens the Create Event form
- **THEN** the time zone selector defaults to the browser's IANA zone and the form cannot be submitted without an explicit selection

#### Scenario: General tab edits the time zone
- **WHEN** an organizer changes the time zone on the General tab from `Europe/Amsterdam` to `Europe/London` and saves
- **THEN** the UI calls the time-zone update endpoint, on success refreshes the page and displays the new zone alongside event datetimes

#### Scenario: Unknown IANA zone rejected
- **WHEN** the form somehow submits a non-IANA value
- **THEN** the server returns `400` and the UI surfaces the validation error inline

---

### Requirement: Event datetimes are entered and displayed in the event time zone

All date-time pickers on event-scoped admin pages — including the General tab's `StartsAt`/`EndsAt` and the policy pages covered by `admin-ui-event-policies` — SHALL interpret entered local clock values as wall-clock time in the event's `TimeZone` (not the browser's), and SHALL display existing UTC datetimes converted to the event's zone. Each picker SHALL show a small caption with the zone (e.g. "Europe/Amsterdam (UTC+02:00)") so the organizer is never in doubt about which zone the input refers to.

The conversion SHALL be performed client-side using a TZ-aware library (e.g. `date-fns-tz` or `Temporal` if available) — not by relying on `Date.toISOString()`/`new Date(local)`, which interpret in the browser's zone.

Read-only displays of event datetimes (e.g. event list, navigation, dashboard tiles) SHALL similarly format in the event's zone with the zone label visible.

#### Scenario: Picker writes wall-clock time in event zone
- **WHEN** an event has `TimeZone="America/Los_Angeles"` and an organizer enters `2026-06-01T09:00` into the start-date picker from a browser in `Europe/Amsterdam`
- **THEN** the value submitted to the API is the UTC instant corresponding to `2026-06-01T09:00 America/Los_Angeles` (i.e. `2026-06-01T16:00Z`), not the browser's local interpretation

#### Scenario: Picker reads UTC and shows local
- **WHEN** the API returns `StartsAt = 2026-06-01T16:00Z` for an event with `TimeZone="America/Los_Angeles"`
- **THEN** the picker shows `2026-06-01T09:00` regardless of the browser's zone

#### Scenario: Zone label displayed on every picker
- **WHEN** any event-scoped date-time picker is rendered
- **THEN** the picker displays the event's zone caption (e.g. "America/Los_Angeles (UTC-07:00)") below or beside the input
