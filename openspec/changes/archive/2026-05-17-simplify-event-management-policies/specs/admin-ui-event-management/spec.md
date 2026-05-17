## REMOVED Requirements

### Requirement: Cancel action is labelled "Cancel ticket type"
**Reason**: The ability to cancel a ticket type is removed. Ticket types remain active until the event is archived. The overflow menu no longer contains a cancel action.
**Migration**: Remove the cancel endpoint and the overflow menu entry. Any client code checking for ticket type `cancellationStatus` must be updated; the field is removed from responses.

---

## MODIFIED Requirements

### Requirement: Ticket types page header shows the event name
The Admin UI Ticket Types page SHALL display the current event's name as the page title (in the same large heading slot previously occupied by "Tickets"). While the event details are loading, the page SHALL fall back to the event slug.

#### Scenario: Header shows event name
- **WHEN** an organizer opens the Ticket Types page for event "devconf-2026" whose name is "DevConf 2026"
- **THEN** the page heading displays "DevConf 2026"

#### Scenario: Header falls back to slug while loading
- **WHEN** the Ticket Types page renders before the event details have loaded
- **THEN** the page heading displays the event slug

---

### Requirement: Ticket type cards expose actions only via the overflow menu
The Admin UI Ticket Types page SHALL expose only an Edit action for a ticket type via the per-card `…` overflow menu. The card SHALL NOT render an inline footer action bar. There is no cancel action.

#### Scenario: No footer action bar
- **WHEN** a ticket type card is rendered
- **THEN** there is no row of inline buttons beneath the stats; the only edit entry point is the `…` overflow menu in the card header

#### Scenario: Overflow menu shows only Edit
- **WHEN** an organizer opens the `…` menu on a ticket type card
- **THEN** the menu contains only the Edit action; there is no cancel option

---

### Requirement: Events list page excludes archived events and reflects archive action immediately
The Admin UI events list page SHALL only display non-archived events (active only — there is no longer a "cancelled" status). When an organizer archives an event via any archive action available in the UI, the archived event SHALL be removed from the events list immediately upon a successful archive response — without requiring a page reload or manual navigation.

#### Scenario: Archived events are not shown on the events list page
- **WHEN** an organizer navigates to the events list page for team "acme" and "conf-2026" (active) and "conf-2025" (archived) exist
- **THEN** "conf-2026" is visible in the list and "conf-2025" is not shown

#### Scenario: Archived event disappears immediately after archive action
- **WHEN** an organizer archives event "conf-2025" from the UI and the archive request succeeds
- **THEN** "conf-2025" is removed from the events list immediately without a full page reload

---

### Requirement: Admin UI exposes event settings through tabbed navigation
The Admin UI SHALL render event settings under `/teams/{teamId}/events/{eventId}/settings` with a side-navigation containing tabs: **General**, **Registration**, **Reconfirmation**, **Email**, **Email templates**, and **Danger zone**. The **Cancellation** tab is removed. The active tab SHALL be highlighted. Each tab SHALL be an independently routable page. The layout shell (breadcrumbs, heading, sidebar nav) SHALL be rendered as a Next.js Server Component so that the team name and event name are fetched server-side and present in the initial HTML.

#### Scenario: Navigate between tabs
- **WHEN** an organizer is on the General tab and clicks the "Registration" tab
- **THEN** the URL changes to `.../settings/registration` and the Registration tab content loads

#### Scenario: Active tab is highlighted
- **WHEN** the Email tab is the current page
- **THEN** the "Email" navigation entry is rendered with the active style

#### Scenario: Team and event names are present on initial render
- **WHEN** an organizer navigates directly to any event settings tab URL or hard-refreshes the page
- **THEN** the breadcrumb shows the team name and event name immediately, without any GUID flash or loading state

#### Scenario: Cancellation tab no longer exists
- **WHEN** an organizer views the event settings sidebar
- **THEN** there is no "Cancellation" entry in the sidebar navigation
