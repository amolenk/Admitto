## ADDED Requirements

### Requirement: Self-registration reports recoverable ticket-state conflicts

The public self-service registration endpoint SHALL return a structured ticket-state conflict response when a request cannot be applied because one or more requested ticket actions no longer match the current ticket state. The response SHALL use a conflict status and SHALL group submitted ticket type ids by their current client-actionable state.

The grouped states SHALL distinguish at least these outcomes: the ticket can currently be registered, the ticket can currently be joined via waitlist, the ticket is unavailable to the public self-service flow, the ticket type is unknown, or the ticket is invalid for the submitted action. The response SHALL NOT persist a partial registration, waitlist entry, or capacity change.

The system SHALL only return these ticket-state details after the attendee's email-verification token has been accepted. Token-related failures SHALL continue to fail before event, catalog, waitlist, or ticket-type lookup and SHALL NOT expose ticket-state details.

#### Scenario: Requested registration ticket became waitlistable
- **WHEN** an attendee submits `registerTicketTypeIds = [Workshop]` and Workshop has become sold out with `WaitlistEnabled = true` and `WaitlistMode = true` before the request is handled
- **THEN** the request is rejected with a ticket-state conflict, no registration or waitlist entry is created, and the response includes Workshop in the waitlistable ticket type ids

#### Scenario: Requested registration ticket became unavailable without waitlist
- **WHEN** an attendee submits `registerTicketTypeIds = [VIP Dinner]` and VIP Dinner has reached capacity with `WaitlistEnabled = false`
- **THEN** the request is rejected with a ticket-state conflict, no registration or waitlist entry is created, and the response includes VIP Dinner in the unavailable ticket type ids

#### Scenario: Requested waitlist ticket became registerable again
- **WHEN** an attendee submits `waitlistTicketTypeIds = [Workshop]` but Workshop has left WaitlistOnly mode and can currently be registered through public self-service
- **THEN** the request is rejected with a ticket-state conflict, no registration or waitlist entry is created, and the response includes Workshop in the registerable ticket type ids

#### Scenario: Mixed selection reports all submitted ticket states
- **WHEN** an attendee submits `registerTicketTypeIds = [Workshop A, Workshop B]` and `waitlistTicketTypeIds = [Workshop C]`, Workshop A remains registerable, Workshop B is now waitlistable, and Workshop C is no longer in WaitlistOnly mode
- **THEN** the request is rejected with a ticket-state conflict, no registration or waitlist entry is created, and the response includes Workshop A in the registerable ticket type ids, Workshop B in the waitlistable ticket type ids, and Workshop C in the registerable ticket type ids

#### Scenario: Token failure does not expose ticket states
- **WHEN** an attendee submits a self-service registration request with an invalid, expired, missing, or mismatched email-verification token
- **THEN** the request is rejected with the existing verification error and the response does not include ticket-state details
