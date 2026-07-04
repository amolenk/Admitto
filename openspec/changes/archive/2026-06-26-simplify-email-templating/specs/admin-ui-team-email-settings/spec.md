## MODIFIED Requirements

### Requirement: Team Email page shows the team-scoped email settings form
The Admin UI SHALL render a page at `/teams/{teamSlug}/settings/email` that loads the team-scoped email settings via `GET /admin/teams/{teamSlug}/email-settings` and presents a form for SMTP host, SMTP port, from-address, authentication mode, username, password, accent color, and font selection. When the GET responds with `404`, the page SHALL render the same form pre-filled with empty/default SMTP values and default branding values so the organizer can create the team-scoped row. This page SHALL be the only SMTP settings UI; event-scoped email settings are not shown.

#### Scenario: Page renders empty form when team has no settings
- **WHEN** an organizer opens `/teams/acme/settings/email` and the backend returns `404` for the team-scoped GET
- **THEN** the page shows the email settings form with empty/default SMTP values (host empty, port `587`, auth mode `none`) and default branding values

#### Scenario: Page renders pre-filled form when team has settings
- **WHEN** an organizer opens `/teams/acme/settings/email` and the backend returns the team-scoped settings DTO with `smtpHost="smtp.acme.org"`, `smtpPort=587`, `fromAddress="events@acme.org"`, `accentColor="#2563eb"`, and `font="Inter"`
- **THEN** the form is pre-filled with those values and the password field is rendered masked-and-empty

#### Scenario: Event settings UI is absent
- **WHEN** an organizer opens an event email area
- **THEN** no event-scoped SMTP settings form is rendered

### Requirement: Team Email form saves via the team-scoped admin endpoint
Submitting the team Email form SHALL `PUT` to `/admin/teams/{teamSlug}/email-settings` with the SMTP fields, branding fields, and the loaded `Version` (or `null` when creating). On success the page SHALL invalidate the team-scoped React Query key and reset the password field to empty. Server-side validation errors SHALL be surfaced inline against the offending field; concurrency conflicts SHALL surface a top-level error indicating the row was modified by someone else.

#### Scenario: Create team settings on first save
- **WHEN** an organizer fills in host, port, from-address, auth mode `basic`, username, password, accent color, and font on a previously-empty team Email page and clicks Save
- **THEN** the UI sends `PUT /admin/teams/acme/email-settings` with `version: null` and a body containing the field values, and on `200`/`201` the form reflects the saved row

#### Scenario: Update team settings preserving the password
- **WHEN** an organizer changes only the from-address, leaves the password field blank, and clicks Save on a team that already has settings
- **THEN** the UI sends `PUT` with `password: null` and the existing `Version`, and the backend keeps the stored password unchanged

#### Scenario: Update branding only
- **WHEN** an organizer changes only the accent color or font selection and clicks Save
- **THEN** the UI sends the updated branding values with the current `Version` and the backend preserves unchanged SMTP credential fields

#### Scenario: Concurrency conflict surfaced
- **WHEN** the backend rejects the `PUT` with a concurrency conflict error
- **THEN** the form displays a top-level error indicating the row was modified externally and prompts a refresh

### Requirement: Admin UI exposes a Next.js proxy route for team-scoped email settings
The Admin UI SHALL provide a Next.js API route at `app/api/teams/[teamSlug]/email-settings/route.ts` that forwards `GET`, `PUT`, and `DELETE` to the backend's `/admin/teams/{teamSlug}/email-settings` endpoint, attaching the same auth-token header used by other backend proxies. Client code SHALL call this proxy and SHALL NOT call the backend directly. There SHALL be no event-scoped email-settings proxy route.

#### Scenario: Proxy forwards GET
- **WHEN** the team Email page issues `GET /api/teams/acme/email-settings`
- **THEN** the proxy issues `GET /admin/teams/acme/email-settings` to the backend with the user's auth token, and relays the status and JSON body verbatim

#### Scenario: Proxy forwards PUT and DELETE
- **WHEN** the team Email page issues `PUT` or `DELETE` against `/api/teams/acme/email-settings`
- **THEN** the proxy forwards the same method and body to the backend's team-scoped endpoint and relays the response

#### Scenario: Event settings proxy removed
- **WHEN** code search inspects Admin UI API routes
- **THEN** there is no `/api/teams/[teamSlug]/events/[eventSlug]/email-settings` proxy route

## ADDED Requirements

### Requirement: Team Email page exposes simple branding controls
The Team Email page SHALL include an accent color input and a font selection control. The font selection SHALL show a minimal set of font choices configured in the UI. The selected value SHALL be sent to the API as a string. The UI SHALL NOT expose a separate Google Fonts URL, arbitrary external font URL, or custom CSS setting.

#### Scenario: Branding controls are visible
- **WHEN** an organizer opens `/teams/acme/settings/email`
- **THEN** the form shows controls for accent color and font selection

#### Scenario: Font choices come from UI configuration
- **WHEN** an organizer uses the Team Email page
- **THEN** the font selection lists the minimal configured UI choices and submits the selected string value

#### Scenario: Separate font URL setting is absent
- **WHEN** an organizer uses the Team Email page
- **THEN** there is no separate input for a Google Fonts URL, arbitrary external font URL, or custom CSS
