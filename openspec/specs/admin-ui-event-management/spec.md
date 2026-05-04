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

The Email tab SHALL also surface the relationship between event-scoped and team-scoped settings by querying the team-scoped admin endpoint (`GET /admin/teams/{teamSlug}/email-settings`) in parallel with the event-scoped GET, treating `404` as "no row". The result SHALL drive an inheritance callout displayed alongside the form:

- When team-scoped settings exist AND the event has no event-scoped row, the callout SHALL read "Inherited from team settings" and SHALL link to `/teams/{teamSlug}/settings/email`. The form SHALL render in its empty/unconfigured state and remain editable so the organizer can create an event-scoped override.
- When team-scoped settings exist AND the event has its own event-scoped row, the callout SHALL read "Overriding team settings" and SHALL link to `/teams/{teamSlug}/settings/email`. The form SHALL render pre-filled from the event-scoped row.
- When team-scoped settings do NOT exist, the callout SHALL NOT be displayed (existing behaviour).

The callout is informational only — it SHALL NOT change save or delete behaviour, which continue to operate on the event-scoped row exclusively.

#### Scenario: Configure email settings for the first time

- **WHEN** an organizer fills in SMTP host "smtp.acme.org", port 587, from-address "events@acme.org", username "noreply", password "•••••" and submits
- **THEN** the email settings are saved and the Email tab shows "Email is configured"

#### Scenario: Secret fields are masked on read

- **WHEN** an organizer reopens the Email tab after saving credentials
- **THEN** the password field is rendered masked and empty; the user must re-enter to change it

#### Scenario: Update non-secret fields without re-entering password

- **WHEN** an organizer changes only the from-address and submits without re-entering the password
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Inherited callout shown when only team-scoped settings exist

- **WHEN** an organizer opens the Email tab for event "devconf-2026" (team "acme") and the team-scoped GET returns settings while the event-scoped GET returns `404`
- **THEN** the page shows an "Inherited from team settings" callout linking to `/teams/acme/settings/email`, and the form renders empty

#### Scenario: Overriding callout shown when both scopes exist

- **WHEN** an organizer opens the Email tab and both the team-scoped and event-scoped GETs return settings
- **THEN** the page shows an "Overriding team settings" callout linking to `/teams/acme/settings/email`, and the form is pre-filled from the event-scoped row

#### Scenario: No callout when team-scoped settings do not exist

- **WHEN** an organizer opens the Email tab and the team-scoped GET returns `404` (regardless of whether the event-scoped GET returns `404` or settings)
- **THEN** the page does not show any inheritance callout

#### Scenario: Callout link navigates to team Email page

- **WHEN** an organizer clicks the link inside the inheritance callout on the event Email tab for team "acme"
- **THEN** the browser navigates to `/teams/acme/settings/email`

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
The Admin UI Ticket Types page SHALL expose Edit and Cancel actions for a ticket type only via the per-card `…` overflow menu. The card SHALL NOT render an inline footer action bar containing duplicate Edit / Cancel buttons.

#### Scenario: No footer action bar
- **WHEN** an active (not cancelled) ticket type card is rendered
- **THEN** there is no row of inline Edit / Cancel buttons beneath the stats; the only edit/cancel entry point is the `…` overflow menu in the card header

#### Scenario: Cancelled ticket type still hides actions
- **WHEN** a cancelled ticket type card is rendered
- **THEN** the overflow menu offers no edit or cancel actions (unchanged behaviour) and there is still no footer action bar

---

### Requirement: Ticket type cards omit the slug
The Admin UI Ticket Types page SHALL NOT display the ticket type slug on the card. The slug SHALL remain visible in the Edit ticket type dialog (as the immutable identifier shown there today).

#### Scenario: Card hides slug
- **WHEN** a ticket type card is rendered for ticket type slug "vip"
- **THEN** the card does not show the text "vip" anywhere; the name and (when present) time-slot badges are the only identifying labels in the card header

---

### Requirement: Cancel action is labelled "Cancel ticket type"
The Admin UI Ticket Types page SHALL label the cancel action as "Cancel ticket type" in the `…` overflow menu (replacing any previous label such as "Cancel" or "Cancel sales"). Confirmation dialogs and toasts that surface the action SHALL use the same wording.

#### Scenario: Overflow menu shows "Cancel ticket type"
- **WHEN** an organizer opens the `…` menu on an active ticket type card
- **THEN** the destructive item reads "Cancel ticket type"

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

The "Create Event" form and the General tab of the event editor SHALL include a required `TimeZone` selector populated from the IANA zone database (e.g. via a searchable combobox of common zones plus free-text fallback for less common ones). The selected value SHALL be submitted to the create endpoint and to the (new) `PUT /admin/teams/{teamSlug}/events/{eventSlug}/time-zone` update endpoint.

When creating a new event the selector SHALL default to the browser's detected zone (`Intl.DateTimeFormat().resolvedOptions().timeZone`) but the organizer SHALL be required to confirm it explicitly.

#### Scenario: Create form requires time zone
- **WHEN** an organizer opens the Create Event form
- **THEN** the time zone selector defaults to the browser's IANA zone and the form cannot be submitted without an explicit selection

#### Scenario: General tab edits the time zone
- **WHEN** an organizer changes the time zone on the General tab from `Europe/Amsterdam` to `Europe/London` and saves
- **THEN** the UI calls the time-zone update endpoint, on success refreshes the page and displays the new zone alongside event datetimes

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

### Requirement: Event Email tab exposes a Send-test-email action with a recipient picker

The event Email tab (`/teams/{teamSlug}/events/{eventSlug}/settings/email`) SHALL render a "Send test email" action below the email settings form whenever the event has its own saved email settings. The action SHALL consist of a recipient dropdown and a button. The dropdown SHALL be populated client-side from the team's contact email (read via `GET /api/teams/{teamSlug}`) and the email addresses of the current team members (read via `GET /api/teams/{teamSlug}/members`), with duplicate addresses collapsed and the team's contact email selected by default. The dropdown SHALL NOT include any event-scoped contact list — the recipient set is the same on both scopes.

When the button is clicked, the page SHALL `POST /api/teams/{teamSlug}/events/{eventSlug}/email-settings/test` with the chosen recipient in the body. While the request is in flight, the button SHALL be disabled. On success, the page SHALL render an inline non-destructive `Alert` near the button identifying the recipient ("Test email sent to <address>"). On failure, the page SHALL render an inline destructive `Alert` near the button containing the server's error message. The action SHALL NOT be rendered when the event has no event-scoped settings — even if team-scoped settings exist — because the test endpoint targets only the saved settings of the current scope.

#### Scenario: Action hidden when the event inherits from team
- **GIVEN** team "acme" has team-scoped settings AND event "devconf-2026" has no event-scoped row
- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/settings/email`
- **THEN** the inheritance callout is shown
- **AND** the Send-test-email action is not rendered

#### Scenario: Action shown when event has its own settings
- **GIVEN** event "devconf-2026" has its own event-scoped settings
- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/settings/email`
- **THEN** the Send-test-email action is rendered below the form, alongside the existing Delete action

#### Scenario: Recipient dropdown matches the team page
- **GIVEN** team "acme" has contact email `events@acme.org` and members `alice@example.com`, `bob@example.com`
- **WHEN** an organizer opens the event Email tab for any event owned by "acme"
- **THEN** the recipient dropdown lists exactly the same options as on the team Email page (`events@acme.org`, `alice@example.com`, `bob@example.com`), with `events@acme.org` selected by default

#### Scenario: Successful test
- **GIVEN** recipient `bob@example.com` is selected on the event Email tab
- **WHEN** the organizer clicks "Send test email" and the API responds `200 OK`
- **THEN** the page renders a non-destructive inline alert reading "Test email sent to bob@example.com"

#### Scenario: Failed test surfaces the server error
- **WHEN** the organizer clicks "Send test email" and the API responds with an error containing "Connection refused"
- **THEN** the page renders a destructive inline alert containing "Connection refused"

---

### Requirement: Events list page excludes archived events and reflects archive action immediately
The Admin UI events list page SHALL only display non-archived events (active and
cancelled). When an organizer archives an event via any archive action available
in the UI, the archived event SHALL be removed from the events list immediately
upon a successful archive response — without requiring a page reload or manual
navigation.

#### Scenario: Archived events are not shown on the events list page
- **WHEN** an organizer navigates to the events list page for team "acme" and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are visible in the list and "conf-2025" is not shown

#### Scenario: Archived event disappears immediately after archive action
- **WHEN** an organizer archives event "conf-2025" from the UI and the archive request succeeds
- **THEN** "conf-2025" is removed from the events list immediately without a full page reload
