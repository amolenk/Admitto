## ADDED Requirements

### Requirement: Admin can manage the registration policy from the UI

The Admin UI SHALL provide a "Registration Policy" page for a ticketed event with a form for the registration window (opens-at and closes-at) and an optional email-domain restriction. The form SHALL be pre-filled with the current policy values. The form SHALL submit the event's current policy version for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values. The page SHALL NOT display any "Open registration" / "Close registration" controls or any registration-status toggle — registration openness is derived from the window.

When the event's lifecycle status is Cancelled or Archived, the form SHALL be read-only and SHALL display a banner indicating the event is not active.

#### Scenario: Configure the registration window
- **WHEN** an organizer of team "acme" opens the Registration Policy page for event "DevConf" and sets the window to "2025-01-01T00:00Z" / "2025-06-01T00:00Z" and submits
- **THEN** the policy is saved and the UI shows a success message

#### Scenario: Configure an email-domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com" and submits
- **THEN** the policy is saved and self-service registrations for "CorpConf" are restricted to "@acme.com"

#### Scenario: No Open/Close controls on the page
- **WHEN** an organizer views the Registration Policy page for any event
- **THEN** the page displays no "Open registration" or "Close registration" buttons or any registration-status toggle

#### Scenario: Form is read-only for cancelled events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose lifecycle status is Cancelled
- **THEN** the form fields are disabled and a banner indicates the event is cancelled

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the Registration Policy form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page

---

### Requirement: Admin can manage the cancellation policy from the UI

The Admin UI SHALL provide a "Cancellation Policy" page for a ticketed event with a form for a single late-cancellation cutoff datetime. The field SHALL be clearable (removing the policy entirely). The form SHALL be pre-filled with the current value or shown empty when no policy is configured. On success the UI SHALL show a confirmation message.

When the event's lifecycle status is Cancelled or Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the late-cancellation cutoff
- **WHEN** an organizer opens the Cancellation Policy page for event "DevConf" and sets the cutoff to "2025-05-25T00:00Z" and submits
- **THEN** the cancellation policy is saved and the UI shows a success message

#### Scenario: Remove the cancellation policy
- **WHEN** an organizer opens the Cancellation Policy page for event "DevConf" which has cutoff "2025-05-25T00:00Z", clears the field, and submits
- **THEN** the cancellation policy is removed and the page displays no configured policy

#### Scenario: Form is read-only for archived events
- **WHEN** an organizer opens the Cancellation Policy page for event "OldConf" whose lifecycle status is Archived
- **THEN** the form fields are disabled and a banner indicates the event is archived

---

### Requirement: Admin can manage the reconfirm policy from the UI

The Admin UI SHALL provide a "Reconfirmation Policy" page for a ticketed event with a form for the reconfirmation window (opens-at and closes-at) and a cadence expressed in days. The form SHALL support removing the policy entirely. The form SHALL validate client-side that close is after open and cadence is a positive integer ≥ 1. Server-side validation errors SHALL be displayed inline. On success the UI SHALL show a confirmation message.

When the event's lifecycle status is Cancelled or Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the reconfirm policy
- **WHEN** an organizer sets the reconfirm window to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" and cadence to 7 days and submits
- **THEN** the reconfirm policy is saved and the UI shows a success message

#### Scenario: Remove the reconfirm policy
- **WHEN** an organizer opens the Reconfirmation Policy page for event "DevConf" which has a policy configured and chooses to remove it
- **THEN** the reconfirm policy is removed and the page displays no configured policy

#### Scenario: Validation error — close before open
- **WHEN** an organizer submits a reconfirm policy with close datetime before the open datetime
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive cadence
- **WHEN** an organizer submits a reconfirm policy with cadence 0
- **THEN** the form displays a validation error without calling the backend

---

### Requirement: Event policy pages are reachable from the sidebar

The Admin UI SHALL expose the Registration Policy, Cancellation Policy, and Reconfirmation Policy pages from the event detail sidebar under a "Policies" section. The event detail header SHALL display the current lifecycle status (Active / Cancelled / Archived).

#### Scenario: Navigate to all three policy pages
- **WHEN** an organizer opens the event detail view for event "DevConf"
- **THEN** the sidebar shows a "Policies" section containing links to "Registration", "Cancellation", and "Reconfirmation"

#### Scenario: Event header shows lifecycle status
- **WHEN** an organizer views event "DevConf" whose lifecycle status is Cancelled
- **THEN** the event detail header shows a badge or label indicating the event is Cancelled
