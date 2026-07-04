## MODIFIED Requirements

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

The Admin UI SHALL render a detail page at `/teams/{teamSlug}/events/{eventSlug}/emails/campaigns/{jobId}` that fetches job details from `GET /admin/…/bulk-emails/{id}`. The page SHALL display: job status, email type, trigger, source descriptor (attendee filters or "External list (N recipients)"), ad-hoc subject/body if present, timestamps (created, started, completed), totals (recipient count, sent, failed, cancelled), and a list or count of failed recipients with their last error. A "Back to bulk emails" link SHALL navigate to the campaigns tab.

For jobs in a non-terminal state (Pending, Resolving, Sending), a "Cancel" button SHALL be shown. Clicking it SHALL call `POST /admin/…/bulk-emails/{id}/cancel`, show a success notification, and refresh the job status.

#### Scenario: Detail page shows job summary

- **WHEN** an organizer opens the detail page for a Completed bulk email job at the new URL
- **THEN** the page shows status "Completed", sent count, failed count, creation and completion timestamps

#### Scenario: Cancel button present for active jobs

- **WHEN** an organizer opens the detail page for a job in Sending status
- **THEN** a "Cancel" button is visible

#### Scenario: Cancel button absent for terminal jobs

- **WHEN** an organizer opens the detail page for a Completed job
- **THEN** no "Cancel" button is shown

#### Scenario: Old job detail URL redirects to new path

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/emails/abc123`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/emails/campaigns/abc123`

---

### Requirement: Send bulk email action uses a Sheet panel

The "Send bulk email" action SHALL be presented as a `Sheet` slide-in panel rather than a modal dialog. The Sheet SHALL slide from the right on desktop and from the bottom on mobile. The form content (recipient selection, subject, body) SHALL be identical to the previous dialog implementation.

#### Scenario: Send bulk email opens as Sheet

- **WHEN** an organizer clicks "Send bulk email"
- **THEN** a Sheet panel slides in from the right (desktop) or bottom (mobile) with the send form

#### Scenario: Sheet closes on successful submission

- **WHEN** an organizer submits a valid bulk email form
- **THEN** the Sheet closes and the campaigns list refreshes

## ADDED Requirements

### Requirement: Unified Email tabbed page groups all email concerns

The Admin UI SHALL render a unified Email page at `/teams/{teamSlug}/events/{eventSlug}/emails` that presents three tabs: **Campaigns**, **Templates**, and **Setup**. The **Campaigns** tab is the default. Navigating to the bare `/emails` path SHALL redirect to `/emails/campaigns`. Each tab SHALL be an independently routable sub-page. The active tab SHALL be visually highlighted.

#### Scenario: Navigating to /emails shows Campaigns tab by default

- **WHEN** an organizer clicks "Email" in the event sidebar
- **THEN** the browser navigates to the Campaigns tab at `…/emails/campaigns`

#### Scenario: Tab navigation switches between Email sub-pages

- **WHEN** an organizer clicks the "Templates" tab
- **THEN** the URL changes to `…/emails/templates` and the template list loads

#### Scenario: Tab navigation to Setup

- **WHEN** an organizer clicks the "Setup" tab
- **THEN** the URL changes to `…/emails/setup` and the email settings form loads
