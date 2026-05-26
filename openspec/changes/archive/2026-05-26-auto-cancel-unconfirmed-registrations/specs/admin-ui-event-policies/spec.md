## MODIFIED Requirements

### Requirement: Admin can manage the reconfirm policy from the UI

The Admin UI SHALL provide a "Reconfirmation Policy" page for a ticketed event with a form for:
- the reconfirmation window (opens-at and closes-at),
- a cadence expressed in days,
- a **minimum email interval** expressed in hours — the minimum time that must pass since the attendee's last reconfirmation email (or their registration, whichever is more recent) before the system will send them another reconfirmation prompt,
- an **auto-cancel unreconfirmed registrations** toggle (`AutoCancelEnabled`), and
- a **max reconfirmation attempts** number input (positive integer, shown and required only when the auto-cancel toggle is on) — the number of unanswered reconfirmation emails after which a registration is automatically cancelled.

The form SHALL support removing the policy entirely. The form SHALL validate client-side that:
- close is after open,
- cadence is a positive integer ≥ 1,
- minimum email interval is a positive integer ≥ 1, and
- when the auto-cancel toggle is on, max reconfirmation attempts is a positive integer ≥ 1.

The max reconfirmation attempts input SHALL be hidden and its validation SHALL be skipped when the auto-cancel toggle is off.

Server-side validation errors SHALL be displayed inline. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message.

When `TicketedEvent.Status` is Archived, the form SHALL be read-only with an explanatory banner.

#### Scenario: Configure the reconfirm policy without auto-cancel

- **WHEN** an organizer sets the reconfirm window to "2025-05-01T00:00Z" / "2025-05-25T00:00Z", cadence to 7 days, minimum email interval to 48 hours, and leaves the auto-cancel toggle off, and submits
- **THEN** the reconfirm policy is saved with `AutoCancelEnabled=false` and the UI shows a success message

#### Scenario: Configure the reconfirm policy with auto-cancel enabled

- **WHEN** an organizer sets the reconfirm policy and turns on the auto-cancel toggle and sets max reconfirmation attempts to 3, and submits
- **THEN** the policy is saved with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3` and the UI shows a success message

#### Scenario: Max attempts input is hidden when auto-cancel toggle is off

- **WHEN** an organizer opens the Reconfirmation Policy page and the auto-cancel toggle is off
- **THEN** the max reconfirmation attempts input is not visible on the page

#### Scenario: Max attempts input appears when auto-cancel toggle is turned on

- **WHEN** an organizer toggles the auto-cancel toggle on
- **THEN** the max reconfirmation attempts input becomes visible and is required

#### Scenario: Remove the reconfirm policy

- **WHEN** an organizer opens the Reconfirmation Policy page for event "DevConf" which has a policy configured and chooses to remove it
- **THEN** the reconfirm policy is removed and the page displays no configured policy

#### Scenario: Validation error — close before open

- **WHEN** an organizer submits a reconfirm policy with close datetime before the open datetime
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive cadence

- **WHEN** an organizer submits a reconfirm policy with cadence 0
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive minimum email interval

- **WHEN** an organizer submits a reconfirm policy with minimum email interval 0
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — max attempts required when auto-cancel is on

- **WHEN** an organizer enables the auto-cancel toggle and submits without providing a max reconfirmation attempts value
- **THEN** the form displays a validation error without calling the backend

#### Scenario: Validation error — non-positive max attempts

- **WHEN** an organizer enables the auto-cancel toggle, sets max reconfirmation attempts to 0, and submits
- **THEN** the form displays a validation error without calling the backend
