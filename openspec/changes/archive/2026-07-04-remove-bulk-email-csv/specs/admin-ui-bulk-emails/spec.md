## MODIFIED Requirements

### Requirement: Bulk Email job detail page shows audit information and supports cancellation

The Admin UI SHALL render a detail page at `/teams/{teamSlug}/events/{eventSlug}/emails/campaigns/{jobId}` that fetches job details from `GET /admin/.../bulk-emails/{id}`. The page SHALL display: job status, email type, trigger, the attendee recipient filter that scoped the send, job-owned subject/body if present, timestamps (created, started, completed), totals (recipient count, sent, failed, cancelled), and a list or count of failed recipients with their last error. A "Back to bulk emails" link SHALL navigate to the campaigns tab.

For jobs in a non-terminal state (Pending, Resolving, Sending), a "Cancel" button SHALL be shown. Clicking it SHALL call `POST /admin/.../bulk-emails/{id}/cancel`, show a success notification, and refresh the job status.

#### Scenario: Detail page shows job summary

- **WHEN** an organizer opens the detail page for a Completed bulk email job at the new URL
- **THEN** the page shows status "Completed", sent count, failed count, creation and completion timestamps

#### Scenario: Detail page shows the attendee filter

- **WHEN** an organizer opens the detail page for a job scoped by ticket type
- **THEN** the page shows the attendee recipient filter and does not offer an "External list" source descriptor

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

### Requirement: Send bulk email action uses a Sheet panel

The "Send bulk email" action SHALL be presented as a `Sheet` slide-in panel rather than a modal dialog. The Sheet SHALL slide from the right on desktop and from the bottom on mobile. The form SHALL collect custom bulk content directly: Subject, Text Body, HTML Body, and attendee recipient selection. It SHALL NOT require or allow selecting a stored template. Recipient selection SHALL target registered attendees only: the Sheet SHALL NOT offer CSV/file upload or any arbitrary/external recipient list input, and SHALL NOT parse recipient files client-side.

#### Scenario: Send bulk email opens as Sheet

- **WHEN** an organizer clicks "Send bulk email"
- **THEN** a Sheet panel slides in from the right (desktop) or bottom (mobile) with the send form

#### Scenario: Sheet collects direct content

- **WHEN** the Sheet opens
- **THEN** it shows required fields for Subject, Text Body, and HTML Body before or alongside recipient selection

#### Scenario: Recipient selection is attendee-only

- **WHEN** the Sheet opens
- **THEN** it presents attendee filter controls only and shows no CSV/file upload control or external recipient list input

#### Scenario: Template selection is absent

- **WHEN** the Sheet opens
- **THEN** it does not load or render a stored template selector

#### Scenario: Sheet closes on successful submission

- **WHEN** an organizer submits a valid bulk email form
- **THEN** the Sheet closes and the campaigns list refreshes
