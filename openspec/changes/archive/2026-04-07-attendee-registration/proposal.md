## Purpose

Attendees register for events through two pathways: self-service registration (subject to capacity, window, and domain rules), and invite-based registration (via coupon code with capacity and domain bypass). Organizers use the invite flow to register attendees on their behalf.

> **Scope note:** The `IsSelfService` and `IsSelfServiceAvailable` flags on `TicketType` are made obsolete by this change and SHALL be removed from the Organization module as part of implementation.

## Requirements

### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint by
providing their email, attendee info, and selected ticket types. Self-service
registrations SHALL enforce per-ticket-type capacity, the registration window, and
optional email domain restrictions.

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on active event "DevConf" with capacity 100 (50 used), an open window, and no domain restriction
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Self-service rejected — capacity full
- **WHEN** an attendee self-registers for "Workshop" where capacity is 20/20 used and the window is open
- **THEN** the registration is rejected with reason "ticket type at capacity"

#### Scenario: Self-service rejected — before registration window opens
- **WHEN** an attendee self-registers for an event whose registration window opens tomorrow
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — after registration window closes
- **WHEN** an attendee self-registers for an event whose registration window closed yesterday
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Self-service rejected — email domain mismatch
- **WHEN** an attendee self-registers as "outsider@gmail.com" for event "CorpConf" which is restricted to "@acme.com" and the window is open
- **THEN** the registration is rejected with reason "email domain not allowed"

#### Scenario: Self-service allowed — email domain matches
- **WHEN** an attendee self-registers as "employee@acme.com" for event "CorpConf" which is restricted to "@acme.com" and the window is open
- **THEN** a registration is created for "employee@acme.com"

#### Scenario: Self-service rejected — no registration window configured
- **WHEN** an attendee self-registers for an event with no registration window configured
- **THEN** the registration is rejected with reason "registration not open"

---

### Requirement: Attendee can register using a coupon code (invite-based registration)
The system SHALL allow attendees to register using a valid, unexpired, single-use
coupon code via a dedicated public endpoint. Coupon-based registrations SHALL restrict
ticket type selection to the coupon's allowlisted types. Coupon-based registrations
SHALL bypass capacity enforcement and email domain restrictions. If the coupon has `bypassRegistrationWindow`
set, the registration SHALL also bypass the registration window. Upon successful
registration, the system SHALL mark the coupon as redeemed.

#### Scenario: Successful coupon registration
- **WHEN** an attendee registers as "speaker@gmail.com" using valid coupon "INVITE-001" for "Speaker Pass" on event "DevConf" where capacity is 5/5 used and the window is open
- **THEN** a registration is created for "speaker@gmail.com", coupon "INVITE-001" is marked as redeemed, and capacity used increases to 6

#### Scenario: Coupon rejected — expired
- **WHEN** an attendee registers using coupon "INVITE-002" that expired yesterday
- **THEN** the registration is rejected with reason "coupon expired"

#### Scenario: Coupon rejected — already redeemed
- **WHEN** an attendee registers using coupon "INVITE-003" that has already been redeemed
- **THEN** the registration is rejected with reason "coupon already used"

#### Scenario: Coupon rejected — ticket type not allowlisted
- **WHEN** an attendee registers using coupon "INVITE-004" (allowlisting only "Speaker Pass") for "General Admission"
- **THEN** the registration is rejected with reason "ticket type not allowed for this coupon"

#### Scenario: Coupon bypasses registration window when flag set
- **WHEN** an attendee registers using valid coupon "INVITE-005" with bypassRegistrationWindow enabled for an event whose window has closed
- **THEN** a registration is created

#### Scenario: Coupon respects registration window when flag not set
- **WHEN** an attendee registers using valid coupon "INVITE-006" with bypassRegistrationWindow disabled for an event whose window has closed
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Coupon bypasses domain restriction
- **WHEN** an attendee registers as "external@gmail.com" using valid coupon "INVITE-007" for event "CorpConf" which is restricted to "@acme.com"
- **THEN** a registration is created for "external@gmail.com"

---

### Requirement: Common ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration.
The system SHALL reject registrations with duplicate ticket types in the selection.
The system SHALL reject registrations referencing non-existent or cancelled ticket
types. The system SHALL reject registrations where selected ticket types have
overlapping time slots. The system SHALL reject all registrations for cancelled or
archived events. The system SHALL reject registrations if the email address is
already registered for the same event.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) on event "DevConf" with an open window
- **THEN** a registration is created with both ticket types, "General Admission" capacity used increases to 51, and "Workshop A" capacity used increases to 11

#### Scenario: Rejected — duplicate ticket types in selection
- **WHEN** an attendee registers selecting "General Admission" twice
- **THEN** the registration is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — non-existent ticket type
- **WHEN** an attendee registers selecting ticket type "Premium VIP" which does not exist
- **THEN** the registration is rejected with reason "unknown ticket type"

#### Scenario: Rejected — cancelled ticket type
- **WHEN** an attendee registers selecting "Workshop A" which has been cancelled
- **THEN** the registration is rejected with reason "ticket type cancelled"

#### Scenario: Rejected — overlapping time slots
- **WHEN** an attendee registers selecting both "Workshop A" (09:00–11:00) and "Workshop B" (10:00–12:00) which overlap
- **THEN** the registration is rejected with reason "overlapping time slots"

#### Scenario: Rejected — cancelled event
- **WHEN** an attendee attempts to register for event "OldConf" which has been cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — duplicate email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to register again
- **THEN** the registration is rejected with reason "already registered"

---

### Requirement: Organizer can configure a registration window
The system SHALL allow organizers to configure a registration window (open and close
datetimes) for an event. This configuration is stored in the Registrations module
(not the Organization module) as part of the event's registration policy. Self-service
registrations outside the window SHALL be rejected.

#### Scenario: Configure registration window
- **WHEN** an organizer sets the registration window for event "DevConf" from "2025-01-01T00:00Z" to "2025-06-01T00:00Z"
- **THEN** the registration window is saved for "DevConf"

---

### Requirement: Organizer can configure an email domain restriction
The system SHALL allow organizers to configure an optional email domain restriction
for an event (single domain pattern). This configuration is stored in the Registrations
module as part of the event's registration policy. Self-service registrations from
non-matching domains SHALL be rejected.

#### Scenario: Configure email domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com"
- **THEN** self-service registrations for "CorpConf" are restricted to "@acme.com" emails

#### Scenario: Remove email domain restriction
- **WHEN** an organizer removes the email domain restriction from event "CorpConf" which was restricted to "@acme.com"
- **THEN** self-service registrations for "CorpConf" accept any email domain

---

### Requirement: Capacity tracking is synchronized with ticket type changes
The system SHALL automatically initialize and update per-ticket-type capacity in
the Registrations module when ticket types are created or modified in the
Organization module. The Organization module SHALL publish a `TicketTypeAddedModuleEvent`
when a ticket type is added, and a `TicketTypeCapacityChangedModuleEvent` when its
capacity is updated. The Registrations module SHALL handle these events to keep its
own capacity records current.

> **Note:** The Organization facade (`IOrganizationFacade`) also needs to expose time
> slot and capacity data per ticket type (in addition to the existing `Slug`, `Name`,
> `IsCancelled` fields) so the Registrations module can perform overlap checks and
> apply correct capacity values at registration time.

#### Scenario: Capacity initialized when ticket type is created
- **WHEN** a ticket type "General Admission" with capacity 100 is added to event "DevConf" in the Organization module
- **THEN** the Registrations module initializes capacity tracking for "General Admission" with max capacity 100 and 0 used

#### Scenario: Capacity updated when ticket type capacity changes
- **WHEN** the Organization module updates "General Admission" capacity from 100 to 150 and capacity tracking shows 50 used
- **THEN** the Registrations module updates max capacity for "General Admission" to 150 with the used count unchanged
