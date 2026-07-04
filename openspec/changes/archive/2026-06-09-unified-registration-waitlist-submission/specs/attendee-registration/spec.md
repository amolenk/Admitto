## MODIFIED Requirements

### Requirement: Attendee can self-register
The system SHALL allow attendees to submit an explicit public registration request after proving ownership of the supplied email address with a valid email-verification token. The request SHALL distinguish ticket types the attendee wants to register for now from ticket types whose waitlists the attendee wants to join.

Self-service registration SHALL enforce per-ticket-type capacity, registration window, optional email-domain restrictions, and self-service availability for `registerTicketTypeIds`. Ticket types without an explicit capacity set SHALL be rejected as not available when requested for registration. Ticket types in WaitlistOnly mode SHALL be rejected when requested for registration.

Ticket types in `waitlistTicketTypeIds` SHALL be validated as waitlist joins: the ticket type MUST exist, MUST be self-service visible, MUST have `WaitlistEnabled = true`, and MUST currently have `WaitlistMode = true`. Waitlist entries SHALL be created in the same transaction as any registration created by the same request. If any requested registration or waitlist action cannot be applied exactly as requested, the system SHALL reject the whole request and persist none of the requested changes.

The system SHALL reject self-service requests that omit the verification token with reason "email verification required". The system SHALL reject self-service requests whose token fails signature verification, has expired, or whose embedded email does not match the supplied registration email, with reason "email verification invalid". The verification check SHALL run before any event, catalog, coupon, waitlist, or ticket-type lookups so that token-related failures do not leak information about other resources.

Whether registration is open SHALL be derived from the registration window (`now in [opensAt, closesAt)`) combined with the event's lifecycle status read from the `TicketedEvent` aggregate (see event-management). There is no separate stored registration-status. Application handlers SHALL load the `TicketedEvent` to validate window, domain, and active-status invariants, then atomically claim registered ticket capacity on the `TicketCatalog`. The atomic claim SHALL also be guarded by `TicketCatalog.EventStatus` so that a concurrent archive cannot leak through after the policy check; an archived `EventStatus` at claim time SHALL fail the registration with reason "event not active" (the EF optimistic concurrency token on `TicketCatalog` is the safety net).

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for registration ticket "General Admission" on event "DevConf" with capacity 100 (50 used), `TicketedEvent.Status` Active, `TicketCatalog.EventStatus` Active, window "2025-01-01T00:00Z" / "2025-06-01T00:00Z" at current time "2025-03-15T12:00Z", no domain restriction, and a valid verification token bound to "dave@example.com"
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Successful mixed registration and waitlist submission
- **WHEN** an attendee submits `registerTicketTypeIds = [General Admission]` and `waitlistTicketTypeIds = [Workshop B]`, where General Admission is available and Workshop B has `WaitlistEnabled = true` and `WaitlistMode = true`
- **THEN** a registration is created for General Admission, a waitlist entry is created for Workshop B, and both changes are committed atomically

#### Scenario: Waitlist-only submission succeeds without registration
- **WHEN** an attendee submits no registration tickets and `waitlistTicketTypeIds = [Workshop B]`, where Workshop B has `WaitlistEnabled = true` and `WaitlistMode = true`
- **THEN** no registration is created, a waitlist entry is created for Workshop B, and the response identifies that there is no registration id

#### Scenario: Self-service rejected — verification token missing
- **WHEN** an attendee self-registers without supplying a verification token
- **THEN** the registration is rejected with reason "email verification required" and no event, catalog, waitlist, or capacity lookup is performed

#### Scenario: Self-service rejected — verification token invalid
- **WHEN** an attendee self-registers with a token that fails signature verification, has expired, or is bound to a different email than the registration email
- **THEN** the registration is rejected with reason "email verification invalid"

#### Scenario: Self-service rejected — capacity full for registration ticket
- **WHEN** an attendee requests `registerTicketTypeIds = [Workshop]` where Workshop capacity is 20/20 used and the window is open
- **THEN** the request is rejected with reason "ticket type at capacity" and no registration or waitlist entry is created

#### Scenario: Self-service rejected — requested registration ticket is in WaitlistOnly mode
- **WHEN** an attendee requests `registerTicketTypeIds = [General Admission]` and General Admission has `WaitlistMode = true`
- **THEN** the request is rejected with reason "ticket type in waitlist mode" and no registration or waitlist entry is created

#### Scenario: Self-service rejected — requested waitlist ticket is no longer in WaitlistOnly mode
- **WHEN** an attendee requests `registerTicketTypeIds = [Workshop A]` and `waitlistTicketTypeIds = [Workshop B]`, but Workshop B has left WaitlistOnly mode by submission time
- **THEN** the request is rejected with a stale ticket-state conflict and no registration or waitlist entry is created

#### Scenario: Coupon-based registration bypasses WaitlistOnly mode check
- **WHEN** an attendee submits a registration with a valid waitlist coupon for ticket type "General Admission" on event "DevConf" and "General Admission" has `WaitlistMode = true`
- **THEN** the registration proceeds normally for the coupon-backed ticket (coupon bypass is unchanged)

#### Scenario: Self-service rejected — ticket type has no capacity set
- **WHEN** an attendee requests registration for "Speaker Pass" which has no capacity configured
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
- **THEN** the registration is created for "employee@acme.com"

#### Scenario: Concurrent archive detected at claim time
- **WHEN** an attendee self-registers and `TicketedEvent.Status` is Active at policy-check time but `TicketCatalog.EventStatus` has been transitioned to Archived by an in-flight archive before the claim commits
- **THEN** the registration fails with reason "event not active" and no capacity is consumed

### Requirement: Ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration. For actual registration ticket sets, the system SHALL reject duplicate ticket types, non-existent ticket types, cancelled ticket types, overlapping time slots, and ticket selections for an email address that is already actively registered for the same event unless the operation is explicitly changing that existing registration.

Waitlist ticket selections are independent per ticket type. The system SHALL reject duplicate ticket types within a waitlist ticket selection and SHALL reject a ticket type that appears in both the registration ticket set and waitlist ticket set in the same request. The system SHALL NOT reject waitlist ticket selections solely because waitlisted ticket types overlap with registered ticket types or with each other.

The system SHALL reject all registration and waitlist submissions when the `TicketedEvent.Status` for the event is not Active. As a consistency safety net, the atomic claim against `TicketCatalog` rejects when `TicketCatalog.EventStatus` is Archived.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) as registration tickets on event "DevConf" with an open window and `TicketedEvent.Status` Active
- **THEN** a registration is created with both ticket types, "General Admission" capacity used increases to 51, and "Workshop A" capacity used increases to 11

#### Scenario: Successful waitlist overlap with registered ticket
- **WHEN** an attendee requests registration for "Workshop A" and waitlist entry for "Workshop B", and both workshops share the same time slot
- **THEN** the registration for "Workshop A" and waitlist entry for "Workshop B" are accepted if each requested action is otherwise valid

#### Scenario: Successful waitlist overlap within waitlist selection
- **WHEN** an attendee requests waitlist entries for "Workshop B" and "Workshop C", and both workshops share the same time slot
- **THEN** both waitlist entries are accepted if each requested waitlist action is otherwise valid

#### Scenario: Rejected — duplicate ticket types in registration selection
- **WHEN** an attendee registers selecting "General Admission" twice in `registerTicketTypeIds`
- **THEN** the request is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — duplicate ticket types in waitlist selection
- **WHEN** an attendee requests "Workshop B" twice in `waitlistTicketTypeIds`
- **THEN** the request is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — same ticket requested for registration and waitlist
- **WHEN** an attendee submits the same ticket type in both `registerTicketTypeIds` and `waitlistTicketTypeIds`
- **THEN** the request is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — non-existent ticket type
- **WHEN** an attendee registers selecting ticket type "Premium VIP" which does not exist on the event
- **THEN** the request is rejected with reason "unknown ticket type"

#### Scenario: Rejected — cancelled ticket type
- **WHEN** an attendee registers selecting "Workshop A" which has been cancelled
- **THEN** the request is rejected with reason "ticket type cancelled"

#### Scenario: Rejected — overlapping registration time slots
- **WHEN** an attendee registers selecting both "Workshop A" (slot "morning") and "Workshop B" (slot "morning") in `registerTicketTypeIds`
- **THEN** the request is rejected with reason "overlapping time slots"

#### Scenario: Rejected — TicketedEvent status is Cancelled
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Rejected — TicketedEvent status is Archived
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Archived
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Rejected — TicketCatalog.EventStatus catches concurrent transition
- **WHEN** policy checks pass against `TicketedEvent` (Active) but the event is archived before the claim commits, so `TicketCatalog.EventStatus` is Archived at claim time
- **THEN** the claim fails and no capacity is consumed

#### Scenario: Rejected — duplicate email for new registration
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to create another registration without using a ticket-change operation
- **THEN** the request is rejected with reason "already registered"

## ADDED Requirements

### Requirement: Registration submission reports mixed outcomes
The public registration endpoint SHALL return the outcome of an explicit registration/waitlist submission, including the created registration id when a registration was created, the ticket types registered, and the ticket types waitlisted.

#### Scenario: Mixed outcome response
- **WHEN** an attendee successfully registers for "Workshop A" and joins the waitlist for "Workshop B" in one request
- **THEN** the response includes the registration id, `registeredTicketTypeIds = [Workshop A]`, and `waitlistedTicketTypeIds = [Workshop B]`

#### Scenario: Waitlist-only outcome response
- **WHEN** an attendee successfully joins only waitlists and no registration is created
- **THEN** the response includes no registration id, an empty `registeredTicketTypeIds` list, and the waitlisted ticket type ids
