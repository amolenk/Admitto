# Attendee Registration — Delta

## MODIFIED Requirements

### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint by
providing their email, attendee info, selected ticket types, **and a valid
email-verification token proving ownership of the supplied email address**.
Self-service registrations SHALL enforce per-ticket-type capacity (ticket types
without an explicit capacity set SHALL be rejected as not available), the
registration window, and optional email domain restrictions.

The system SHALL reject self-service requests that omit the verification token
with reason "email verification required". The system SHALL reject self-service
requests whose token fails signature verification, has expired, or whose embedded
email does not match the supplied registration email, with reason "email
verification invalid". The verification check SHALL run before any event,
catalog, coupon, or ticket-type lookups so that token-related failures do not
leak information about other resources.

Whether registration is open SHALL be derived from the registration window
(`now ∈ [opensAt, closesAt)`) combined with the event's lifecycle status read from
the `TicketedEvent` aggregate (see event-management). There is no separate stored
registration-status. Application handlers SHALL load the `TicketedEvent` to validate
window, domain, and active-status invariants, then atomically claim ticket capacity
on the `TicketCatalog`. The atomic claim SHALL also be guarded by
`TicketCatalog.EventStatus` so that a concurrent cancel/archive cannot leak
through after the policy check; an `EventStatus` of Cancelled or Archived at
claim time SHALL fail the registration with reason "event not active" (the EF
optimistic concurrency token on `TicketCatalog` is the safety net).

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on event "DevConf" with capacity 100 (50 used), `TicketedEvent.Status` Active, `TicketCatalog.EventStatus` Active, window "2025-01-01T00:00Z" / "2025-06-01T00:00Z" at current time "2025-03-15T12:00Z", no domain restriction, and a valid verification token bound to "dave@example.com"
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Self-service rejected — verification token missing
- **WHEN** an attendee self-registers without supplying a verification token
- **THEN** the registration is rejected with reason "email verification required" and no event, catalog, or capacity lookup is performed

#### Scenario: Self-service rejected — verification token invalid
- **WHEN** an attendee self-registers with a token that fails signature verification, has expired, or is bound to a different email than the registration email
- **THEN** the registration is rejected with reason "email verification invalid"

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
- **WHEN** an attendee self-registers as "employee@acme.com" for event "CorpConf" which is restricted to "@acme.com", the window is open, and a valid verification token bound to "employee@acme.com"
- **THEN** a registration is created for "employee@acme.com"

#### Scenario: Concurrent cancel detected at claim time
- **WHEN** an attendee self-registers and `TicketedEvent.Status` is Active at policy-check time but `TicketCatalog.EventStatus` has been transitioned to Cancelled by an in-flight cancel before the claim commits
- **THEN** the registration fails with reason "event not active" and no capacity is consumed

---

### Requirement: Attendee can register using a coupon code
The system SHALL allow attendees to register using a valid, unexpired, single-use
coupon code via a dedicated public endpoint. Coupon-based registrations SHALL
restrict ticket type selection to the coupon's allowlisted types.
**Coupon-based registrations SHALL reject any request whose supplied email does
not match `coupon.TargetEmail`, with reason "coupon email mismatch".** This
binds the bearer credential (the coupon code, delivered to the target email) to
the address it was issued for, without requiring a separate verification token.

Coupon-based registrations SHALL bypass capacity enforcement, email domain
restrictions, and the requirement for a capacity to be set. If the coupon has
`bypassRegistrationWindow` set, the registration SHALL also bypass the
registration window. Upon successful registration, the system SHALL mark the
coupon as redeemed. The used capacity counter SHALL be incremented for each
ticket type regardless of bypass. Coupon registrations SHALL NOT bypass the
active-status gate: registrations are rejected when `TicketedEvent.Status` is
Cancelled or Archived (with the `TicketCatalog.EventStatus` claim-time check as
the safety net for concurrent transitions). Coupon registrations SHALL NOT
require an email-verification token.

#### Scenario: Successful coupon registration
- **WHEN** an attendee registers as "speaker@gmail.com" using valid coupon "INVITE-001" issued to "speaker@gmail.com" for "Speaker Pass" on event "DevConf" where capacity is 5/5 used and the window is open
- **THEN** a registration is created for "speaker@gmail.com", coupon "INVITE-001" is marked as redeemed, and capacity used increases to 6

#### Scenario: Coupon rejected — email does not match coupon target
- **WHEN** an attendee registers as "attacker@gmail.com" using valid coupon "INVITE-001" that was issued to "speaker@gmail.com"
- **THEN** the registration is rejected with reason "coupon email mismatch" and the coupon remains unredeemed

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
- **WHEN** an attendee registers using valid coupon "INVITE-006" with bypassRegistrationWindow enabled for an event whose window has closed, and the supplied email matches the coupon target
- **THEN** a registration is created

#### Scenario: Coupon respects registration window when flag not set
- **WHEN** an attendee registers using valid coupon "INVITE-007" with bypassRegistrationWindow disabled for an event whose window has closed
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Coupon bypasses domain restriction
- **WHEN** an attendee registers as "external@gmail.com" using valid coupon "INVITE-008" issued to "external@gmail.com" for event "CorpConf" which is restricted to "@acme.com"
- **THEN** a registration is created for "external@gmail.com"

#### Scenario: Coupon bypasses capacity requirement
- **WHEN** an attendee registers using valid coupon "INVITE-009" for "Speaker Pass" which has no capacity configured, with email matching the coupon target
- **THEN** a registration is created

#### Scenario: Coupon does not bypass cancelled/archived event
- **WHEN** an attendee registers using valid coupon "INVITE-010" for an event with `TicketedEvent.Status` Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Coupon does not require an email-verification token
- **WHEN** an attendee registers using valid coupon "INVITE-011" with email matching the coupon target and no verification token supplied
- **THEN** a registration is created
