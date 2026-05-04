## MODIFIED Requirements

### Requirement: Ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration.
The system SHALL reject registrations with duplicate ticket types in the selection.
The system SHALL reject registrations referencing non-existent or cancelled ticket
types. The system SHALL reject registrations where selected ticket types have
overlapping time slots. The system SHALL reject all registrations when the
`TicketedEvent.Status` for the event is Cancelled or Archived (and as a
consistency safety net, the atomic claim against `TicketCatalog` rejects when
`TicketCatalog.EventStatus` is Cancelled or Archived).

The system SHALL keep one registration identity per event/email. If the email
address already belongs to a `Registered` registration for the same event, the
registration request SHALL be rejected with reason "already registered". If the
email address belongs to a `Cancelled` registration for the same event, a
successful registration request SHALL reset that existing `Registration` instead
of creating a new row. Resetting SHALL preserve the existing `RegistrationId`,
transition the status to `Registered`, clear cancellation metadata, clear
reconfirmation state, replace attendee details, replace ticket snapshots, replace
additional details, claim capacity according to the active registration path, and
produce the same attendee-registered side effects as a newly created
registration.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) on event "DevConf" with an open window and `TicketedEvent.Status` Active
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

#### Scenario: Rejected — TicketedEvent status is Cancelled
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — TicketedEvent status is Archived
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Archived
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — TicketCatalog.EventStatus catches concurrent transition
- **WHEN** policy checks pass against `TicketedEvent` (Active) but the event is cancelled before the claim commits, so `TicketCatalog.EventStatus` is Cancelled at claim time
- **THEN** the registration is rejected with reason "event not active" and no capacity is consumed

#### Scenario: Rejected — duplicate active email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to register again
- **THEN** the registration is rejected with reason "already registered"

#### Scenario: Self-service resets a cancelled registration
- **WHEN** "alice@example.com" has a `Cancelled` registration for event "DevConf" and self-registers again with a valid verification token, open window, valid ticket selection, and valid additional details
- **THEN** the existing registration is reset to `Registered`, its original registration id is returned, its attendee details, ticket snapshots, and additional details match the new request, its cancellation metadata and reconfirmation state are cleared, capacity is claimed, and attendee-registered side effects are produced

#### Scenario: Coupon registration resets a cancelled registration
- **WHEN** "speaker@gmail.com" has a `Cancelled` registration for event "DevConf" and registers with a valid coupon issued to "speaker@gmail.com"
- **THEN** the existing registration is reset to `Registered`, its original registration id is returned, capacity is claimed using coupon bypass rules, the coupon is marked redeemed, cancellation metadata and reconfirmation state are cleared, and attendee-registered side effects are produced

#### Scenario: Reset is not applied when self-service gates fail
- **WHEN** "alice@example.com" has a `Cancelled` registration for event "DevConf" and self-registers again after the registration window has closed
- **THEN** the request is rejected with reason "registration closed", the existing registration remains `Cancelled`, and no capacity is consumed

