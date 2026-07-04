## MODIFIED Requirements

### Requirement: System re-notifies when a claim window expires
When a waitlist coupon's claim window expires without redemption the system SHALL revoke the coupon and trigger notification for the next batch of waitlist entries (subject to the same quiet-hours rules).

Waitlist coupon redemption SHALL apply the offered ticket as a capacity grant. If the attendee has no active registration for the event, the grant can create a registration. If the attendee already has an active registration for the event, the grant can be applied by changing that registration's tickets. In both cases, the final persisted registration ticket set SHALL be valid.

#### Scenario: Coupon expires — next batch notified
- **WHEN** the waitlist coupon issued to "alice@example.com" reaches its `ExpiresAt` without being redeemed, and "bob@example.com" is the next entry on the waitlist
- **THEN** the coupon is revoked, a new coupon is issued to "bob@example.com", a notification email is sent to "bob@example.com", and "bob@example.com" is removed from the waitlist

#### Scenario: Coupon expires — waitlist exhausted, capacity restored
- **WHEN** the last waitlist coupon expires without redemption and the waitlist is empty
- **THEN** the coupon is revoked, WaitlistMode conditions are re-evaluated, and if capacity is available and no pending coupons remain, WaitlistMode is lifted

#### Scenario: Coupon redeemed — first registration created
- **WHEN** "alice@example.com" has no active registration and redeems a waitlist coupon before `ExpiresAt`
- **THEN** a registration is created with the offered ticket, the coupon is marked redeemed, and no further waitlist processing occurs for that slot

#### Scenario: Coupon redeemed — existing registration changed
- **WHEN** "alice@example.com" already has an active registration and redeems a waitlist coupon before `ExpiresAt` by submitting a valid final ticket selection that includes the offered ticket
- **THEN** the existing registration's tickets are changed, the coupon is marked redeemed, and no further waitlist processing occurs for that slot

## ADDED Requirements

### Requirement: Waitlist entries are independent ticket-type intents
Waitlist entries SHALL be independent per ticket type. The system SHALL allow an attendee to hold active waitlist entries for multiple ticket types even when those ticket types overlap each other or overlap the attendee's current registered tickets.

The system SHALL enforce time-slot overlap constraints only when creating or changing an actual Registration. A waitlist offer claim SHALL validate the final registered ticket set at claim time.

#### Scenario: Waitlist entry overlaps current registration
- **WHEN** an attendee is registered for "Workshop A" and joins the waitlist for overlapping "Workshop B"
- **THEN** the waitlist entry for "Workshop B" is accepted if the waitlist is active

#### Scenario: Multiple waitlist entries overlap each other
- **WHEN** an attendee joins waitlists for "Workshop B" and "Workshop C" and those ticket types share the same time slot
- **THEN** both waitlist entries are accepted if each waitlist is active

#### Scenario: Waitlist offer claim validates final registration
- **WHEN** an attendee with current ticket "Workshop A" claims a waitlist offer for overlapping "Workshop B"
- **THEN** the claim succeeds only if the final submitted registration ticket set removes the overlap or otherwise satisfies registration ticket validation

### Requirement: Registration submission can join selected waitlists atomically
The public registration submission SHALL be able to create waitlist entries for all ticket types listed in `waitlistTicketTypeIds` in the same transaction as any registration created for `registerTicketTypeIds`.

Each requested waitlist ticket type SHALL currently have `WaitlistEnabled = true` and `WaitlistMode = true`. If any requested waitlist action cannot be applied, the system SHALL reject the entire submission and SHALL NOT create a partial registration or partial waitlist entries.

#### Scenario: Mixed submission creates waitlist entry atomically
- **WHEN** an attendee submits registration for "Workshop A" and waitlist join for "Workshop B" in one public registration request
- **THEN** the registration and waitlist entry are persisted in the same transaction

#### Scenario: Mixed submission rejects stale waitlist state
- **WHEN** an attendee submits waitlist join for "Workshop B" but Workshop B has left WaitlistMode before submission is handled
- **THEN** the entire submission is rejected and no registration or waitlist entry is created
