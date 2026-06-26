## MODIFIED Requirements

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

### Requirement: Unified Email tabbed page groups all email concerns
The Admin UI SHALL render a unified Email page at `/teams/{teamSlug}/events/{eventSlug}/emails` that presents the event email concerns that remain after simplification. The **Campaigns** tab SHALL be the default. Navigating to the bare `/emails` path SHALL redirect to `/emails/campaigns`. Template and event SMTP setup tabs SHALL NOT be shown because transactional templates are not editable and SMTP settings are team-scoped.

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
