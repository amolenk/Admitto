# admin-ui-email-templates Specification

## Purpose
Organizers manage email templates from the Admin UI. They can view all supported template types for a team or event scope, see whether each type has a custom override or uses the built-in default, create or edit a custom template, preview the rendered output, delete a custom override, and send a rendered test email to a chosen recipient.

## Requirements

### Requirement: Email settings page links to a template list sub-page
The team Email settings page (`/teams/{teamSlug}/settings/email`) and the event Email settings page SHALL each include a "Templates" section or link that navigates to a template list page at `…/settings/email/templates`. The link SHALL be visible regardless of whether any custom templates exist.

#### Scenario: Templates link visible on team email settings page
- **WHEN** an organizer opens `/teams/acme/settings/email`
- **THEN** a "Templates" link or button is visible and clicking it navigates to `/teams/acme/settings/email/templates`

#### Scenario: Templates link visible on event email settings page
- **WHEN** an organizer opens the event email settings page for "devconf-2026"
- **THEN** a "Templates" link is visible and navigates to the event-scoped templates list

---

### Requirement: Template list page enumerates all supported types
The Admin UI SHALL render a template list page at `/teams/{teamSlug}/settings/email/templates` (and its event-scoped equivalent) that shows one row per supported template type (`ticket`, `cancellation`, `visa-letter-denied`, `ticket-types-removed`, `reconfirm`). Each row SHALL display a human-readable label for the type and a status badge indicating "Custom" when a stored custom template exists for that scope or "Default" when the built-in default will be used.

#### Scenario: Rows appear for all supported types
- **WHEN** an organizer opens the team template list page for team "acme"
- **THEN** the page shows exactly five rows, one for each supported template type

#### Scenario: Custom badge shown for a type with a stored template
- **WHEN** team "acme" has a custom `ticket` template stored and no custom template for the other types
- **THEN** the `ticket` row shows a "Custom" badge and all other rows show "Default"

#### Scenario: Clicking a row navigates to the template detail page
- **WHEN** an organizer clicks the `ticket` row
- **THEN** the browser navigates to `/teams/acme/settings/email/templates/ticket`

---

### Requirement: Template detail page loads the stored custom template or empty defaults
The Admin UI SHALL render a template detail page at `/teams/{teamSlug}/settings/email/templates/{type}` (and the event-scoped equivalent) that fetches the stored custom template via `GET /admin/teams/{teamSlug}/email-templates/{type}` and displays a form with Subject, Text Body, and HTML Body fields. When the GET returns 404 (no custom template), the form SHALL be rendered empty so the organizer can create one. A "Back to templates" link SHALL be present.

#### Scenario: Form pre-filled when custom template exists
- **WHEN** an organizer opens the detail page for the `ticket` type and a custom template exists
- **THEN** the Subject, Text Body, and HTML Body fields are pre-filled with the stored values

#### Scenario: Empty form when no custom template exists
- **WHEN** an organizer opens the detail page for the `ticket` type and the GET returns 404
- **THEN** the form fields are empty and the delete action is hidden

---

### Requirement: Template detail page saves via the upsert endpoint
Submitting the template form SHALL `PUT` to `/admin/teams/{teamSlug}/email-templates/{type}` with the form values and the loaded `Version` (or `null` when creating). On success the page SHALL refetch the template and show a success notification. Validation errors SHALL be surfaced inline.

#### Scenario: Create new custom template
- **WHEN** an organizer fills in Subject, Text Body, and HTML Body on an empty form and clicks Save
- **THEN** the UI sends `PUT /admin/teams/acme/email-templates/ticket` with `version: null` and the entered values, and on success the form reflects the saved state with the new version

#### Scenario: Update existing custom template
- **WHEN** an organizer edits the Subject and clicks Save on a form with a loaded version
- **THEN** the UI sends `PUT` with the current `Version` and on success the row in the list shows "Custom"

---

### Requirement: Template detail page supports deleting the custom template
The template detail page SHALL show a "Delete custom template" action only when a stored custom template exists. Clicking it SHALL prompt for confirmation and then send `DELETE /admin/teams/{teamSlug}/email-templates/{type}`. On success the form is cleared and the status reverts to "Default".

#### Scenario: Delete action hidden when no custom template exists
- **WHEN** the template detail page renders with a 404 from the GET
- **THEN** the delete action is not displayed

#### Scenario: Delete prompts for confirmation
- **WHEN** an organizer clicks "Delete custom template" on a page with a loaded template
- **THEN** a confirmation dialog appears and no DELETE request is sent until confirmed

#### Scenario: Successful delete clears form and reverts badge to Default
- **WHEN** the organizer confirms the delete and the backend returns 200
- **THEN** the form is cleared, the delete action is hidden, and the list page would show "Default" for this type

---

### Requirement: Template detail page shows a rendered preview
The template detail page SHALL include a "Preview" panel that fetches the rendered output from `GET /admin/teams/{teamSlug}/email-templates/{type}/preview` and displays the rendered subject, text body, and HTML body. The HTML body SHALL be displayed inside an isolated sandbox (e.g. `<iframe srcdoc>`). The preview SHALL refresh automatically when the page loads and SHALL provide a manual "Refresh preview" button.

#### Scenario: Preview panel shows rendered subject and HTML
- **WHEN** an organizer opens the template detail page for `ticket`
- **THEN** the preview panel displays the rendered subject line and the rendered HTML body in a sandboxed container

#### Scenario: Refresh preview button updates the panel
- **WHEN** an organizer clicks "Refresh preview"
- **THEN** the frontend re-fetches the preview endpoint and updates the preview panel content

---

### Requirement: Template detail page supports sending a test email
The template detail page SHALL include a "Send test email" action that opens a dialog. The dialog SHALL present a dropdown of candidate recipient addresses composed of: (1) the email addresses of all team members, and (2) the `fromAddress` configured in the email settings for the current scope (team or event), if any. Duplicate addresses SHALL be deduplicated. The organizer selects one recipient and confirms; the UI SHALL then `POST` to `/admin/teams/{teamSlug}/email-templates/{type}/test-send` with the selected email address. On success a success notification SHALL be shown.

#### Scenario: Recipient dropdown includes team member emails and SMTP from-address
- **WHEN** an organizer opens the Send test email dialog on the ticket template detail page for team "acme" and the team has email settings with `fromAddress = "events@acme.org"` and members "alice@example.com" and "bob@example.com"
- **THEN** the recipient dropdown lists "alice@example.com", "bob@example.com", and "events@acme.org" (deduplicated)

#### Scenario: Recipient dropdown includes only member emails when no email settings exist
- **WHEN** an organizer opens the dialog and no email settings are configured for the scope
- **THEN** the recipient dropdown lists only the team member email addresses

#### Scenario: Test email sent to selected recipient
- **WHEN** an organizer selects "alice@example.com" from the dropdown and clicks Send
- **THEN** the UI posts `{ "recipient": "alice@example.com" }` to the test-send endpoint and on success shows a confirmation notification

#### Scenario: Error surfaced when test send fails
- **WHEN** the test-send endpoint returns an error (e.g. SMTP not configured)
- **THEN** the dialog shows the error message and remains open

---

### Requirement: Admin UI exposes Next.js proxy routes for template preview and test-send endpoints
The Admin UI SHALL provide Next.js API routes that forward requests to the backend preview and test-send endpoints, attaching the auth-token header. Proxy routes SHALL follow the same pattern as existing email-settings proxy routes.

#### Scenario: Proxy forwards preview GET
- **WHEN** the Admin UI requests `GET /api/teams/acme/email-templates/ticket/preview`
- **THEN** the proxy forwards the request to `GET /admin/teams/acme/email-templates/ticket/preview` with the auth token and relays the response

#### Scenario: Proxy forwards test-send POST
- **WHEN** the Admin UI posts to `/api/teams/acme/email-templates/ticket/test-send`
- **THEN** the proxy forwards to `POST /admin/teams/acme/email-templates/ticket/test-send` and relays the response
