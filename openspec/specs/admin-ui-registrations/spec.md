# Admin UI Registrations Specification

## Purpose

The Admin UI lets team admins manage registrations for a ticketed event. This capability covers the "Add registration" affordance on the event's registrations area, allowing an admin to submit a single new registration directly through the UI, backed by the admin-add registration endpoint.

## Requirements

### Requirement: Admin UI surfaces an "Add registration" affordance on the event registrations area
The Admin UI SHALL provide an "Add registration" affordance on the event's registrations area, reachable from the existing event navigation for a team's event. Activating it SHALL open a form (dialog or page) that lets a team admin submit a single new registration for that event.

The form SHALL collect:
- Attendee `firstName` and `lastName` (both required, validated client-side as non-empty)
- Attendee email (required, validated client-side as a well-formed email)
- One or more ticket-type selections, populated from the event's current ticket catalog (required, at least one selection)
- Additional details, rendered from the event's `AdditionalDetailSchema` using the same field types and length limits the public registration form already uses (optional)

Submission SHALL `POST` to the admin-add registration endpoint via the Admin UI's `apiClient` (not the generated OpenAPI SDK), consistent with other Admin UI features.

On success, the UI SHALL close the form, refresh the registrations list so the new registration appears, and surface a brief confirmation. On a 4xx response, the UI SHALL keep the form open and render the server's validation errors inline against the relevant fields, including the duplicate-email and "event not active" cases.

The visual design SHALL match existing admin features in look-and-feel (form controls, spacing, validation messaging) per the repo's design conventions.

#### Scenario: Successfully add a registration via the Admin UI
- **WHEN** an admin on team "acme" opens the registrations area for event "devconf", clicks "Add registration", fills in `firstName="Speaker"`, `lastName="One"`, `email="speaker@example.com"`, selects ticket type "Speaker Pass", and submits
- **THEN** the UI calls the admin-add endpoint with all four fields, the form closes on success, and the registrations list shows "Speaker One" / "speaker@example.com" with ticket "Speaker Pass"

#### Scenario: Display client-side validation error on add — missing first name
- **WHEN** an admin submits the add-registration form with an empty `firstName`
- **THEN** the form displays an inline validation error on the first-name field and does not call the backend

#### Scenario: Display client-side validation error on add — missing last name
- **WHEN** an admin submits the add-registration form with an empty `lastName`
- **THEN** the form displays an inline validation error on the last-name field and does not call the backend

#### Scenario: Display client-side validation error on add — missing email
- **WHEN** an admin submits the add-registration form with an empty email
- **THEN** the form displays an inline validation error on the email field and does not call the backend

#### Scenario: Display server validation error — duplicate email
- **WHEN** an admin submits the add-registration form for an email that is already registered for the event and the backend responds with the "already registered" error
- **THEN** the form remains open and the duplicate-registration error is rendered inline against the email field

#### Scenario: Display server validation error — event not active
- **WHEN** an admin submits the add-registration form and the backend responds with the "event not active" error
- **THEN** the form remains open and the error is surfaced to the user

#### Scenario: Ticket selection sourced from the event's ticket catalog
- **WHEN** an admin opens the add-registration form for event "devconf"
- **THEN** the ticket-type selector lists the active ticket types currently configured on that event (cancelled ticket types are not selectable)

#### Scenario: Additional details rendered from the event schema
- **WHEN** an admin opens the add-registration form for an event whose `AdditionalDetailSchema` declares `dietary` (maxLength 200) and `tshirt` (maxLength 5)
- **THEN** the form renders inputs for `dietary` and `tshirt` with the appropriate length limits, and submitting valid values stores them on the new registration

### Requirement: Admin UI displays a registrations list page for each event
The Admin UI SHALL provide a page at `/teams/{teamSlug}/events/{eventSlug}/registrations` that loads all registrations of the event in a single fetch and renders them in a table.

The table SHALL NOT include a per-row "Cancel" action. Cancel functionality has moved to the attendee detail page.

#### Scenario: SC001 Page loads and shows registrations
- **GIVEN** an organizer is authenticated and viewing an event with registrations
- **WHEN** they navigate to the registrations page
- **THEN** the table displays one row per registration with columns Attendee, Ticket, Status, Reconfirm, Registered, and Actions

#### Scenario: SC002 Empty event shows an empty-state row
- **GIVEN** an event has no registrations
- **WHEN** the organizer opens the registrations page
- **THEN** the table shows an empty-state message (e.g. "No registrations yet.") instead of rows

#### Scenario: SC003 Attendee column shows the registration's first and last name
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
- **THEN** the Reconfirm column shows a checkmark plus the formatted timestamp (event-zone aware per `admin-ui-event-management`)
- **AND** when `hasReconfirmed=false` the Reconfirm column shows "—"

### Requirement: Registrations page summarises totals against capacity
The page SHALL display a single summary tile showing the total registration count, optionally followed by the event capacity.

#### Scenario: SC007 Summary tile shows total only when capacity is unset
- **GIVEN** the event has no ticket type with a `MaxCapacity`
- **WHEN** the page renders
- **THEN** the summary tile shows `Total: <N>`

#### Scenario: SC008 Summary tile shows total of capacity when capacity exists
- **GIVEN** the event's ticket types sum to a non-zero `MaxCapacity` of 250
- **WHEN** the page renders with 47 registrations
- **THEN** the summary tile shows `Total: 47 of 250`

### Requirement: Registrations page supports client-side search, filter, sort, and paging
The page SHALL apply search, ticket-type filter, column sort, and pagination entirely on the client without re-fetching from the server.

#### Scenario: SC009 Search filters across name and email
- **GIVEN** the search box contains "jane"
- **WHEN** the table renders
- **THEN** only rows whose `firstName`, `lastName`, or `email` contain "jane" (case-insensitive substring match) are shown

#### Scenario: SC010 Ticket-type filter narrows the rows
- **GIVEN** the ticket-type filter is set to a specific ticket slug
- **WHEN** the table renders
- **THEN** only registrations holding at least one ticket with that slug are shown

#### Scenario: SC011 Default sort is by attendee name ascending
- **WHEN** the page first loads
- **THEN** rows are ordered by `lastName` ascending, tie-broken by `firstName` ascending
- **AND** clicking the Registered column header re-sorts by registered timestamp descending

#### Scenario: SC012 Clicking a sortable column header toggles sort direction
- **GIVEN** the Attendee column is currently sorted ascending
- **WHEN** the organizer clicks the Attendee column header
- **THEN** the rows are reordered by `lastName` descending (then `firstName` descending)

#### Scenario: SC013 Pagination shows 25 rows per page
- **GIVEN** the filtered list contains 60 rows
- **WHEN** the page first renders
- **THEN** the table shows the first 25 rows and the footer reports "Showing 1–25 of 60"

#### Scenario: SC014 Next page advances the visible window
- **GIVEN** the filtered list contains 60 rows and the user is on page 1
- **WHEN** the user clicks "Next"
- **THEN** the table shows rows 26–50

### Requirement: Registrations page exposes Add registration and Export CSV affordances
The page SHALL provide a primary "Add registration" button that navigates to the existing add-registration page, and a secondary "Export CSV" button that surfaces a "Coming soon" notification when clicked.

#### Scenario: SC015 Add registration navigates to the add page
- **WHEN** the organizer clicks "Add registration"
- **THEN** the browser navigates to `/teams/{teamSlug}/events/{eventSlug}/registrations/add`

#### Scenario: SC016 Export CSV shows a Coming soon notification
- **WHEN** the organizer clicks "Export CSV"
- **THEN** a notification is shown that informs them the export is not yet available
- **AND** no file is downloaded

### Requirement: Registrations page hides features that are out of scope for this slice
The page SHALL NOT show row-level multi-select checkboxes, bulk-action toolbars, status tabs (All / Confirmed / Pending / Cancelled), or columns for Company or other additional-detail fields.

#### Scenario: SC017 No multi-select checkbox column
- **WHEN** the page renders
- **THEN** the table contains no checkbox column and no "X selected" toolbar

#### Scenario: SC018 No status tabs above the table
- **WHEN** the page renders
- **THEN** there is no Confirmed/Pending/Cancelled tab strip

### Requirement: Admin UI registrations table rows link to the attendee detail page

Each row in the registrations table SHALL be clickable and navigate the admin to the attendee detail page at `/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}`. The attendee name or email SHALL serve as the primary clickable element; the full row MAY additionally support click-through navigation.

#### Scenario: SC023 Clicking a registration row navigates to the attendee detail page

- **GIVEN** an organizer is on the registrations list page and the table has at least one row
- **WHEN** they click on the attendee name in a row
- **THEN** the browser navigates to the attendee detail page for that registration's ID
