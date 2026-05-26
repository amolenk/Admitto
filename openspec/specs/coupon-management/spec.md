## Purpose

Organizers create single-use coupon codes to invite specific people to their events. Coupons grant access to selected ticket types and bypass capacity and domain restrictions. Organizers can list, view, and revoke coupons.

## Requirements

### Requirement: Organizer can create a coupon
The system SHALL allow organizers (Owner or Organizer role) to create a coupon by
specifying a target email, allowlisted ticket type IDs, expiry datetime, and whether
the coupon bypasses the registration window. The system SHALL generate a unique
GUID-based coupon code upon creation. The system SHALL trigger an invitation email
to the target email upon creation. The system SHALL reject coupon creation if any
specified ticket type does not exist or is cancelled, if the expiry datetime is in
the past, or if the event lifecycle status is Cancelled or Archived.

#### Scenario: Successful coupon creation
- **WHEN** an organizer creates a coupon for "speaker@example.com" on active event "DevConf" allowlisting the ID of "Speaker Pass" expiring "2025-06-01T00:00Z" with bypassRegistrationWindow disabled
- **THEN** a coupon is created with a unique code and an invitation email is triggered for "speaker@example.com"

#### Scenario: Coupon with registration window bypass
- **WHEN** an organizer creates a coupon with bypassRegistrationWindow enabled
- **THEN** a coupon is created with bypassRegistrationWindow set to true

#### Scenario: Rejected — ticket type does not exist
- **WHEN** an organizer creates a coupon allowlisting a ticket type ID that does not exist on event "DevConf"
- **THEN** the coupon creation is rejected with reason "unknown ticket type"

#### Scenario: Rejected — ticket type is cancelled
- **WHEN** an organizer creates a coupon allowlisting the ID of "Workshop A" which has been cancelled on event "DevConf"
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
email, derived status (active/redeemed/revoked/expired), allowlisted ticket type IDs
and names, expiry, and creation date. Coupon status is derived from aggregate state:
Redeemed > Revoked > Expired > Active.

#### Scenario: List coupons for an event
- **WHEN** an organizer lists coupons for "DevConf" which has coupons for "speaker@example.com" (active), "alice@example.com" (redeemed), and "bob@example.com" (revoked)
- **THEN** all 3 coupons are returned with their status, email, allowlisted ticket type ids and names, and expiry

#### Scenario: Empty coupon list
- **WHEN** an organizer lists coupons for event "DevConf" which has no coupons
- **THEN** an empty list is returned

---

### Requirement: Organizer can view a single coupon's full details
The system SHALL allow organizers to view a single coupon's full details including
the coupon code and the allowlisted ticket type IDs and names.

#### Scenario: View coupon details
- **WHEN** an organizer views the details of a coupon for "speaker@example.com" on event "DevConf"
- **THEN** the full details are returned including the coupon code and allowlisted ticket type ids and names

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

---

### Requirement: Coupons have a source discriminator

The `Coupon` aggregate gains a `Source` field with values `Organiser` (existing, default) and `Waitlist` (new). Organiser-created coupons behave exactly as today. Waitlist coupons are system-generated and carry the following differences:

- **No invitation email is triggered** on creation (the waitlist notification email serves this purpose and is sent separately by the waitlist notification flow).
- `Source: Waitlist` coupons appear in the organiser's coupon list alongside organiser-created coupons, distinguishable by the `source` field.
- Waitlist coupons cannot be created via the organiser API (`POST .../coupons`); they are created only by the `ProcessWaitlistNotifications` command handler.

#### Scenario: Waitlist coupon appears in coupon list with source "waitlist"
- **WHEN** an organizer lists coupons for "DevConf" which has one organiser-created coupon for "speaker@example.com" and one system-generated waitlist coupon for "alice@example.com"
- **THEN** both coupons are returned; the waitlist coupon has `"source": "waitlist"` and the organiser coupon has `"source": "organiser"`

#### Scenario: Waitlist coupon does not trigger invitation email
- **WHEN** the system creates a waitlist coupon for "alice@example.com"
- **THEN** no invitation email is sent (only the waitlist notification email is sent by the waitlist notification flow)

---

### Requirement: Public coupon details lookup endpoint

The system SHALL expose a public (unauthenticated) endpoint:

```
GET /events/{teamSlug}/{eventSlug}/coupons/{couponCode}
```

that returns the coupon's status and allowlisted ticket types. This allows the external event website to parse a coupon code received by the attendee and pre-select the correct ticket type in the registration form before the attendee begins filling in their details.

The response SHALL include:
- `status`: `"active"` | `"expired"` | `"redeemed"` | `"revoked"`
- `allowedTicketTypes`: array of `{ id, name }` objects
- `expiresAt`: ISO 8601 datetime (nullable for non-expiring coupons)

The target email SHALL NOT be returned.

The endpoint SHALL return `404 Not Found` when the coupon code does not exist for the specified event.

#### Scenario: Look up an active waitlist coupon
- **WHEN** a public client requests coupon code "abc-123" for event "DevConf" at team "acme" and the coupon is active and allowlists ticket type "General Admission"
- **THEN** the response is 200 OK with `status: "active"`, `allowedTicketTypes` containing `{ id: "...", name: "General Admission" }`, and the `expiresAt` datetime

#### Scenario: Look up a redeemed coupon
- **WHEN** a public client requests coupon code "abc-456" which has already been redeemed
- **THEN** the response is 200 OK with `status: "redeemed"`

#### Scenario: Look up a non-existent coupon code
- **WHEN** a public client requests coupon code "does-not-exist" for event "DevConf"
- **THEN** the response is `404 Not Found`

#### Scenario: Look up a coupon that belongs to a different event
- **WHEN** a public client requests a valid coupon code but uses the slug of a different event
- **THEN** the response is `404 Not Found`
