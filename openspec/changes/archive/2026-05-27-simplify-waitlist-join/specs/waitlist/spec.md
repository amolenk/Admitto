## MODIFIED Requirements

### Requirement: Attendee can join the waitlist for a ticket type in WaitlistOnly mode

The system SHALL allow an attendee to join the waitlist for a specific ticket type
that is in WaitlistOnly mode. The request MUST include a valid OTP verification token
that proves email ownership. The entry is added to the waitlist immediately — there is
no separate confirmation step. The attendee is assigned the next position in the ranked
queue. An attendee with the same email address SHALL NOT be added twice for the same
ticket type; a duplicate request SHALL succeed silently (idempotent).

#### Scenario: Successfully join the waitlist
- **WHEN** attendee "alice@example.com" submits a waitlist join request for ticket
  type "General Admission" on event "DevConf" which is in WaitlistOnly mode, including
  a valid OTP verification token proving ownership of "alice@example.com"
- **THEN** an active waitlist entry is created immediately for "alice@example.com" at
  the next queue position and the response is HTTP 202 Accepted

#### Scenario: Duplicate join request is silently accepted
- **WHEN** "alice@example.com" is already on the waitlist for "General Admission" and
  submits another join request with a valid token
- **THEN** the response is HTTP 202 Accepted with no duplicate entry created

#### Scenario: Invalid or expired token is rejected
- **WHEN** "alice@example.com" submits a waitlist join request with an invalid or
  expired verification token
- **THEN** the request is rejected with reason "verification token invalid or expired"

#### Scenario: Token email mismatch is rejected
- **WHEN** a join request contains a token issued for "bob@example.com" but the
  request specifies a different email address
- **THEN** the request is rejected (the token's email is authoritative; no email field
  is needed in the request body — the token carries it)

#### Scenario: Cannot join when ticket type is not in WaitlistOnly mode
- **WHEN** "alice@example.com" submits a waitlist join request for ticket type
  "General Admission" that is NOT in WaitlistOnly mode
- **THEN** the request is rejected with reason "ticket type not in waitlist mode"

#### Scenario: Cannot join when waitlistEnabled is false
- **WHEN** "alice@example.com" submits a waitlist join request for a ticket type
  with `WaitlistEnabled = false`
- **THEN** the request is rejected with reason "waitlist not enabled for this ticket type"

## REMOVED Requirements

### Requirement: Attendee confirms waitlist entry via email link
**Reason**: Removed as part of simplifying the join flow. Email ownership is now
proved upfront via the OTP verification token included in the join request.
**Migration**: The `POST /waitlist/{ticketTypeId}/confirm` endpoint is removed.
Attendees must use the OTP flow to obtain a verification token before joining the
waitlist.
