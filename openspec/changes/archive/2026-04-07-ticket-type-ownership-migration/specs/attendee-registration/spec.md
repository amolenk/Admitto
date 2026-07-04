## MODIFIED Requirements

### Requirement: Ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration.
The system SHALL reject registrations with duplicate ticket types in the selection.
The system SHALL reject registrations referencing non-existent or cancelled ticket
types. The system SHALL reject registrations where selected ticket types have
overlapping time slots. The system SHALL reject all registrations when the event
lifecycle status is Cancelled or Archived. The system SHALL reject registrations if
the email address is already registered for the same event.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) on event "DevConf" with an open window
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

#### Scenario: Rejected — event lifecycle status is Cancelled
- **WHEN** an attendee attempts to register for event "OldConf" whose lifecycle status is Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — event lifecycle status is Archived
- **WHEN** an attendee attempts to register for event "OldConf" whose lifecycle status is Archived
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — duplicate email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to register again
- **THEN** the registration is rejected with reason "already registered"
