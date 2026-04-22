## MODIFIED Requirements

### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint by
providing their email, attendee info, and selected ticket types. Self-service
registrations SHALL enforce per-ticket-type capacity (ticket types without an
explicit capacity set SHALL be rejected as not available), the registration window,
and optional email domain restrictions.

Whether registration is open SHALL be derived from the registration window
(`now ∈ [opensAt, closesAt)`) combined with the event's lifecycle status read from
the `TicketedEventLifecycleGuard` (see event-lifecycle-guard). There is no separate
stored registration-status.

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on event "DevConf" with capacity 100 (50 used), lifecycle guard Active, window "2025-01-01T00:00Z" / "2025-06-01T00:00Z" at current time "2025-03-15T12:00Z", and no domain restriction
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Self-service rejected — capacity full
- **WHEN** an attendee self-registers for "Workshop" where capacity is 20/20 used and the window is open
- **THEN** the registration is rejected with reason "ticket type at capacity"

#### Scenario: Self-service rejected — ticket type has no capacity set
- **WHEN** an attendee self-registers for "Speaker Pass" which has no capacity configured
- **THEN** the registration is rejected with reason "ticket type not available"

#### Scenario: Self-service rejected — before registration window opens
- **WHEN** an attendee self-registers for an event whose registration window opens tomorrow
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — after registration window closes
- **WHEN** an attendee self-registers for an event whose registration window closed yesterday
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Self-service rejected — no registration window configured
- **WHEN** an attendee self-registers for an event with no registration window configured
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — email domain mismatch
- **WHEN** an attendee self-registers as "outsider@gmail.com" for event "CorpConf" which is restricted to "@acme.com" and the window is open
- **THEN** the registration is rejected with reason "email domain not allowed"

#### Scenario: Self-service allowed — email domain matches
- **WHEN** an attendee self-registers as "employee@acme.com" for event "CorpConf" which is restricted to "@acme.com" and the window is open
- **THEN** a registration is created for "employee@acme.com"

---

### Requirement: Ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration.
The system SHALL reject registrations with duplicate ticket types in the selection.
The system SHALL reject registrations referencing non-existent or cancelled ticket
types. The system SHALL reject registrations where selected ticket types have
overlapping time slots. The system SHALL reject all registrations when the
`TicketedEventLifecycleGuard` status for the event is Cancelled or Archived. The
system SHALL reject registrations if the email address is already registered for
the same event.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) on event "DevConf" with an open window and Active lifecycle guard
- **THEN** a registration is created with both ticket types, "General Admission" capacity used increases to 51, and "Workshop A" capacity used increases to 11

#### Scenario: Rejected — duplicate ticket types in selection
- **WHEN** an attendee registers selecting "General Admission" twice
- **THEN** the registration is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — non-existent ticket type
- **WHEN** an attendee registers selecting ticket type "Premium VIP" which does not exist on the event
- **THEN** the registration is rejected with reason "unknown ticket type"

#### Scenario: Rejected — cancelled ticket type
- **WHEN** an attendee registers selecting "Workshop A" which has been cancelled
- **THEN** the registration is rejected with reason "ticket type cancelled"

#### Scenario: Rejected — overlapping time slots
- **WHEN** an attendee registers selecting both "Workshop A" (slot "morning") and "Workshop B" (slot "morning") which share a time slot
- **THEN** the registration is rejected with reason "overlapping time slots"

#### Scenario: Rejected — lifecycle guard status is Cancelled
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEventLifecycleGuard` status is Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — lifecycle guard status is Archived
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEventLifecycleGuard` status is Archived
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — duplicate email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to register again
- **THEN** the registration is rejected with reason "already registered"
