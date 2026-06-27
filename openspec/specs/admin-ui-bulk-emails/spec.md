# admin-ui-bulk-emails Specification

## Purpose

The Admin UI lets organizers view, audit, create, and cancel event bulk email campaigns. Custom campaign content is authored directly in the send flow and stored on the bulk email job.

## Requirements

### Requirement: Bulk Emails list page shows all bulk email jobs for an event

The Admin UI SHALL render a Bulk Emails list page at `/teams/{teamSlug}/events/{eventSlug}/emails/campaigns`. The page SHALL fetch all bulk email jobs from `GET /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails` and display them in a table. Each row SHALL show at minimum: a human-readable label for the email type (e.g. "Custom" for `bulk-custom`, "Reconfirm" for `reconfirm`), the job status with a colour-coded badge, recipient count, sent count, failed count, the trigger (organizer name or "System"), and the creation timestamp. The table SHALL default to newest-first order.

A status-filter control SHALL allow the organizer to filter the list to a specific status subset (e.g. active: Pending/Resolving/Sending; completed; failed/cancelled). When no jobs exist, the page SHALL render an empty state that explains the feature and offers a "Send bulk email" button.

The Bulk Emails list page is accessed via the **Campaigns** tab in the unified Email tabbed page at `/teams/{teamSlug}/events/{eventSlug}/emails`.

#### Scenario: List page renders all jobs newest-first

- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/emails/campaigns` and three bulk email jobs exist (one Pending, one Completed, one Cancelled)
- **THEN** the page displays three rows in descending creation-time order with status badges "Pending", "Completed", and "Cancelled"

#### Scenario: Status filter narrows results

- **WHEN** an organizer selects the "Active" status filter
- **THEN** only jobs with status Pending, Resolving, or Sending are shown

#### Scenario: Empty state shown when no jobs exist

- **WHEN** no bulk email jobs exist for the event
- **THEN** the page shows an empty-state message and a "Send bulk email" button

#### Scenario: Row click navigates to job detail page

- **WHEN** an organizer clicks a row in the bulk emails table
- **THEN** the browser navigates to `/teams/{teamSlug}/events/{eventSlug}/emails/campaigns/{jobId}`

#### Scenario: Old emails URL redirects to campaigns tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/emails`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/emails/campaigns`

---

### Requirement: Bulk Email job detail page shows audit information and supports cancellation

The Admin UI SHALL render a detail page at `/teams/{teamSlug}/events/{eventSlug}/emails/campaigns/{jobId}` that fetches job details from `GET /admin/.../bulk-emails/{id}`. The page SHALL display: job status, email type, trigger, source descriptor (attendee filters or "External list (N recipients)"), job-owned subject/body if present, timestamps (created, started, completed), totals (recipient count, sent, failed, cancelled), and a list or count of failed recipients with their last error. A "Back to bulk emails" link SHALL navigate to the campaigns tab.

For jobs in a non-terminal state (Pending, Resolving, Sending), a "Cancel" button SHALL be shown. Clicking it SHALL call `POST /admin/.../bulk-emails/{id}/cancel`, show a success notification, and refresh the job status.

#### Scenario: Detail page shows job summary

- **WHEN** an organizer opens the detail page for a Completed bulk email job at the new URL
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

#### Scenario: Old job detail URL redirects to new path

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/emails/abc123`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/emails/campaigns/abc123`

---

### Requirement: Send bulk email action uses a Sheet panel

The "Send bulk email" action SHALL be presented as a `Sheet` slide-in panel rather than a modal dialog. The Sheet SHALL slide from the right on desktop and from the bottom on mobile. The form SHALL collect custom bulk content directly: Subject, Text Body, HTML Body, and recipient selection. It SHALL NOT require or allow selecting a stored template.

#### Scenario: Send bulk email opens as Sheet

- **WHEN** an organizer clicks "Send bulk email"
- **THEN** a Sheet panel slides in from the right (desktop) or bottom (mobile) with the send form

#### Scenario: Sheet collects direct content

- **WHEN** the Sheet opens
- **THEN** it shows required fields for Subject, Text Body, and HTML Body before or alongside recipient selection

#### Scenario: Template selection is absent

- **WHEN** the Sheet opens
- **THEN** it does not load or render a stored template selector

#### Scenario: Sheet closes on successful submission

- **WHEN** an organizer submits a valid bulk email form
- **THEN** the Sheet closes and the campaigns list refreshes

---

### Requirement: Unified Email tabbed page groups all email concerns

The Admin UI SHALL render a unified Email page at `/teams/{teamSlug}/events/{eventSlug}/emails` that presents the event email concerns that remain after simplification. The **Campaigns** tab SHALL be the default. Navigating to the bare `/emails` path SHALL redirect to `/emails/campaigns`. Template and event SMTP setup tabs SHALL NOT be shown because transactional templates are not editable and SMTP settings are deployment-managed.

#### Scenario: Navigating to /emails shows Campaigns tab by default

- **WHEN** an organizer clicks "Email" in the event sidebar
- **THEN** the browser navigates to the Campaigns tab at `.../emails/campaigns`

#### Scenario: Campaigns remains available

- **WHEN** an organizer opens `.../emails/campaigns`
- **THEN** the campaigns list loads

#### Scenario: Templates tab removed

- **WHEN** an organizer opens the event email page
- **THEN** no Templates tab is shown

#### Scenario: Setup tab removed

- **WHEN** an organizer opens the event email page
- **THEN** no event-scoped Setup tab or settings form is shown

---

### Requirement: Admin UI exposes Next.js proxy routes for bulk-email endpoints

The Admin UI SHALL provide Next.js API routes that forward requests to the backend bulk-email endpoints, attaching the auth-token header. Required proxy routes:

- `GET  /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` -> `GET  /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` -> `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/preview` -> `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/preview`
- `GET  /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]` -> `GET /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}`
- `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]/cancel` -> `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}/cancel`

Bulk email create proxying SHALL forward the direct content fields (`subject`, `textBody`, `htmlBody`) supplied by the Sheet and SHALL NOT fetch or copy content from a stored template.

#### Scenario: List proxy forwards GET

- **WHEN** the Admin UI requests `GET /api/teams/acme/events/devconf-2026/bulk-emails`
- **THEN** the proxy forwards to `GET /admin/teams/acme/events/devconf-2026/bulk-emails` with the auth token and relays the response

#### Scenario: Create proxy forwards direct content POST

- **WHEN** the Admin UI posts to `/api/teams/acme/events/devconf-2026/bulk-emails` with subject, text body, HTML body, and source
- **THEN** the proxy forwards those fields to `POST /admin/teams/acme/events/devconf-2026/bulk-emails` with the auth token and relays the response

#### Scenario: Cancel proxy forwards POST

- **WHEN** the Admin UI posts to `/api/teams/acme/events/devconf-2026/bulk-emails/some-job-id/cancel`
- **THEN** the proxy forwards to `POST /admin/teams/acme/events/devconf-2026/bulk-emails/some-job-id/cancel` with the auth token and relays the response
