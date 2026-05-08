## ADDED Requirements

### Requirement: Bulk Emails list page shows all bulk email jobs for an event

The Admin UI SHALL render a Bulk Emails list page at `/teams/{teamSlug}/events/{eventSlug}/emails`. The page SHALL fetch all bulk email jobs from `GET /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails` and display them in a table. Each row SHALL show at minimum: a human-readable label for the email type (e.g. "Custom" for `bulk-custom`, "Reconfirm" for `reconfirm`), the job status with a colour-coded badge, recipient count, sent count, failed count, the trigger (organizer name or "System"), and the creation timestamp. The table SHALL default to newest-first order.

A status-filter control SHALL allow the organizer to filter the list to a specific status subset (e.g. active: Pending/Resolving/Sending; completed; failed/cancelled). When no jobs exist, the page SHALL render an empty state that explains the feature and offers a "Send bulk email" button.

#### Scenario: List page renders all jobs newest-first

- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/emails` and three bulk email jobs exist (one Pending, one Completed, one Cancelled)
- **THEN** the page displays three rows in descending creation-time order with status badges "Pending", "Completed", and "Cancelled"

#### Scenario: Status filter narrows results

- **WHEN** an organizer selects the "Active" status filter
- **THEN** only jobs with status Pending, Resolving, or Sending are shown

#### Scenario: Empty state shown when no jobs exist

- **WHEN** no bulk email jobs exist for the event
- **THEN** the page shows an empty-state message and a "Send bulk email" button

#### Scenario: Row click navigates to job detail page

- **WHEN** an organizer clicks a row in the bulk emails table
- **THEN** the browser navigates to `/teams/{teamSlug}/events/{eventSlug}/emails/{jobId}`

---

### Requirement: Bulk Email job detail page shows audit information and supports cancellation

The Admin UI SHALL render a detail page at `/teams/{teamSlug}/events/{eventSlug}/emails/{jobId}` that fetches job details from `GET /admin/…/bulk-emails/{id}`. The page SHALL display: job status, email type, trigger, source descriptor (attendee filters or "External list (N recipients)"), ad-hoc subject/body if present, timestamps (created, started, completed), totals (recipient count, sent, failed, cancelled), and a list or count of failed recipients with their last error. A "Back to bulk emails" link SHALL be present.

For jobs in a non-terminal state (Pending, Resolving, Sending), a "Cancel" button SHALL be shown. Clicking it SHALL call `POST /admin/…/bulk-emails/{id}/cancel`, show a success notification, and refresh the job status.

#### Scenario: Detail page shows job summary

- **WHEN** an organizer opens the detail page for a Completed bulk email job
- **THEN** the page shows status "Completed", sent count, failed count, creation and completion timestamps

#### Scenario: Cancel button present for active jobs

- **WHEN** an organizer opens the detail page for a job in Sending status
- **THEN** a "Cancel" button is visible

#### Scenario: Cancel button absent for terminal jobs

- **WHEN** an organizer opens the detail page for a Completed job
- **THEN** no "Cancel" button is shown

#### Scenario: Cancel success refreshes status

- **WHEN** an organizer clicks "Cancel" and the backend responds 202 Accepted
- **THEN** a success notification is shown and the page re-fetches the job, eventually showing Cancelled status

---

### Requirement: Create Bulk Email dialog allows organizers to select a template and send a custom bulk email

The Bulk Emails list page SHALL provide a "Send bulk email" button that opens a multi-step dialog. The dialog SHALL have two steps:

**Step 1 — Select template**: A dropdown or searchable list shows all `CustomBulkTemplate` records for the event scope (fetched from `GET /api/…/custom-bulk-templates`). The organizer selects one. A "Create template" link in the dialog navigates to the templates settings page. If no custom templates exist, the dialog shows a prompt to create one first and the Send button is disabled.

**Step 2 — Recipients**: The organizer chooses the recipient source:
- *Registered attendees*: an optional ticket-type multi-select filter and optional registration-status filter (defaults to all confirmed registrations). A "Preview" action calls `POST /admin/…/bulk-emails/preview` and displays the matched count and a sample.
- *External list (CSV)*: the organizer uploads a CSV file. The UI parses the file client-side. The expected format is one row per recipient with at minimum an `email` column; an optional `name` column is also supported. After parsing, the matched count is shown and the organizer may inspect the first N rows. CSV uploads SHALL be capped at 5,000 rows; exceeding this limit displays a client-side validation error.

The dialog SHALL display a summary ("You are about to send to N recipients using template '{name}'. This action cannot be undone.") and a "Send" button. Clicking "Send" calls `POST /admin/…/bulk-emails` with `emailType: "bulk-custom"`, the selected template's `subject`/`textBody`/`htmlBody` as the ad-hoc content fields, and the resolved source payload. On success the dialog closes, the list page re-fetches, and a toast notification is shown.

#### Scenario: Dialog opens from the "Send bulk email" button

- **WHEN** an organizer clicks "Send bulk email" on the list page
- **THEN** the dialog opens on Step 1 (Select template)

#### Scenario: No templates available disables Send

- **WHEN** no custom templates exist for the event and the organizer opens the dialog
- **THEN** the dialog shows a prompt to create a template first and the "Send" button is disabled

#### Scenario: Selected template name appears in the summary

- **WHEN** an organizer selects the "Alumni invite" template and proceeds to Step 2
- **THEN** the send confirmation summary reads "You are about to send to N recipients using template 'Alumni invite'."

#### Scenario: Attendee-source preview shows count and sample

- **WHEN** an organizer selects "Registered attendees" with no filters and clicks "Preview"
- **THEN** the UI calls the preview endpoint and displays "X recipients matched" plus a sample of email addresses

#### Scenario: CSV upload parses and shows row count

- **WHEN** an organizer uploads a valid CSV file with 250 rows containing email and name columns
- **THEN** the dialog shows "250 recipients" and the first few rows in a preview table

#### Scenario: CSV upload rejects files over the row limit

- **WHEN** an organizer uploads a CSV file with 5,001 rows
- **THEN** the dialog shows a validation error "CSV must not exceed 5,000 recipients" and does not proceed

#### Scenario: Successful send creates the job and shows a toast

- **WHEN** an organizer completes both steps and clicks "Send"
- **THEN** the UI posts to the create endpoint with the selected template's content as ad-hoc fields, the dialog closes, a "Bulk email queued" toast is shown, and the new job appears in the list with Pending status

#### Scenario: Backend validation error surfaced inline

- **WHEN** the backend rejects the create request (e.g. no SMTP configured)
- **THEN** the dialog shows the error message and remains open so the organizer can correct the issue

---

### Requirement: Admin UI exposes Next.js proxy routes for bulk-email endpoints

The Admin UI SHALL provide Next.js API routes that forward requests to the backend bulk-email endpoints, attaching the auth-token header. Required proxy routes:

- `GET  /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` → `GET  /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` → `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/preview` → `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/preview`
- `GET  /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]` → `GET  /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]/cancel` → `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}/cancel`

#### Scenario: List proxy forwards GET

- **WHEN** the Admin UI requests `GET /api/teams/acme/events/devconf-2026/bulk-emails`
- **THEN** the proxy forwards to `GET /admin/teams/acme/events/devconf-2026/bulk-emails` with the auth token and relays the response

#### Scenario: Create proxy forwards POST

- **WHEN** the Admin UI posts to `/api/teams/acme/events/devconf-2026/bulk-emails`
- **THEN** the proxy forwards to `POST /admin/teams/acme/events/devconf-2026/bulk-emails` with the auth token and relays the response

#### Scenario: Cancel proxy forwards POST

- **WHEN** the Admin UI posts to `/api/teams/acme/events/devconf-2026/bulk-emails/some-job-id/cancel`
- **THEN** the proxy forwards to `POST /admin/teams/acme/events/devconf-2026/bulk-emails/some-job-id/cancel` with the auth token and relays the response
