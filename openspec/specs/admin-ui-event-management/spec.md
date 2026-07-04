## Purpose

Team owners create ticketed events, and organizers manage their core metadata, registration policy, and ticket catalog from the Admin UI through tabbed event-settings pages. Creation is async — the UI submits and polls the Organization creation-status endpoint until the event materialises in Registrations.

## Requirements

### Requirement: Admin UI event dashboard hero card
The event hero card SHALL display the event name, dates, time, timezone, website URL (if set), status badge, and countdown badge. The hero card SHALL NOT include a "Copy link" or any other shortcut action button in the top-right corner.

#### Scenario: Hero card shows event metadata without action buttons
- **WHEN** an organizer views the event dashboard
- **THEN** the hero card shows the event name, date, time, website URL (if set), status badge, and countdown, and no copy or share button is visible

---

### Requirement: Admin UI event dashboard check-in card
The check-in card SHALL display check-in timing information, a QR scanner button, and summary statistics (checked-in count, expected count, completion percentage). The check-in card SHALL NOT include a "Share link" or any copy-shortcut button.

#### Scenario: Check-in card shows scanner button without share link
- **WHEN** an organizer views the event dashboard check-in card
- **THEN** the card shows the QR Scanner button and check-in stats, but no "Share link" button is present

---

### Requirement: Team owner can create a ticketed event via the UI
The Admin UI SHALL provide a "Create Event" page reachable from the team's events list for team owners. The form SHALL collect name, public slug, start datetime, and end datetime. The form SHALL validate inputs client-side and surface server-side validation errors inline.

Submission SHALL `POST` to the Organization create-event endpoint, which responds `202 Accepted` with a `Location` header pointing to a creation-status URL (see event-management). The UI SHALL then poll that URL until status becomes `Created`, `Rejected`, or `Expired`. While polling, the UI SHALL display a non-blocking spinner and disable the form. On `Created`, the UI SHALL navigate to the new event's Edit Event page (General tab) using the event's UUID. On `Rejected`, the UI SHALL render the rejection reason inline so the user can edit and resubmit. On `Expired`, the UI SHALL render a generic "creation timed out, please try again" error.

#### Scenario: Successfully create an event (async)
- **WHEN** a team owner submits the create event form for name "DevConf 2026", public slug `devconf-2026`, start "2026-06-01T09:00Z", end "2026-06-03T17:00Z" and the backend returns `202 Accepted`, then polling eventually returns status `Created` with the new event's ID
- **THEN** the organizer is redirected to `/teams/{teamId}/events/{eventId}/edit/general`

#### Scenario: Duplicate public slug rejection is shown
- **WHEN** the backend rejects a submitted public slug because it is already in use
- **THEN** the UI shows the duplicate-slug error to the organizer and does not report the save as successful

#### Scenario: Display client-side validation error on create
- **WHEN** a team owner submits the create event form with an empty name
- **THEN** the form displays an inline validation error on the name field without calling the backend

#### Scenario: Create event option is hidden for non-owners
- **WHEN** an Organizer or Crew member views a team's events list in the sidebar
- **THEN** the "New event" option is not shown

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

### Requirement: Admin UI exposes event settings through a tabbed Edit Event page

The Admin UI SHALL provide a tabbed **Edit Event** page at `/teams/{teamId}/events/{eventId}/edit` accessible from the event sidebar as the second item after Dashboard. The page SHALL have three tabs implemented as independently routable sub-pages:

- **General** at `/teams/{teamId}/events/{eventId}/edit/general` — general event details (name, dates, etc.)
- **Policies** at `/teams/{teamId}/events/{eventId}/edit/policies` — registration policy, additional detail fields, and reconfirmation policy on a single scrollable page
- **Danger zone** at `/teams/{teamId}/events/{eventId}/edit/danger` — destructive actions

The bare `/edit` path SHALL redirect to `/edit/general`. The active tab SHALL be visually highlighted. There is no shared settings sub-nav or sub-layout; the tab bar is part of the Edit Event page layout itself.

The old `settings/*` URL patterns SHALL permanently redirect (HTTP 308) to their corresponding new paths.

After a successful event creation the UI SHALL navigate to `/teams/{teamId}/events/{eventId}/edit/general` (was `settings`).

#### Scenario: Edit Event page is accessible from the sidebar

- **WHEN** an organizer clicks "Edit Event" in the event sidebar
- **THEN** the browser navigates to `/teams/{teamId}/events/{eventId}/edit/general` and the General tab content is displayed

#### Scenario: Switching to Policies tab

- **WHEN** an organizer clicks the "Policies" tab on the Edit Event page
- **THEN** the URL changes to `.../edit/policies` and a single page shows both the registration policy form and the reconfirmation policy form

#### Scenario: Switching to Danger zone tab

- **WHEN** an organizer clicks the "Danger zone" tab
- **THEN** the URL changes to `.../edit/danger` and the danger zone actions are shown

#### Scenario: Old settings URL redirects to General tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/general`

#### Scenario: Old settings/registration URL redirects to Policies tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/registration`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/policies`

#### Scenario: Old settings/reconfirm URL redirects to Policies tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/reconfirm`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/policies`

#### Scenario: Post-creation redirect lands on General tab

- **WHEN** a team owner completes the create-event flow and the event is successfully created
- **THEN** the UI navigates to `/teams/{teamId}/events/{eventId}/edit/general`

---

### Requirement: General tab manages event metadata
The General tab SHALL show a form pre-filled with the event's name, public slug, start datetime, and end datetime. The public slug field SHALL be required and SHALL use the backend slug validation rules. The form SHALL submit partial updates with the event's current `Version` for optimistic concurrency. On a concurrency conflict the UI SHALL display an error and refetch the latest values.

#### Scenario: Successfully update event name
- **WHEN** an organizer changes the event name and submits
- **THEN** the event metadata is updated and a success message is shown

#### Scenario: Edit form shows current public slug
- **WHEN** an organizer opens the event General page for an event whose public slug is `azure-fest-2026`
- **THEN** the public slug field is pre-filled with `azure-fest-2026`

#### Scenario: Display concurrency conflict
- **WHEN** an organizer submits General-tab changes with a stale `Version`
- **THEN** the UI displays a concurrency conflict error and refetches the current values

---

### Requirement: Admin UI can apply team accent color as a scoped visual accent

When team detail data includes an accent color, the Admin UI MAY expose it through a scoped CSS variable for selected-team UI affordances. This SHALL be limited to small accents and SHALL NOT require a full design-system retheme.

#### Scenario: Selected team accent variable is available
- **WHEN** the dashboard renders for selected team "acme" with accent color `#0f766e`
- **THEN** team-scoped UI can read a CSS variable or equivalent value containing `#0f766e`

---

### Requirement: Registration tab manages registration policy and ticket types
The Registration tab SHALL allow organizers to configure the registration window (open and close datetimes), an optional allowed-email-domain restriction, and the list of ticket types (name, capacity, price). The tab SHALL display the current registration status (Draft, Open, or Closed) and provide explicit "Open for registration" / "Close for registration" actions. Ticket type edits SHALL submit independently with their own concurrency tokens.

The ticket type add and edit forms SHALL include:
- An **"Enable self-service registration"** checkbox (default: checked). When unchecked, the ticket type is only accessible via admin registration or coupon.
- A **"Limit capacity"** checkbox. When unchecked, the capacity is unlimited (null). When checked, a positive integer capacity input is revealed. This replaces the plain optional capacity number input, fixing the inability to clear a capacity once set.

The ticket type list row SHALL display a visual indicator (e.g., a badge or icon) showing whether self-service is enabled or disabled for each ticket type.

#### Scenario: Configure registration window
- **WHEN** an organizer sets the registration window for "devconf-2026" from "2026-01-01T00:00Z" to "2026-05-15T00:00Z" and submits
- **THEN** the window is saved and the form reflects the new values

#### Scenario: Add a ticket type
- **WHEN** an organizer adds a ticket type "Standard" with capacity 200 and submits
- **THEN** the ticket type is created and listed in the Registration tab

#### Scenario: Registration status defaults to Draft for newly created events
- **WHEN** an organizer opens the Registration tab for an event just created via the UI
- **THEN** the status displayed is "Draft" and the "Open for registration" action is visible

#### Scenario: Add ticket type with self-service enabled and capacity limit
- **WHEN** an organizer checks "Enable self-service registration", checks "Limit capacity", enters 200, and submits
- **THEN** the ticket type is created with `selfServiceEnabled: true` and `maxCapacity: 200`

#### Scenario: Add ticket type with self-service disabled
- **WHEN** an organizer unchecks "Enable self-service registration" and submits
- **THEN** the ticket type is created with `selfServiceEnabled: false`

#### Scenario: Add ticket type with unlimited self-service capacity
- **WHEN** an organizer checks "Enable self-service registration", leaves "Limit capacity" unchecked, and submits
- **THEN** the ticket type is created with `selfServiceEnabled: true` and `maxCapacity: null`

#### Scenario: Remove capacity limit on existing ticket type
- **WHEN** an organizer edits a ticket type that has a capacity of 200, unchecks "Limit capacity", and saves
- **THEN** the ticket type is updated with `maxCapacity: null` (unlimited)

#### Scenario: Self-service indicator shown in ticket type list
- **WHEN** an organizer views the Registration tab with ticket types "general" (selfServiceEnabled: true) and "vip" (selfServiceEnabled: false)
- **THEN** each row shows a distinct visual indicator for self-service status

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

---

### Requirement: Ticket types page header shows the event name
The Admin UI Ticket Types page SHALL display the current event's name as the page title (in the same large heading slot previously occupied by "Tickets"). While the event details are loading, the page SHALL fall back to the event slug.

#### Scenario: Header shows event name
- **WHEN** an organizer opens the Ticket Types page for event "devconf-2026" whose name is "DevConf 2026"
- **THEN** the page heading displays "DevConf 2026"

#### Scenario: Header falls back to slug while loading
- **WHEN** the Ticket Types page renders before the event details have loaded
- **THEN** the page heading displays the event slug

---

### Requirement: Ticket types page uses "registered" wording for free-event ticketing
The Admin UI Ticket Types page SHALL use the verb "registered" (and its noun form "Registered") in place of "sold"/"Sold" everywhere on the page. This applies to:

- The header summary line ("N registered of M across K ticket types").
- The per-card stat label ("Registered" instead of "Sold").
- Any percentage or sub-label associated with capacity ("X% registered").

#### Scenario: Header summary uses "registered"
- **WHEN** the Ticket Types page renders with totals 12 registered out of 100 across 3 ticket types
- **THEN** the summary line reads "12 registered of 100 across 3 ticket types"

#### Scenario: Card stat label uses "Registered"
- **WHEN** any ticket type card is rendered
- **THEN** the leftmost stat in the three-column block is labelled "Registered" (not "Sold")

---

### Requirement: Available ticket types use "Available" badge text
The Admin UI Ticket Types page SHALL render the in-sale status badge with the text "Available" instead of "On sale". The visual styling and the conditions for showing it (active and not at capacity) SHALL remain unchanged.

#### Scenario: Active, in-stock ticket type shows "Available"
- **WHEN** a card renders an active ticket type with remaining capacity
- **THEN** the status badge text reads "Available"

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

### Requirement: Ticket type cards omit the slug
The Admin UI Ticket Types page SHALL NOT display the ticket type slug on the card. The slug SHALL remain visible in the Edit ticket type dialog (as the immutable identifier shown there today).

#### Scenario: Card hides slug
- **WHEN** a ticket type card is rendered for ticket type slug "vip"
- **THEN** the card does not show the text "vip" anywhere; the name and (when present) time-slot badges are the only identifying labels in the card header

---

### Requirement: Ticket type cards have a subtle ticket-stub appearance
The Admin UI Ticket Types page SHALL style ticket type cards with (a) a noticeably rounded outer border-radius and (b) a single horizontal perforated/dashed divider with rounded notches on the left and right edges separating the card header (name + status badge) from the stats region, evoking a tear-off ticket stub. The treatment SHALL be implemented with CSS only (no SVG/illustration assets) and SHALL NOT change the card's content layout or grid placement.

#### Scenario: Card shows perforated divider
- **WHEN** a ticket type card is rendered
- **THEN** a dashed/perforated horizontal line with rounded edge notches is visible inside the card, separating the header (name + badge) from the stats region

#### Scenario: No layout shift versus prior card
- **WHEN** comparing the new card to the prior card at the same viewport width
- **THEN** the grid columns, card width, and stat block remain unchanged; only the border-radius, vertical padding, and divider treatment differ

---

### Requirement: Event create/edit forms include a required time zone

The "Create Event" form and the General tab of the event editor SHALL include a required `TimeZone` selector populated from the IANA zone database (e.g. via a searchable combobox of common zones plus free-text fallback for less common ones). The selected value SHALL be submitted to the create endpoint when creating an event and to the general event details update endpoint when editing an event.

When creating a new event the selector SHALL default to the browser's detected zone (`Intl.DateTimeFormat().resolvedOptions().timeZone`) but the organizer SHALL be required to confirm it explicitly.

#### Scenario: Create form requires time zone
- **WHEN** an organizer opens the Create Event form
- **THEN** the time zone selector defaults to the browser's IANA zone and the form cannot be submitted without an explicit selection

#### Scenario: General tab edits the time zone
- **WHEN** an organizer changes the time zone on the General tab from `Europe/Amsterdam` to `Europe/London` and saves
- **THEN** the UI calls the general event details update endpoint once, on success refreshes the page and displays the new zone alongside event datetimes

#### Scenario: Unknown IANA zone rejected
- **WHEN** the form somehow submits a non-IANA value
- **THEN** the server returns `400` and the UI surfaces the validation error inline

---

### Requirement: Event datetimes are entered and displayed in the event time zone

All date-time pickers on event-scoped admin pages — including the General tab's `StartsAt`/`EndsAt` and the policy pages covered by `admin-ui-event-policies` — SHALL interpret entered local clock values as wall-clock time in the event's `TimeZone` (not the browser's), and SHALL display existing UTC datetimes converted to the event's zone. Each picker SHALL show a small caption with the zone (e.g. "Europe/Amsterdam (UTC+02:00)") so the organizer is never in doubt about which zone the input refers to.

The conversion SHALL be performed client-side using a TZ-aware library (e.g. `date-fns-tz` or `Temporal` if available) — not by relying on `Date.toISOString()`/`new Date(local)`, which interpret in the browser's zone.

Read-only displays of event datetimes (e.g. event list, navigation, dashboard tiles) SHALL similarly format in the event's zone with the zone label visible.

#### Scenario: Picker writes wall-clock time in event zone
- **WHEN** an event has `TimeZone="America/Los_Angeles"` and an organizer enters `2026-06-01T09:00` into the start-date picker from a browser in `Europe/Amsterdam`
- **THEN** the value submitted to the API is the UTC instant corresponding to `2026-06-01T09:00 America/Los_Angeles` (i.e. `2026-06-01T16:00Z`), not the browser's local interpretation

#### Scenario: Picker reads UTC and shows local
- **WHEN** the API returns `StartsAt = 2026-06-01T16:00Z` for an event with `TimeZone="America/Los_Angeles"`
- **THEN** the picker shows `2026-06-01T09:00` regardless of the browser's zone

#### Scenario: Zone label displayed on every picker
- **WHEN** any event-scoped date-time picker is rendered
- **THEN** the picker displays the event's zone caption (e.g. "America/Los_Angeles (UTC-07:00)") below or beside the input

---

### Requirement: Event main sidebar includes a dedicated Bulk Emails entry

The Admin UI event sidebar (the persistent side-navigation shown when an organizer is viewing any page under `/teams/{teamSlug}/events/{eventSlug}/`) SHALL include an "Emails" entry that links to the Bulk Emails list page at `/teams/{teamSlug}/events/{eventSlug}/emails`. This entry SHALL be active when the current path is `/emails` or starts with `/emails/`. It SHALL NOT be active when the organizer is on event edit pages.

#### Scenario: Emails sidebar entry links to bulk emails list

- **WHEN** an organizer clicks the "Emails" entry in the event sidebar for event "devconf-2026"
- **THEN** the browser navigates to `/teams/acme/events/devconf-2026/emails`

#### Scenario: Emails entry is active on the bulk emails list page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/emails`
- **THEN** the "Emails" sidebar entry is rendered with the active style

#### Scenario: Emails entry is NOT active on the event edit page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/edit/general`
- **THEN** the "Emails" sidebar entry is NOT rendered as active; instead the "Edit Event" entry is active

#### Scenario: Emails entry is active on the bulk email detail page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/emails/some-job-id`
- **THEN** the "Emails" sidebar entry is rendered with the active style

---

### Requirement: Events list page excludes archived events and reflects archive action immediately
The Admin UI events list page SHALL only display non-archived events (active only — there is no longer a "cancelled" status). When an organizer archives an event via any archive action available in the UI, the archived event SHALL be removed from the events list immediately upon a successful archive response — without requiring a page reload or manual navigation.

#### Scenario: Archived events are not shown on the events list page
- **WHEN** an organizer navigates to the events list page for team "acme" and "conf-2026" (active) and "conf-2025" (archived) exist
- **THEN** "conf-2026" is visible in the list and "conf-2025" is not shown

#### Scenario: Archived event disappears immediately after archive action
- **WHEN** an organizer archives event "conf-2025" from the UI and the archive request succeeds
- **THEN** "conf-2025" is removed from the events list immediately without a full page reload
