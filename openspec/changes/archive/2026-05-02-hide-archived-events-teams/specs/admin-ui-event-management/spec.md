## ADDED Requirements

### Requirement: Events list page excludes archived events and reflects archive action immediately
The Admin UI events list page SHALL only display non-archived events (active and
cancelled). When an organizer archives an event via any archive action available
in the UI, the archived event SHALL be removed from the events list immediately
upon a successful archive response — without requiring a page reload or manual
navigation.

#### Scenario: Archived events are not shown on the events list page
- **WHEN** an organizer navigates to the events list page for team "acme" and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are visible in the list and "conf-2025" is not shown

#### Scenario: Archived event disappears immediately after archive action
- **WHEN** an organizer archives event "conf-2025" from the UI and the archive request succeeds
- **THEN** "conf-2025" is removed from the events list immediately without a full page reload
