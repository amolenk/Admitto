## Purpose

Organizers manage team-scoped SMTP email settings from the Admin UI through a dedicated Email page in the team settings area. Team-scoped settings act as a default that event-scoped settings can override.

## Requirements

### Requirement: Team settings sidebar exposes an Email entry

The Admin UI team-settings layout SHALL include an "Email" navigation entry alongside the existing General, Members, and Danger zone entries. The entry SHALL link to `/teams/{teamSlug}/settings/email` and SHALL be highlighted as active when that route (or any sub-route) is selected.

#### Scenario: Email entry visible in sidebar

- **WHEN** an organizer opens any page under `/teams/{teamSlug}/settings`
- **THEN** the team settings sidebar shows four entries: General, Members, Email, and Danger zone

#### Scenario: Active state on Email page

- **WHEN** an organizer is on `/teams/acme/settings/email`
- **THEN** the "Email" sidebar entry is rendered with the active style and the other entries are inactive

---

### Requirement: Team Email page shows the team-scoped email settings form

The Admin UI SHALL render a page at `/teams/{teamSlug}/settings/email` that loads the team-scoped email settings via `GET /admin/teams/{teamSlug}/email-settings` and presents a form for SMTP host, SMTP port, from-address, authentication mode, username, and password. When the GET responds with `404`, the page SHALL render the same form pre-filled with empty defaults so the organizer can create the team-scoped row. The form SHALL share its component implementation with the event Email tab, parameterised only by scope-specific URL and query key.

#### Scenario: Page renders empty form when team has no settings

- **WHEN** an organizer opens `/teams/acme/settings/email` and the backend returns `404` for the team-scoped GET
- **THEN** the page shows the email settings form with empty defaults (host empty, port `587`, auth mode `none`)

#### Scenario: Page renders pre-filled form when team has settings

- **WHEN** an organizer opens `/teams/acme/settings/email` and the backend returns the team-scoped settings DTO with `smtpHost="smtp.acme.org"`, `smtpPort=587`, `fromAddress="events@acme.org"`
- **THEN** the form is pre-filled with those values and the password field is rendered masked-and-empty

#### Scenario: Same form component is used for both scopes

- **WHEN** code search inspects the team Email page and the event Email tab
- **THEN** both mount the same `EmailSettingsForm` component, differing only in the API URL and React Query key passed in

---

### Requirement: Team Email form saves via the team-scoped admin endpoint

Submitting the team Email form SHALL `PUT` to `/admin/teams/{teamSlug}/email-settings` with the form values and the loaded `Version` (or `null` when creating). On success the page SHALL invalidate the team-scoped React Query key and reset the password field to empty. Server-side validation errors SHALL be surfaced inline against the offending field; concurrency conflicts SHALL surface a top-level error indicating the row was modified by someone else.

#### Scenario: Create team settings on first save

- **WHEN** an organizer fills in host, port, from-address, auth mode `basic`, username, and password on a previously-empty team Email page and clicks Save
- **THEN** the UI sends `PUT /admin/teams/acme/email-settings` with `version: null` and a body containing the field values, and on `200`/`201` the form reflects the saved row

#### Scenario: Update team settings preserving the password

- **WHEN** an organizer changes only the from-address, leaves the password field blank, and clicks Save on a team that already has settings
- **THEN** the UI sends `PUT` with `password: null` and the existing `Version`, and the backend keeps the stored password unchanged

#### Scenario: Concurrency conflict surfaced

- **WHEN** the backend rejects the `PUT` with a concurrency conflict error
- **THEN** the form displays a top-level error indicating the row was modified externally and prompts a refresh

---

### Requirement: Team Email page supports deleting the team-scoped row

The team Email page SHALL provide a "Delete team email settings" action visible only when a team-scoped row exists. The action SHALL prompt for confirmation and then issue `DELETE /admin/teams/{teamSlug}/email-settings` with the loaded `Version`. On success the page SHALL invalidate the React Query cache and re-render the empty form (matching the no-settings state).

#### Scenario: Delete requires confirmation

- **WHEN** an organizer clicks "Delete team email settings" on a team that has a team-scoped row
- **THEN** a confirmation dialog appears and no request is sent until the organizer confirms

#### Scenario: Successful delete returns the page to empty state

- **WHEN** the organizer confirms the delete and the backend returns `200`/`204`
- **THEN** the form is rendered empty (host empty, port `587`, auth mode `none`) and the delete action is hidden

#### Scenario: Delete action hidden when no team-scoped row

- **WHEN** the team Email page renders and the team-scoped GET returned `404`
- **THEN** the delete action is not displayed

---

### Requirement: Admin UI exposes a Next.js proxy route for team-scoped email settings

The Admin UI SHALL provide a Next.js API route at `app/api/teams/[teamSlug]/email-settings/route.ts` that forwards `GET`, `PUT`, and `DELETE` to the backend's `/admin/teams/{teamSlug}/email-settings` endpoint, attaching the same auth-token header used by the existing event-scoped proxy. Client code SHALL call this proxy and SHALL NOT call the backend directly.

#### Scenario: Proxy forwards GET

- **WHEN** the team Email page issues `GET /api/teams/acme/email-settings`
- **THEN** the proxy issues `GET /admin/teams/acme/email-settings` to the backend with the user's auth token, and relays the status and JSON body verbatim

#### Scenario: Proxy forwards PUT and DELETE

- **WHEN** the team Email page issues `PUT` or `DELETE` against `/api/teams/acme/email-settings`
- **THEN** the proxy forwards the same method and body to the backend's team-scoped endpoint and relays the response
