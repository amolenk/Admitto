## MODIFIED Requirements

### Requirement: Email settings page links to a template list sub-page

The event Email Setup tab (`/teams/{teamSlug}/events/{eventSlug}/emails/setup`) SHALL include a "Templates" link or section that navigates to the event-scoped template list at `.../emails/templates`. The team Email settings page (`/teams/{teamSlug}/settings/email`) retains its link to the team-scoped template list at `.../settings/email/templates` (unchanged).

#### Scenario: Templates link visible on event Email Setup tab

- **WHEN** an organizer opens the Email Setup tab for event "devconf-2026"
- **THEN** a "Templates" link or button is visible and clicking it navigates to `/teams/acme/events/devconf-2026/emails/templates`

#### Scenario: Templates link visible on team email settings page

- **WHEN** an organizer opens `/teams/acme/settings/email`
- **THEN** a "Templates" link or button is visible and clicking it navigates to `/teams/acme/settings/email/templates`

---

### Requirement: Template list page enumerates all supported types

The Admin UI SHALL render a template list page at `/teams/{teamSlug}/events/{eventSlug}/emails/templates` (event-scoped) and `/teams/{teamSlug}/settings/email/templates` (team-scoped). Each page shows one row per supported template type (`ticket`, `cancellation`, `visa-letter-denied`, `ticket-types-removed`, `reconfirm`). Each row SHALL display a human-readable label for the type and a status badge indicating "Custom" when a stored custom template exists for that scope or "Default" when the built-in default will be used.

#### Scenario: Rows appear for all supported types

- **WHEN** an organizer opens the event-scoped template list page for event "devconf-2026"
- **THEN** the page shows exactly five rows, one for each supported template type

#### Scenario: Custom badge shown for a type with a stored template

- **WHEN** team "acme" has a custom `ticket` template stored and no custom template for the other types
- **THEN** the `ticket` row shows a "Custom" badge and all other rows show "Default"

#### Scenario: Clicking a row navigates to the event-scoped template detail page

- **WHEN** an organizer clicks the `ticket` row on the event-scoped template list
- **THEN** the browser navigates to `/teams/acme/events/devconf-2026/emails/templates/ticket`

#### Scenario: Old settings/email/templates URL redirects to new path

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/email/templates`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/emails/templates`

---

### Requirement: Template detail page loads the stored custom template or empty defaults

The Admin UI SHALL render a template detail page at `/teams/{teamSlug}/events/{eventSlug}/emails/templates/{type}` (event-scoped) and `/teams/{teamSlug}/settings/email/templates/{type}` (team-scoped). The page fetches the stored custom template via `GET /admin/teams/{teamSlug}/email-templates/{type}` (or its event-scoped equivalent) and displays a form with Subject, Text Body, and HTML Body fields. When the GET returns 404 (no custom template), the form SHALL be rendered empty so the organizer can create one. A "Back to templates" link SHALL be present.

#### Scenario: Event-scoped template detail loads at new URL

- **WHEN** an organizer opens the `ticket` template detail for event "devconf-2026"
- **THEN** the page loads at `/teams/acme/events/devconf-2026/emails/templates/ticket` with the stored template or empty fields

#### Scenario: Old settings template detail URL redirects

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/email/templates/ticket`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/emails/templates/ticket`
