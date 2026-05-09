## MODIFIED Requirements

### Requirement: Admin can create a ticketed event via the UI
The Admin UI SHALL provide a "Create Event" page reachable from the team's events list. The form SHALL collect name, start datetime, and end datetime (no slug field). The form SHALL validate inputs client-side and surface server-side validation errors inline.

Submission SHALL `POST` to the Organization create-event endpoint, which responds `202 Accepted` with a `Location` header pointing to a creation-status URL (see event-management). The UI SHALL then poll that URL until status becomes `Created`, `Rejected`, or `Expired`. While polling, the UI SHALL display a non-blocking spinner and disable the form. On `Created`, the UI SHALL navigate to the new event's settings page (General tab) using the event's UUID. On `Rejected`, the UI SHALL render the rejection reason inline so the user can edit and resubmit. On `Expired`, the UI SHALL render a generic "creation timed out, please try again" error.

#### Scenario: Successfully create an event (async)
- **WHEN** an organizer submits the create event form for name "DevConf 2026", start "2026-06-01T09:00Z", end "2026-06-03T17:00Z" and the backend returns `202 Accepted`, then polling eventually returns status `Created` with the new event's ID
- **THEN** the organizer is redirected to `/teams/{teamId}/events/{eventId}/settings`

#### Scenario: Display client-side validation error on create
- **WHEN** an organizer submits the create event form with an empty name
- **THEN** the form displays an inline validation error on the name field without calling the backend

#### Scenario: Display rejection from polling
- **WHEN** the polling endpoint reports status `Rejected` with a reason
- **THEN** the form is re-enabled and the rejection reason is displayed inline

#### Scenario: Spinner shown while polling
- **WHEN** the backend has returned `202 Accepted` and polling is in progress
- **THEN** the form is disabled and a non-blocking spinner is displayed

#### Scenario: Expired creation displays a timeout error
- **WHEN** polling eventually returns status `Expired`
- **THEN** the form is re-enabled and a "creation timed out, please try again" error is displayed

---

### Requirement: Admin UI exposes event settings through tabbed navigation
The Admin UI SHALL render event settings under `/teams/{teamId}/events/{eventId}/settings` with a side-navigation containing three tabs: **General**, **Registration**, and **Email**. The active tab SHALL be highlighted. Each tab SHALL be an independently routable page.

#### Scenario: Navigate between tabs
- **WHEN** an organizer is on the General tab and clicks the "Registration" tab
- **THEN** the URL changes to `.../settings/registration` and the Registration tab content loads

#### Scenario: Active tab is highlighted
- **WHEN** the Email tab is the current page
- **THEN** the "Email" navigation entry is rendered with the active style

---

### Requirement: General tab manages event metadata
The General tab SHALL show a form pre-filled with the event's name, start datetime, and end datetime. There is no slug field. The form SHALL submit partial updates with the event's current `Version` for optimistic concurrency. On a concurrency conflict the UI SHALL display an error and refetch the latest values.

#### Scenario: Successfully update event name
- **WHEN** an organizer changes the event name and submits
- **THEN** the event metadata is updated and a success message is shown

#### Scenario: Display concurrency conflict
- **WHEN** an organizer submits General-tab changes with a stale `Version`
- **THEN** the UI displays a concurrency conflict error and refetches the current values

## REMOVED Requirements

### Requirement: Slug is read-only on General tab
**Reason**: The `TicketedEvent` aggregate no longer carries a slug field.
**Migration**: Remove the slug read-only field from the General tab form.
