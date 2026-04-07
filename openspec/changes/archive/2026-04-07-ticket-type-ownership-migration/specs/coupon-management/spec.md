## MODIFIED Requirements

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
