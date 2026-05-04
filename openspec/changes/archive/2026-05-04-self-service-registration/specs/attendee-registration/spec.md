## MODIFIED Requirements

### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint `POST /events/{teamSlug}/{eventSlug}/register` by providing their email, attendee info, selected ticket types, **and a valid email-verification token proving ownership of the supplied email address**. Self-service registrations SHALL enforce per-ticket-type capacity (ticket types without an explicit capacity set SHALL be rejected as not available), the registration window, optional email domain restrictions, and `EventRegistrationPolicy.RegistrationStatus` being `Open`.

The verification token SHALL be a short-lived HMAC-signed JWT (HS256) issued by the OTP verify endpoint (`POST /events/{teamSlug}/{eventSlug}/otp/verify`). It embeds `email`, `eventId`, `teamId`, and `exp`. The system SHALL validate the token signature, expiry, and binding (`eventId` matches the event being registered for, `email` matches the supplied registration email) before performing any event, catalog, coupon, or ticket-type lookups. The token has a TTL of 15 minutes from issuance. A single token MAY be used to register (it is not invalidated on success — within its TTL a token could theoretically be reused for a duplicate registration, which is blocked by existing duplicate-email registration checks).

The system SHALL reject self-service requests that omit the verification token with reason "email verification required". The system SHALL reject self-service requests whose token fails signature verification, has expired, or whose embedded email does not match the supplied registration email, with reason "email verification invalid". The verification check SHALL run before any event, catalog, coupon, or ticket-type lookups so that token-related failures do not leak information about other resources.

Whether registration is open SHALL be derived from the registration window (`now ∈ [opensAt, closesAt)`) combined with the event's lifecycle status read from the `TicketedEvent` aggregate (see event-management) and `EventRegistrationPolicy.RegistrationStatus` being `Open`. Application handlers SHALL load the `TicketedEvent` to validate window, domain, and active-status invariants, then atomically claim ticket capacity on the `TicketCatalog`. The atomic claim SHALL also be guarded by `TicketCatalog.EventStatus` so that a concurrent cancel/archive cannot leak through after the policy check; an `EventStatus` of Cancelled or Archived at claim time SHALL fail the registration with reason "event not active".

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on event "DevConf" with capacity 100 (50 used), `TicketedEvent.Status` Active, `TicketCatalog.EventStatus` Active, window "2025-01-01T00:00Z" / "2025-06-01T00:00Z" at current time "2025-03-15T12:00Z", no domain restriction, `EventRegistrationPolicy.RegistrationStatus` Open, and a valid verification token bound to "dave@example.com"
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Self-service rejected — verification token missing
- **WHEN** an attendee self-registers without supplying a verification token
- **THEN** the registration is rejected with reason "email verification required" and no event, catalog, or capacity lookup is performed

#### Scenario: Self-service rejected — verification token invalid
- **WHEN** an attendee self-registers with a token that fails signature verification, has expired, or is bound to a different email than the registration email
- **THEN** the registration is rejected with reason "email verification invalid"

#### Scenario: Self-service rejected — token bound to different event
- **WHEN** an attendee self-registers for "devconf-2026" using a verification token issued for "other-event"
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
- **WHEN** an attendee self-registers as "employee@acme.com" for event "CorpConf" which is restricted to "@acme.com", the window is open, `EventRegistrationPolicy.RegistrationStatus` is Open, and a valid verification token bound to "employee@acme.com"
- **THEN** a registration is created for "employee@acme.com"

#### Scenario: Self-service rejected — registration policy is Draft
- **WHEN** an attendee self-registers for an event whose `EventRegistrationPolicy.RegistrationStatus` is `Draft`
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — registration policy is Closed
- **WHEN** an attendee self-registers for an event whose `EventRegistrationPolicy.RegistrationStatus` is `Closed`
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Concurrent cancel detected at claim time
- **WHEN** an attendee self-registers and `TicketedEvent.Status` is Active at policy-check time but `TicketCatalog.EventStatus` has been transitioned to Cancelled by an in-flight cancel before the claim commits
- **THEN** the registration fails with reason "event not active" and no capacity is consumed
