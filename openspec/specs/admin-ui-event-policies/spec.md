# Admin UI Event Policies Specification

## Purpose

Admins manage a ticketed event's registration, cancellation, and reconfirm policies from dedicated Admin UI pages, with optimistic concurrency against the owning `TicketedEvent` aggregate and read-only behaviour when the event is no longer Active.

## Requirements

### Requirement: Admin can manage the registration policy from the UI

The Admin UI SHALL provide a "Registration Policy" page for a ticketed event with a form for the registration window (opens-at and closes-at) and an optional email-domain restriction. The form SHALL be pre-filled with the current policy values. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values. The page SHALL NOT display any "Open registration" / "Close registration" controls or any registration-status toggle — registration openness is derived from the window and the event's status.

When `TicketedEvent.Status` is Archived, the form SHALL be read-only and SHALL display a banner indicating the event is not active.

#### Scenario: Configure the registration window
- **WHEN** an organizer of team "acme" opens the Registration Policy page for event "DevConf" and sets the window to "2025-01-01T00:00Z" / "2025-06-01T00:00Z" and submits
- **THEN** the policy is saved and the UI shows a success message

#### Scenario: Configure an email-domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com" and submits
- **THEN** the policy is saved and self-service registrations for "CorpConf" are restricted to "@acme.com"

#### Scenario: No Open/Close controls on the page
- **WHEN** an organizer views the Registration Policy page for any event
- **THEN** the page displays no "Open registration" or "Close registration" buttons or any registration-status toggle

#### Scenario: Form is read-only for archived events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose `TicketedEvent.Status` is Archived
- **THEN** the form fields are disabled and a banner indicates the event is archived

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the Registration Policy form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page

---

### Requirement: Admin can manage the additional-detail schema from the registration policy page
The Admin UI SHALL extend the Registration Policy page with an "Additional details" section that lets organizers add, rename, reorder, and remove additional detail fields. Each row SHALL display the field's `Name`, `Key`, and `MaxLength`. Adding a field SHALL auto-generate the `Key` from the `Name` (kebab-case) and SHALL allow the organizer to override it before the field is first persisted; once persisted the `Key` SHALL be read-only.

The form SHALL submit the entire ordered field list together with the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values.

Removing a field SHALL require an explicit confirmation that informs the organizer that historical values for that field will be preserved on existing registrations but will no longer be collected for new registrations.

When `TicketedEvent.Status` is Archived, the editor SHALL be read-only and SHALL display a banner indicating the event is not active.

#### Scenario: Add a new additional detail field
- **WHEN** an organizer of team "acme" opens the Registration Policy page for active event "DevConf", adds a field named "Dietary requirements" with maxLength 200, and submits
- **THEN** the schema is saved with a new field whose key is auto-generated as "dietary-requirements"

#### Scenario: Override the auto-generated key before persisting
- **WHEN** an organizer adds a new field named "Dietary requirements" and edits the auto-generated key to "dietary" before submitting
- **THEN** the schema is saved with the field's key as "dietary"

#### Scenario: Reorder fields
- **WHEN** an organizer drags the "T-shirt size" row above the "Dietary requirements" row and submits
- **THEN** the schema is persisted in the new order

#### Scenario: Rename a field without changing its key
- **WHEN** an organizer changes the name of the persisted field with key "dietary" to "Dietary needs" and submits
- **THEN** the schema is saved and the field's key remains "dietary"

#### Scenario: Remove a field requires confirmation
- **WHEN** an organizer clicks the remove button for the field with key "dietary"
- **THEN** the UI shows a confirmation dialog explaining that historical values will be preserved but no longer collected
- **AND** removal proceeds only after the organizer confirms

#### Scenario: Editor is read-only for archived events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose `TicketedEvent.Status` is Archived
- **THEN** the additional-details rows are read-only and a banner indicates the event is archived

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the additional-details form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page

---

### Requirement: Admin can manage the reconfirm policy from the UI

The Admin UI SHALL provide a "Reconfirmation Policy" page for a ticketed event with a form for:
- the reconfirmation window (opens-at and closes-at),
- a cadence expressed in days,
- a **minimum email interval** expressed in hours — the minimum time that must pass since the attendee's last reconfirmation email (or their registration, whichever is more recent) before the system will send them another reconfirmation prompt,
- an **auto-cancel unreconfirmed registrations** toggle (`AutoCancelEnabled`), and
- a **max reconfirmation attempts** number input (positive integer, shown and required only when the auto-cancel toggle is on) — the number of unanswered reconfirmation emails after which a registration is automatically cancelled.

The form SHALL support removing the policy entirely. The form SHALL validate client-side that:
- close is after open,
- cadence is a positive integer ≥ 1,
- minimum email interval is a positive integer ≥ 1, and
- when the auto-cancel toggle is on, max reconfirmation attempts is a positive integer ≥ 1.

The max reconfirmation attempts input SHALL be hidden and its validation SHALL be skipped when the auto-cancel toggle is off.

Server-side validation errors SHALL be displayed inline. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message.

When `TicketedEvent.Status` is Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the reconfirm policy without auto-cancel

- **WHEN** an organizer sets the reconfirm window to "2025-05-01T00:00Z" / "2025-05-25T00:00Z", cadence to 7 days, minimum email interval to 48 hours, and leaves the auto-cancel toggle off, and submits
- **THEN** the reconfirm policy is saved with `AutoCancelEnabled=false` and the UI shows a success message

#### Scenario: Configure the reconfirm policy with auto-cancel enabled

- **WHEN** an organizer sets the reconfirm policy and turns on the auto-cancel toggle and sets max reconfirmation attempts to 3, and submits
- **THEN** the policy is saved with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3` and the UI shows a success message

#### Scenario: Max attempts input is hidden when auto-cancel toggle is off

- **WHEN** an organizer opens the Reconfirmation Policy page and the auto-cancel toggle is off
- **THEN** the max reconfirmation attempts input is not visible on the page

#### Scenario: Max attempts input appears when auto-cancel toggle is turned on

- **WHEN** an organizer toggles the auto-cancel toggle on
- **THEN** the max reconfirmation attempts input becomes visible and is required

#### Scenario: Remove the reconfirm policy
- **WHEN** an organizer opens the Reconfirmation Policy page for event "DevConf" which has a policy configured and chooses to remove it
- **THEN** the reconfirm policy is removed and the page displays no configured policy

#### Scenario: Validation error — close before open
- **WHEN** an organizer submits a reconfirm policy with close datetime before the open datetime
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive cadence
- **WHEN** an organizer submits a reconfirm policy with cadence 0
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive minimum email interval
- **WHEN** an organizer submits a reconfirm policy with minimum email interval 0
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — max attempts required when auto-cancel is on

- **WHEN** an organizer enables the auto-cancel toggle and submits without providing a max reconfirmation attempts value
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive max attempts

- **WHEN** an organizer enables the auto-cancel toggle, sets max reconfirmation attempts to 0, and submits
- **THEN** the form displays a validation error without calling the backend

---

### Requirement: Policy date-time pickers honour the event time zone

All date-time pickers on the event policy pages (registration window open/close, reconfirm window open/close) SHALL interpret and display values in the event's `TimeZone` per the rules in `admin-ui-event-management` (entered local time = wall clock in event TZ; display = UTC instants converted to event TZ; zone caption visible on every input).

Validation rules that compare datetimes (e.g. "close after open") SHALL be performed in the event's zone for user-facing error messages, while the values submitted to the API SHALL still be UTC instants.

#### Scenario: Reconfirm window opens at local 09:00 in event zone
- **WHEN** an event has `TimeZone="Europe/Amsterdam"` and the organizer enters `2025-05-01T09:00` for the reconfirm window opens-at
- **THEN** the API receives the UTC instant for `2025-05-01T09:00 Europe/Amsterdam` (e.g. `2025-05-01T07:00Z`)

#### Scenario: "Close after open" validation message uses event zone
- **WHEN** an organizer enters a registration window with close before open
- **THEN** the inline validation error references the values shown in the event's zone, not the browser's

---

### Requirement: Event policy pages are reachable from the sidebar

The Admin UI SHALL expose the Registration Policy and Reconfirmation Policy pages from the event detail sidebar under a "Policies" section. The event detail header SHALL display the current event status (Active / Archived), read from `TicketedEvent.Status`.

#### Scenario: Navigate to policy pages
- **WHEN** an organizer opens the event detail view for event "DevConf"
- **THEN** the sidebar shows a "Policies" section containing links to "Registration" and "Reconfirmation" (the "Cancellation" link is removed)

#### Scenario: Event header shows status
- **WHEN** an organizer views event "DevConf" whose `TicketedEvent.Status` is Archived
- **THEN** the event detail header shows a badge or label indicating the event is Archived
