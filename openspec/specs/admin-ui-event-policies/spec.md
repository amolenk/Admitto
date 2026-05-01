# Admin UI Event Policies Specification

## Purpose

Admins manage a ticketed event's registration, cancellation, and reconfirm policies from dedicated Admin UI pages, with optimistic concurrency against the owning `TicketedEvent` aggregate and read-only behaviour when the event is no longer Active.

## Requirements

### Requirement: Admin can manage the registration policy from the UI

The Admin UI SHALL provide a "Registration Policy" page for a ticketed event with a form for the registration window (opens-at and closes-at) and an optional email-domain restriction. The form SHALL be pre-filled with the current policy values. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values. The page SHALL NOT display any "Open registration" / "Close registration" controls or any registration-status toggle — registration openness is derived from the window and the event's status.

When `TicketedEvent.Status` is Cancelled or Archived, the form SHALL be read-only and SHALL display a banner indicating the event is not active.

#### Scenario: Configure the registration window
- **WHEN** an organizer of team "acme" opens the Registration Policy page for event "DevConf" and sets the window to "2025-01-01T00:00Z" / "2025-06-01T00:00Z" and submits
- **THEN** the policy is saved and the UI shows a success message

#### Scenario: Configure an email-domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com" and submits
- **THEN** the policy is saved and self-service registrations for "CorpConf" are restricted to "@acme.com"

#### Scenario: No Open/Close controls on the page
- **WHEN** an organizer views the Registration Policy page for any event
- **THEN** the page displays no "Open registration" or "Close registration" buttons or any registration-status toggle

#### Scenario: Form is read-only for cancelled events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the form fields are disabled and a banner indicates the event is cancelled

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the Registration Policy form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page

---

### Requirement: Admin can manage the additional-detail schema from the registration policy page
The Admin UI SHALL extend the Registration Policy page with an "Additional details" section that lets organizers add, rename, reorder, and remove additional detail fields. Each row SHALL display the field's `Name`, `Key`, and `MaxLength`. Adding a field SHALL auto-generate the `Key` from the `Name` (kebab-case) and SHALL allow the organizer to override it before the field is first persisted; once persisted the `Key` SHALL be read-only.

The form SHALL submit the entire ordered field list together with the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values.

Removing a field SHALL require an explicit confirmation that informs the organizer that historical values for that field will be preserved on existing registrations but will no longer be collected for new registrations.

When `TicketedEvent.Status` is Cancelled or Archived, the editor SHALL be read-only and SHALL display a banner indicating the event is not active.

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

#### Scenario: Editor is read-only for cancelled events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the additional-details rows are read-only and a banner indicates the event is cancelled

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the additional-details form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page

---

### Requirement: Admin can manage the cancellation policy from the UI

The Admin UI SHALL provide a "Cancellation Policy" page for a ticketed event with a form for a single late-cancellation cutoff datetime. The field SHALL be clearable (removing the policy entirely). The form SHALL be pre-filled with the current value or shown empty when no policy is configured. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message.

When `TicketedEvent.Status` is Cancelled or Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the late-cancellation cutoff
- **WHEN** an organizer opens the Cancellation Policy page for event "DevConf" and sets the cutoff to "2025-05-25T00:00Z" and submits
- **THEN** the cancellation policy is saved and the UI shows a success message

#### Scenario: Remove the cancellation policy
- **WHEN** an organizer opens the Cancellation Policy page for event "DevConf" which has cutoff "2025-05-25T00:00Z", clears the field, and submits
- **THEN** the cancellation policy is removed and the page displays no configured policy

#### Scenario: Form is read-only for archived events
- **WHEN** an organizer opens the Cancellation Policy page for event "OldConf" whose `TicketedEvent.Status` is Archived
- **THEN** the form fields are disabled and a banner indicates the event is archived

---

### Requirement: Admin can manage the reconfirm policy from the UI

The Admin UI SHALL provide a "Reconfirmation Policy" page for a ticketed event with a form for the reconfirmation window (opens-at and closes-at) and a cadence expressed in days. The form SHALL support removing the policy entirely. The form SHALL validate client-side that close is after open and cadence is a positive integer ≥ 1. Server-side validation errors SHALL be displayed inline. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message.

When `TicketedEvent.Status` is Cancelled or Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the reconfirm policy
- **WHEN** an organizer sets the reconfirm window to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" and cadence to 7 days and submits
- **THEN** the reconfirm policy is saved and the UI shows a success message

#### Scenario: Remove the reconfirm policy
- **WHEN** an organizer opens the Reconfirmation Policy page for event "DevConf" which has a policy configured and chooses to remove it
- **THEN** the reconfirm policy is removed and the page displays no configured policy

#### Scenario: Validation error — close before open
- **WHEN** an organizer submits a reconfirm policy with close datetime before the open datetime
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive cadence
- **WHEN** an organizer submits a reconfirm policy with cadence 0
- **THEN** the form displays a validation error without calling the backend

---

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

---

### Requirement: Event policy pages are reachable from the sidebar

The Admin UI SHALL expose the Registration Policy, Cancellation Policy, and Reconfirmation Policy pages from the event detail sidebar under a "Policies" section. The event detail header SHALL display the current event status (Active / Cancelled / Archived), read from `TicketedEvent.Status`.

#### Scenario: Navigate to all three policy pages
- **WHEN** an organizer opens the event detail view for event "DevConf"
- **THEN** the sidebar shows a "Policies" section containing links to "Registration", "Cancellation", and "Reconfirmation"

#### Scenario: Event header shows status
- **WHEN** an organizer views event "DevConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the event detail header shows a badge or label indicating the event is Cancelled
