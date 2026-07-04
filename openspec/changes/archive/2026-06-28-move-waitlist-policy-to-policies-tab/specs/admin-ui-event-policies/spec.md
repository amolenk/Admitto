## ADDED Requirements

### Requirement: Admin can manage the waitlist policy from the UI

The Admin UI SHALL provide a Waitlist policy section on the event Policies tab. The section SHALL show the event-wide waitlist quiet hours (`QuietHoursStart` and `QuietHoursEnd`) from the event's `WaitlistPolicy`. The form SHALL explain that quiet hours extend waitlist offer claim deadlines and do not delay notification emails. The form SHALL submit the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL refresh the displayed values.

The Admin UI SHALL NOT show waitlist quiet-hours controls on the General event settings form.

When `TicketedEvent.Status` is Archived, the Waitlist policy form SHALL be read-only and SHALL display the existing archived-event policy banner.

#### Scenario: Configure waitlist quiet hours
- **WHEN** an organizer opens the Policies tab for active event "DevConf", changes waitlist quiet hours to `23:00` / `07:00`, and submits
- **THEN** the waitlist policy is saved with `QuietHoursStart = 23:00` and `QuietHoursEnd = 07:00`, and the displayed event details are refreshed

#### Scenario: General settings does not show waitlist quiet hours
- **WHEN** an organizer opens the General event settings form
- **THEN** the form does not display `QuietHoursStart`, `QuietHoursEnd`, or waitlist quiet-hours controls

#### Scenario: Waitlist policy copy explains notification behavior
- **WHEN** an organizer views the Waitlist policy section
- **THEN** the UI states that waitlist notifications are sent immediately and quiet hours extend the claim deadline

#### Scenario: Waitlist policy form is read-only for archived events
- **WHEN** an organizer opens the Policies tab for event "DevConf" whose `TicketedEvent.Status` is Archived
- **THEN** the Waitlist policy fields are disabled and the archived-event policy banner is displayed

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the Waitlist policy form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page
