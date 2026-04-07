## Purpose

Organizers create single-use coupon codes to invite specific people to their events. Coupons grant access to selected ticket types and bypass capacity and domain restrictions. Organizers can list, view, and revoke coupons.

## Requirements

### Requirement: Organizer can create a coupon
The system SHALL allow organizers (Owner or Organizer role) to create a coupon by
specifying a target email, allowlisted ticket type(s), expiry datetime, and whether
the coupon bypasses the registration window. The system SHALL generate a unique
GUID-based coupon code upon creation. The system SHALL trigger an invitation email
to the target email upon creation. The system SHALL reject coupon creation if any
specified ticket type does not exist or is cancelled, if the expiry datetime is in
the past, or if the event lifecycle status is Cancelled or Archived.

#### Scenario: Successful coupon creation
- **WHEN** an organizer creates a coupon for "speaker@example.com" on active event "DevConf" allowlisting "Speaker Pass" expiring "2025-06-01T00:00Z" with bypassRegistrationWindow disabled
- **THEN** a coupon is created with a unique code and an invitation email is triggered for "speaker@example.com"

#### Scenario: Coupon with registration window bypass
- **WHEN** an organizer creates a coupon with bypassRegistrationWindow enabled
- **THEN** a coupon is created with bypassRegistrationWindow set to true

#### Scenario: Rejected — ticket type does not exist
- **WHEN** an organizer creates a coupon allowlisting "Premium VIP" which does not exist on event "DevConf"
- **THEN** the coupon creation is rejected with reason "unknown ticket type"

#### Scenario: Rejected — ticket type is cancelled
- **WHEN** an organizer creates a coupon allowlisting "Workshop A" which has been cancelled on event "DevConf"
- **THEN** the coupon creation is rejected with reason "ticket type cancelled"

#### Scenario: Rejected — expiry in the past
- **WHEN** an organizer creates a coupon with expiry "2020-01-01T00:00Z"
- **THEN** the coupon creation is rejected with reason "expiry must be in the future"

#### Scenario: Rejected — event lifecycle status is Cancelled
- **WHEN** an organizer creates a coupon for event "OldConf" whose lifecycle status is Cancelled
- **THEN** the coupon creation is rejected with reason "event not active"

#### Scenario: Rejected — event lifecycle status is Archived
- **WHEN** an organizer creates a coupon for event "OldConf" whose lifecycle status is Archived
- **THEN** the coupon creation is rejected with reason "event not active"

---

### Requirement: Organizer can list coupons for an event
The system SHALL allow organizers to list all coupons for an event showing target
email, derived status (active/redeemed/revoked/expired), allowlisted ticket types,
expiry, and creation date. Coupon status is derived from aggregate state:
Redeemed > Revoked > Expired > Active.

#### Scenario: List coupons for an event
- **WHEN** an organizer lists coupons for "DevConf" which has coupons for "speaker@example.com" (active), "alice@example.com" (redeemed), and "bob@example.com" (revoked)
- **THEN** all 3 coupons are returned with their status, email, ticket types, and expiry

#### Scenario: Empty coupon list
- **WHEN** an organizer lists coupons for event "DevConf" which has no coupons
- **THEN** an empty list is returned

---

### Requirement: Organizer can view a single coupon's full details
The system SHALL allow organizers to view a single coupon's full details including
the coupon code.

#### Scenario: View coupon details
- **WHEN** an organizer views the details of a coupon for "speaker@example.com" on event "DevConf"
- **THEN** the full details are returned including the coupon code

---

### Requirement: Organizer can revoke a coupon
The system SHALL allow organizers to revoke an active or expired coupon, preventing
it from being used for registration. Revoking an already-revoked coupon SHALL
succeed without error (idempotent). The system SHALL reject revocation of a coupon
that has already been redeemed.

#### Scenario: Successful revocation
- **WHEN** an organizer revokes active coupon "INVITE-001" for "speaker@example.com"
- **THEN** the coupon status changes to "revoked" and the coupon can no longer be used for registration

#### Scenario: Revoke already-expired coupon succeeds
- **WHEN** an organizer revokes expired coupon "INVITE-002" for "bob@example.com"
- **THEN** the coupon status changes to "revoked"

#### Scenario: Rejected — revoke redeemed coupon
- **WHEN** an organizer attempts to revoke coupon "INVITE-003" which has already been redeemed
- **THEN** the revocation is rejected with reason "coupon already redeemed"
