## Purpose

Admins create ticketed events and manage their core metadata, registration policy, ticket catalog, and email settings from the Admin UI through tabbed event-settings pages. Creation is async — the UI submits and polls the Organization creation-status endpoint until the event materialises in Registrations.

## Requirements

### Requirement: Admin can create a ticketed event via the UI
The Admin UI SHALL provide a "Create Event" page reachable from the team's events list. The form SHALL collect slug, name, start datetime, and end datetime. The form SHALL validate inputs client-side and surface server-side validation errors inline.

Submission SHALL `POST` to the Organization create-event endpoint, which responds `202 Accepted` with a `Location` header pointing to a creation-status URL (see event-management). The UI SHALL then poll that URL until status becomes `Created`, `Rejected`, or `Expired`. While polling, the UI SHALL display a non-blocking spinner and disable the form. On `Created`, the UI SHALL navigate to the new event's settings page (General tab). On `Rejected`, the UI SHALL render the rejection reason inline (e.g., "duplicate slug") so the user can edit and resubmit. On `Expired`, the UI SHALL render a generic "creation timed out, please try again" error.

#### Scenario: Successfully create an event (async)
- **WHEN** an organizer on team "acme" submits the create event form for slug "devconf-2026", name "DevConf 2026", start "2026-06-01T09:00Z", end "2026-06-03T17:00Z" and the backend returns `202 Accepted` with a creation-status URL, then polling eventually returns status `Created`
- **THEN** the organizer is redirected to `/teams/acme/events/devconf-2026/settings`

#### Scenario: Display client-side validation error on create
- **WHEN** an organizer submits the create event form with an empty name
- **THEN** the form displays an inline validation error on the name field without calling the backend

#### Scenario: Display rejection from polling
- **WHEN** an organizer submits the create event form with slug "devconf-2026" and the polling endpoint reports status `Rejected` with reason `duplicate_slug`
- **THEN** the form is re-enabled and a "duplicate slug" error is displayed inline against the slug field

#### Scenario: Spinner shown while polling
- **WHEN** the backend has returned `202 Accepted` and polling is in progress
- **THEN** the form is disabled and a non-blocking spinner is displayed

#### Scenario: Expired creation displays a timeout error
- **WHEN** polling eventually returns status `Expired`
- **THEN** the form is re-enabled and a "creation timed out, please try again" error is displayed

---

### Requirement: Admin UI exposes event settings through tabbed navigation
The Admin UI SHALL render event settings under `/teams/{teamSlug}/events/{eventSlug}/settings` with a side-navigation containing three tabs: **General**, **Registration**, and **Email**. The active tab SHALL be highlighted. Each tab SHALL be an independently routable page that loads only the data owned by its module.

#### Scenario: Navigate between tabs
- **WHEN** an organizer is on the General tab and clicks the "Registration" tab
- **THEN** the URL changes to `.../settings/registration` and the Registration tab content loads

#### Scenario: Active tab is highlighted
- **WHEN** the Email tab is the current page
- **THEN** the "Email" navigation entry is rendered with the active style

---

### Requirement: General tab manages event metadata
The General tab SHALL show a form pre-filled with the event's name, start datetime, and end datetime. The slug SHALL be displayed as read-only. The form SHALL submit partial updates with the event's current `Version` for optimistic concurrency. On a concurrency conflict the UI SHALL display an error and refetch the latest values.

#### Scenario: Successfully update event name
- **WHEN** an organizer changes the event name from "DevConf 2026" to "DevConf Europe 2026" and submits
- **THEN** the event metadata is updated and a success message is shown

#### Scenario: Slug is read-only
- **WHEN** an organizer views the General tab for "devconf-2026"
- **THEN** the slug field shows "devconf-2026" and cannot be edited

#### Scenario: Display concurrency conflict
- **WHEN** an organizer submits General-tab changes with a stale `Version`
- **THEN** the UI displays a concurrency conflict error and refetches the current values

---

### Requirement: Registration tab manages registration policy and ticket types
The Registration tab SHALL allow organizers to configure the registration window (open and close datetimes), an optional allowed-email-domain restriction, and the list of ticket types (name, capacity, price). The tab SHALL display the current registration status (Draft, Open, or Closed) and provide explicit "Open for registration" / "Close for registration" actions. Ticket type edits SHALL submit independently with their own concurrency tokens.

#### Scenario: Configure registration window
- **WHEN** an organizer sets the registration window for "devconf-2026" from "2026-01-01T00:00Z" to "2026-05-15T00:00Z" and submits
- **THEN** the window is saved and the form reflects the new values

#### Scenario: Add a ticket type
- **WHEN** an organizer adds a ticket type "Standard" with capacity 200 and submits
- **THEN** the ticket type is created and listed in the Registration tab

#### Scenario: Registration status defaults to Draft for newly created events
- **WHEN** an organizer opens the Registration tab for an event just created via the UI
- **THEN** the status displayed is "Draft" and the "Open for registration" action is visible

---

### Requirement: Open-registration action is gated by email configuration in the UI
The Registration tab SHALL disable the "Open for registration" action and display an inline hint when the backend reports that email is not configured for the event. The hint SHALL link to the Email tab. The disabled state is a UX guard only; the backend remains the source of truth and SHALL also reject the action when email is not configured (see registration-policy spec).

#### Scenario: Open action disabled when email not configured
- **WHEN** an organizer views the Registration tab for an event whose email is not configured
- **THEN** the "Open for registration" button is disabled and a hint links to the Email tab

#### Scenario: Open action enabled when email is configured
- **WHEN** an organizer views the Registration tab for an event whose email is configured
- **THEN** the "Open for registration" button is enabled

#### Scenario: Backend rejection still shown on race
- **WHEN** an organizer clicks "Open for registration" but the backend rejects because email was just unconfigured
- **THEN** the UI displays the server-side error returned by the backend

---

### Requirement: Email tab manages event email server settings
The Email tab SHALL show a form for the event's email settings (SMTP host, port, from-address, authentication mode, and credentials when applicable). Existing secret fields (e.g. password) SHALL be displayed masked, and updating them SHALL require re-entry rather than reusing the masked value. The form SHALL submit with the email-settings `Version` for optimistic concurrency. After a successful save the Email tab SHALL display "Email is configured".

#### Scenario: Configure email settings for the first time
- **WHEN** an organizer fills in SMTP host "smtp.acme.org", port 587, from-address "events@acme.org", username "noreply", password "•••••" and submits
- **THEN** the email settings are saved and the Email tab shows "Email is configured"

#### Scenario: Secret fields are masked on read
- **WHEN** an organizer reopens the Email tab after saving credentials
- **THEN** the password field is rendered masked and empty; the user must re-enter to change it

#### Scenario: Update non-secret fields without re-entering password
- **WHEN** an organizer changes only the from-address and submits without re-entering the password
- **THEN** the from-address is updated and the stored password is unchanged

---


### Requirement: Add ticket type form supports time slots
The Admin UI **Add ticket type** dialog SHALL include a "Time slots" input that lets organizers attach zero or more time-slot slugs to a new ticket type. Each entered token SHALL be validated against the slug format `^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$` before it is accepted as a chip. The form SHALL submit the resulting array as `timeSlots` on the existing `POST /admin/teams/{teamSlug}/events/{eventSlug}/ticket-types` request, sending an empty array (not `null`) when no slots are entered.

#### Scenario: Add a ticket type with two time slots
- **WHEN** an organizer enters slug "vip", name "VIP Pass", and adds the time slots "morning" and "afternoon", then submits
- **THEN** the API receives a request with `timeSlots: ["morning", "afternoon"]` and the new ticket type appears in the list with both time slots

#### Scenario: Add a ticket type with no time slots
- **WHEN** an organizer adds a ticket type without entering any time slot
- **THEN** the API receives a request with `timeSlots: []` and the new ticket type is created without time slots

#### Scenario: Reject invalid time-slot token
- **WHEN** an organizer types "Morning Session!" in the time-slot input and confirms
- **THEN** the token is rejected inline (not added as a chip) with a message indicating the allowed slug format

---

### Requirement: Add ticket type form suggests time slots already used in the event
The "Time slots" input in the Admin UI **Add ticket type** dialog SHALL surface, as selectable suggestions, the deduplicated set of time-slot slugs currently used by other ticket types of the same event (sourced from the loaded `GET …/ticket-types` response). Selecting a suggestion SHALL add it as a chip exactly as if the organizer had typed it. Free-form entry SHALL remain available regardless of suggestions.

#### Scenario: Suggestions are drawn from existing ticket types
- **WHEN** an event has ticket types whose time slots are `["morning"]` and `["morning", "afternoon"]`, and an organizer opens the Add ticket type dialog
- **THEN** the time-slot input offers "morning" and "afternoon" as suggestions

#### Scenario: No suggestions when event has no time slots
- **WHEN** an organizer opens the Add ticket type dialog for an event whose ticket types have no time slots
- **THEN** the time-slot input shows no suggestions but still accepts free-form entry

---

### Requirement: Ticket type listing displays time slots
The Admin UI ticket types page SHALL render each ticket type's time slots as compact badges on the ticket type card. Cards for ticket types without time slots SHALL omit the badge row entirely.

#### Scenario: Card shows time slots
- **WHEN** the ticket types page renders a ticket type whose time slots are `["morning", "afternoon"]`
- **THEN** the card displays both slugs as badges in the card header area

#### Scenario: Card omits the row when no time slots
- **WHEN** the ticket types page renders a ticket type with an empty time slots list
- **THEN** the card does not display a time-slot badge row

---

### Requirement: Edit ticket type dialog shows time slots as read-only
The Admin UI **Edit ticket type** dialog SHALL display the ticket type's existing time slots as disabled chips together with a helper text indicating that time slots cannot be changed after creation. The edit submission SHALL NOT include a `timeSlots` field.

#### Scenario: Time slots are visible but not editable
- **WHEN** an organizer opens the Edit ticket type dialog for a ticket type with time slots `["morning"]`
- **THEN** the dialog shows a disabled "morning" chip and helper text explaining time slots are immutable, and submitting the form sends only the name and capacity fields

#### Scenario: Edit dialog hides the section when no time slots
- **WHEN** an organizer opens the Edit ticket type dialog for a ticket type with no time slots
- **THEN** the time-slot section is omitted
