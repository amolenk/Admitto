## ADDED Requirements

### Requirement: Email templates area includes a Custom Templates section for bulk email templates

The event (and team) email templates page SHALL include a **Custom Templates** section alongside the existing system-type rows. This section lists all `CustomBulkTemplate` records for the scope and provides actions to create, edit, and delete them. It is the primary place where organizers manage the reusable content for custom bulk sends.

The Custom Templates section SHALL display a table with columns: Name, Subject preview (truncated), and action buttons (Edit, Delete). When no custom templates exist, the section SHALL show an empty state with a "Create custom template" button.

#### Scenario: Custom Templates section visible on the event templates page

- **WHEN** an organizer opens the event email templates page for "devconf-2026"
- **THEN** a "Custom Templates" section is visible below the system-type rows

#### Scenario: Empty state shown when no custom templates exist

- **WHEN** no custom bulk templates exist for the event
- **THEN** the Custom Templates section shows an empty-state message and a "Create custom template" button

#### Scenario: Existing custom templates listed in the section

- **WHEN** two custom templates ("Alumni invite", "Schedule update") exist for the event
- **THEN** both appear in the Custom Templates table ordered by name

---

### Requirement: Organizers can create a custom bulk email template from the templates page

Clicking "Create custom template" SHALL open a dialog (or navigate to a sub-page) with a form collecting: Name (required, unique within scope), Subject (required), Text Body (required), and HTML Body (optional). Submitting SHALL `POST` to `/admin/…/custom-bulk-templates`. On success the section refreshes and a success notification is shown. Validation errors SHALL be surfaced inline.

#### Scenario: Create form validates required fields

- **WHEN** an organizer submits the create form without a Subject
- **THEN** an inline validation error is shown and no POST is sent

#### Scenario: Duplicate name error surfaced inline

- **WHEN** an organizer creates a template with a name already in use in the same scope
- **THEN** the form shows "A template with this name already exists" inline

#### Scenario: Successful create adds the template to the list

- **WHEN** an organizer fills in all required fields and submits
- **THEN** the new template appears in the Custom Templates table and a success toast is shown

---

### Requirement: Organizers can edit a custom bulk email template

Clicking "Edit" on a custom template row SHALL open a pre-filled form (same fields as create, plus the current `Version`). Submitting SHALL `PUT` to `/admin/…/custom-bulk-templates/{id}`. Optimistic concurrency conflicts SHALL be surfaced as an inline error prompting the organizer to reload.

#### Scenario: Edit form pre-filled with existing values

- **WHEN** an organizer clicks "Edit" on the "Alumni invite" template
- **THEN** the form opens with Name, Subject, Text Body, and HTML Body pre-populated

#### Scenario: Successful edit updates the list

- **WHEN** an organizer changes the Subject and saves
- **THEN** the updated template appears in the list with the new Subject preview

---

### Requirement: Organizers can delete a custom bulk email template

Clicking "Delete" on a custom template row SHALL prompt for confirmation. On confirmation it SHALL `DELETE /admin/…/custom-bulk-templates/{id}`. On success the template is removed from the list and a success notification is shown.

#### Scenario: Delete prompts for confirmation

- **WHEN** an organizer clicks "Delete" on a custom template row
- **THEN** a confirmation dialog appears and no DELETE request is sent until confirmed

#### Scenario: Successful delete removes the template from the list

- **WHEN** the organizer confirms deletion and the backend responds `204 No Content`
- **THEN** the template row is removed and a success toast is shown

---

### Requirement: Admin UI exposes proxy routes for custom-bulk-template endpoints

The Admin UI SHALL provide Next.js API routes forwarding requests to the backend custom-bulk-template endpoints with the auth token:

- `GET  /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates` → `GET  /admin/…`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates` → `POST /admin/…`
- `GET  /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` → `GET  /admin/…/{id}`
- `PUT  /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` → `PUT  /admin/…/{id}`
- `DELETE /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` → `DELETE /admin/…/{id}`

#### Scenario: List proxy forwards GET

- **WHEN** the Admin UI requests `GET /api/teams/acme/events/devconf-2026/custom-bulk-templates`
- **THEN** the proxy forwards to `GET /admin/teams/acme/events/devconf-2026/custom-bulk-templates` with the auth token and relays the response
