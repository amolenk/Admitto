## ADDED Requirements

### Requirement: Admin can create a ticketed event via the UI
The Admin UI SHALL provide a "Create Event" page reachable from the team's events list. The form SHALL collect slug, name, start datetime, and end datetime. The form SHALL validate inputs client-side and surface server-side validation errors inline. On successful creation the UI SHALL navigate to the new event's settings page (General tab).

#### Scenario: Successfully create an event
- **WHEN** an organizer on the team "acme" navigates to the "Create Event" page, fills in slug "devconf-2026", name "DevConf 2026", start "2026-06-01T09:00Z", end "2026-06-03T17:00Z", and submits
- **THEN** the event is created and the organizer is redirected to `/teams/acme/events/devconf-2026/settings`

#### Scenario: Display validation error on create
- **WHEN** an organizer submits the create event form with an empty name
- **THEN** the form displays an inline validation error on the name field without calling the backend

#### Scenario: Display server-side error on create
- **WHEN** an organizer submits the create event form with a slug that already exists for the team
- **THEN** the form displays the server-side error returned by the backend

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
