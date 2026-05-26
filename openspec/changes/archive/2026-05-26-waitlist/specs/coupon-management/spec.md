# Coupon Management — Delta Spec (Waitlist Change)

## Status: MODIFIED

Changes to the `coupon-management` capability introduced by the waitlist change.

---

### MODIFIED Requirement: Coupons have a source discriminator

The `Coupon` aggregate gains a `Source` field with values `Organiser` (existing,
default) and `Waitlist` (new). Organiser-created coupons behave exactly as today.
Waitlist coupons are system-generated and carry the following differences:

- **No invitation email is triggered** on creation (the waitlist notification email
  serves this purpose and is sent separately by the waitlist notification flow).
- `Source: Waitlist` coupons appear in the organiser's coupon list alongside
  organiser-created coupons, distinguishable by the `source` field.
- Waitlist coupons cannot be created via the organiser API (`POST .../coupons`); they
  are created only by the `ProcessWaitlistNotifications` command handler.

#### Scenario: Waitlist coupon appears in coupon list with source "waitlist"
- **WHEN** an organizer lists coupons for "DevConf" which has one organiser-created
  coupon for "speaker@example.com" and one system-generated waitlist coupon for
  "alice@example.com"
- **THEN** both coupons are returned; the waitlist coupon has `"source": "waitlist"`
  and the organiser coupon has `"source": "organiser"`

#### Scenario: Waitlist coupon does not trigger invitation email
- **WHEN** the system creates a waitlist coupon for "alice@example.com"
- **THEN** no invitation email is sent (only the waitlist notification email is sent
  by the waitlist notification flow)

---

### NEW Requirement: Public coupon details lookup endpoint

The system SHALL expose a public (unauthenticated) endpoint:

```
GET /events/{teamSlug}/{eventSlug}/coupons/{couponCode}
```

that returns the coupon's status and allowlisted ticket types. This allows the
external event website to parse a coupon code received by the attendee and
pre-select the correct ticket type in the registration form before the attendee
begins filling in their details.

The response SHALL include:
- `status`: `"active"` | `"expired"` | `"redeemed"` | `"revoked"`
- `allowedTicketTypes`: array of `{ id, name }` objects
- `expiresAt`: ISO 8601 datetime (nullable for non-expiring coupons)

The target email SHALL NOT be returned.

The endpoint SHALL return `404 Not Found` when the coupon code does not exist for
the specified event.

#### Scenario: Look up an active waitlist coupon
- **WHEN** a public client requests coupon code "abc-123" for event "DevConf" at
  team "acme" and the coupon is active and allowlists ticket type "General Admission"
- **THEN** the response is 200 OK with `status: "active"`, `allowedTicketTypes`
  containing `{ id: "...", name: "General Admission" }`, and the `expiresAt` datetime

#### Scenario: Look up a redeemed coupon
- **WHEN** a public client requests coupon code "abc-456" which has already been
  redeemed
- **THEN** the response is 200 OK with `status: "redeemed"`

#### Scenario: Look up a non-existent coupon code
- **WHEN** a public client requests coupon code "does-not-exist" for event "DevConf"
- **THEN** the response is `404 Not Found`

#### Scenario: Look up a coupon that belongs to a different event
- **WHEN** a public client requests a valid coupon code but uses the slug of a
  different event
- **THEN** the response is `404 Not Found`
