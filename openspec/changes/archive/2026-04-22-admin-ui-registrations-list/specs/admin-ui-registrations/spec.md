## ADDED Requirements

### Requirement: Admin UI displays a registrations list page for each event
The Admin UI SHALL provide a page at `/teams/{teamSlug}/events/{eventSlug}/registrations` that loads all registrations of the event in a single fetch and renders them in a table.

#### Scenario: SC001 Page loads and shows registrations
- **GIVEN** an organizer is authenticated and viewing an event with registrations
- **WHEN** they navigate to the registrations page
- **THEN** the table displays one row per registration with columns Attendee, Ticket, Status, Reconfirm, Registered

#### Scenario: SC002 Empty event shows an empty-state row
- **GIVEN** an event has no registrations
- **WHEN** the organizer opens the registrations page
- **THEN** the table shows an empty-state message (e.g. "No registrations yet.") instead of rows

#### Scenario: SC003 Attendee column derives display name from email
- **GIVEN** a registration with email `jane.doe@example.com`
- **WHEN** the page renders that row
- **THEN** the Attendee column shows `jane.doe` as the primary line and `jane.doe@example.com` as the secondary line

#### Scenario: SC004 Ticket column shows one badge per ticket
- **GIVEN** a registration that holds two tickets
- **WHEN** the page renders that row
- **THEN** the Ticket column shows two badges with the ticket display names

#### Scenario: SC005 Status column is hardcoded to "Confirmed"
- **WHEN** the page renders any registration row
- **THEN** the Status column shows the badge "Confirmed"

#### Scenario: SC006 Reconfirm column is hardcoded to "—"
- **WHEN** the page renders any registration row
- **THEN** the Reconfirm column shows "—"

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

#### Scenario: SC009 Search filters by email substring
- **GIVEN** the search box contains "jane"
- **WHEN** the table renders
- **THEN** only rows whose email contains "jane" (case-insensitive) are shown

#### Scenario: SC010 Ticket-type filter narrows the rows
- **GIVEN** the ticket-type filter is set to a specific ticket slug
- **WHEN** the table renders
- **THEN** only registrations holding at least one ticket with that slug are shown

#### Scenario: SC011 Default sort is most-recent first
- **WHEN** the page first loads
- **THEN** rows are ordered by Registered timestamp descending

#### Scenario: SC012 Clicking a sortable column header toggles sort direction
- **GIVEN** the Attendee column is currently sorted ascending
- **WHEN** the organizer clicks the Attendee column header
- **THEN** the rows are reordered by Attendee descending

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
