## MODIFIED Requirements

### Requirement: Admin UI displays a registrations list page for each event
The Admin UI SHALL provide a page at `/teams/{teamSlug}/events/{eventSlug}/registrations` that loads all registrations of the event in a single fetch and renders them in a table.

Each row in the table SHALL include a "Cancel" action that is visible and enabled only when the row's `status` is `Registered`. Activating the action SHALL open a confirmation dialog that:
- Explains the action is irreversible.
- Presents a required reason selector with the three options: `AttendeeRequest`, `VisaLetterDenied`, `TicketTypesRemoved` (human-readable labels).
- Has a "Confirm" button that is disabled until a reason is selected.

On confirmation, the UI SHALL call `POST /admin/…/registrations/{registrationId}/cancel` via `apiClient` with the selected reason. On success, the UI SHALL refresh the registration list so the row's status updates to `Cancelled`. On a 4xx/5xx response, the UI SHALL surface an error notification without navigating away.

#### Scenario: SC001 Page loads and shows registrations
- **WHEN** an organizer is authenticated and views an event with registrations
- **THEN** the table displays one row per registration with columns Attendee, Ticket, Status, Reconfirm, Registered, and Actions

#### Scenario: SC002 Empty event shows an empty-state row
- **GIVEN** an event has no registrations
- **WHEN** the organizer opens the registrations page
- **THEN** the table shows an empty-state message instead of rows

#### Scenario: SC003 Attendee column shows name and email
- **GIVEN** a registration with `firstName="Jane"`, `lastName="Doe"`, `email="jane.doe@example.com"`
- **WHEN** the page renders that row
- **THEN** the Attendee column shows "Jane Doe" as the primary line and "jane.doe@example.com" as the secondary line

#### Scenario: SC004 Ticket column shows one badge per ticket
- **GIVEN** a registration that holds two tickets
- **WHEN** the page renders that row
- **THEN** the Ticket column shows two badges with the ticket display names

#### Scenario: SC005 Status column reflects the registration's status
- **GIVEN** a registration row whose `status` is `Registered`
- **WHEN** the page renders the row
- **THEN** the Status column shows a "Registered" badge styled as success
- **AND** when `status` is `Cancelled` the Status column shows a "Cancelled" badge styled as muted

#### Scenario: SC006 Reconfirm column reflects HasReconfirmed
- **WHEN** the page renders a row whose `hasReconfirmed=true` and `reconfirmedAt="2026-04-20T08:30Z"`
- **THEN** the Reconfirm column shows a checkmark plus the formatted timestamp
- **AND** when `hasReconfirmed=false` the Reconfirm column shows "—"

#### Scenario: SC019 Cancel action is shown only for Registered rows
- **GIVEN** a table with one `Registered` row and one `Cancelled` row
- **WHEN** the page renders
- **THEN** only the `Registered` row has an active "Cancel" action; the `Cancelled` row's action is absent or disabled

#### Scenario: SC020 Cancel confirmation dialog requires a reason before confirming
- **WHEN** an organizer activates the "Cancel" action on a `Registered` row
- **THEN** a confirmation dialog opens with a reason selector and a disabled "Confirm" button
- **AND** the "Confirm" button becomes enabled only after a reason is selected

#### Scenario: SC021 Confirmed cancel with reason updates the row status
- **GIVEN** an organizer selects "AttendeeRequest" in the confirmation dialog and clicks "Confirm"
- **WHEN** the cancel endpoint returns 200
- **THEN** the table refreshes and the row's status changes to `Cancelled`

#### Scenario: SC022 Cancel API error surfaces a notification
- **GIVEN** an organizer confirms cancellation but the cancel endpoint returns a 4xx or 5xx error
- **WHEN** the response arrives
- **THEN** an error notification is shown and the row's status is unchanged
