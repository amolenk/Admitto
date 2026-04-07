# Attendee Registration Specification

### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint by
providing their email, attendee info, and selected ticket types. Self-service
registrations SHALL enforce per-ticket-type capacity (ticket types without an
explicit capacity set SHALL be rejected as not available), the registration window,
and optional email domain restrictions.

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on active event "DevConf" with capacity 100 (50 used), an open registration window, and no domain restriction
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

### Requirement: Attendee can register using a coupon code
The system SHALL allow attendees to register using a valid, unexpired, single-use
coupon code via a dedicated public endpoint. Coupon-based registrations SHALL
restrict ticket type selection to the coupon's allowlisted types. Coupon-based
registrations SHALL bypass capacity enforcement, email domain restrictions, and the
requirement for a capacity to be set. If the coupon has `bypassRegistrationWindow`
set, the registration SHALL also bypass the registration window. Upon successful
registration, the system SHALL mark the coupon as redeemed. The used capacity
counter SHALL be incremented for each ticket type regardless of bypass.

#### Scenario: Successful coupon registration
- **WHEN** an attendee registers as "speaker@gmail.com" using valid coupon "INVITE-001" for "Speaker Pass" on event "DevConf" where capacity is 5/5 used and the window is open
- **THEN** a registration is created for "speaker@gmail.com", coupon "INVITE-001" is marked as redeemed, and capacity used increases to 6

#### Scenario: Coupon rejected — expired
- **WHEN** an attendee registers using coupon "INVITE-002" that expired yesterday
- **THEN** the registration is rejected with reason "coupon expired"

#### Scenario: Coupon rejected — already redeemed
- **WHEN** an attendee registers using coupon "INVITE-003" that has already been redeemed
- **THEN** the registration is rejected with reason "coupon already used"

#### Scenario: Coupon rejected — revoked
- **WHEN** an attendee registers using coupon "INVITE-004" that has been revoked
- **THEN** the registration is rejected with reason "coupon revoked"

#### Scenario: Coupon rejected — ticket type not allowlisted
- **WHEN** an attendee registers using coupon "INVITE-005" (allowlisting only "Speaker Pass") for "General Admission"
- **THEN** the registration is rejected with reason "ticket type not allowed for this coupon"

#### Scenario: Coupon bypasses registration window when flag set
- **WHEN** an attendee registers using valid coupon "INVITE-006" with bypassRegistrationWindow enabled for an event whose window has closed
- **THEN** a registration is created

#### Scenario: Coupon respects registration window when flag not set
- **WHEN** an attendee registers using valid coupon "INVITE-007" with bypassRegistrationWindow disabled for an event whose window has closed
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Coupon bypasses domain restriction
- **WHEN** an attendee registers as "external@gmail.com" using valid coupon "INVITE-008" for event "CorpConf" which is restricted to "@acme.com"
- **THEN** a registration is created for "external@gmail.com"

#### Scenario: Coupon bypasses capacity requirement
- **WHEN** an attendee registers using valid coupon "INVITE-009" for "Speaker Pass" which has no capacity configured
- **THEN** a registration is created

---

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
