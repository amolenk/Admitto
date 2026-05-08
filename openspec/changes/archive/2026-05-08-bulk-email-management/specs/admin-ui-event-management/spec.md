## ADDED Requirements

### Requirement: Event main sidebar includes a dedicated Bulk Emails entry

The Admin UI event sidebar (the persistent side-navigation shown when an organizer is viewing any page under `/teams/{teamSlug}/events/{eventSlug}/`) SHALL include an "Emails" entry that links to the Bulk Emails list page at `/teams/{teamSlug}/events/{eventSlug}/emails`. This entry SHALL be active when the current path is `/emails` or starts with `/emails/`. It SHALL NOT be active when the organizer is on settings sub-pages such as `/settings/email` or `/settings/email/templates`.

The event email settings page remains accessible via the Settings sidebar entry → Email sub-tab.

#### Scenario: Emails sidebar entry links to bulk emails list

- **WHEN** an organizer clicks the "Emails" entry in the event sidebar for event "devconf-2026"
- **THEN** the browser navigates to `/teams/acme/events/devconf-2026/emails`

#### Scenario: Emails entry is active on the bulk emails list page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/emails`
- **THEN** the "Emails" sidebar entry is rendered with the active style

#### Scenario: Emails entry is NOT active on the email settings page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/settings/email`
- **THEN** the "Emails" sidebar entry is NOT rendered as active; instead the "Settings" entry is active

#### Scenario: Emails entry is active on the bulk email detail page

- **WHEN** the current URL is `/teams/acme/events/devconf-2026/emails/some-job-id`
- **THEN** the "Emails" sidebar entry is rendered with the active style
