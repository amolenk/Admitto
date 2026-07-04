## MODIFIED Requirements

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
