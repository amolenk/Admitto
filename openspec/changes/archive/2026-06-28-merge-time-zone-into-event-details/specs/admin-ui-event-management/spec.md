## MODIFIED Requirements

### Requirement: Event create/edit forms include a required time zone

The "Create Event" form and the General tab of the event editor SHALL include a required `TimeZone` selector populated from the IANA zone database (e.g. via a searchable combobox of common zones plus free-text fallback for less common ones). The selected value SHALL be submitted to the create endpoint when creating an event and to the general event details update endpoint when editing an event.

When creating a new event the selector SHALL default to the browser's detected zone (`Intl.DateTimeFormat().resolvedOptions().timeZone`) but the organizer SHALL be required to confirm it explicitly.

#### Scenario: Create form requires time zone
- **WHEN** an organizer opens the Create Event form
- **THEN** the time zone selector defaults to the browser's IANA zone and the form cannot be submitted without an explicit selection

#### Scenario: General tab edits the time zone
- **WHEN** an organizer changes the time zone on the General tab from `Europe/Amsterdam` to `Europe/London` and saves
- **THEN** the UI calls the general event details update endpoint once, on success refreshes the page and displays the new zone alongside event datetimes

#### Scenario: Unknown IANA zone rejected
- **WHEN** the form somehow submits a non-IANA value
- **THEN** the server returns `400` and the UI surfaces the validation error inline
