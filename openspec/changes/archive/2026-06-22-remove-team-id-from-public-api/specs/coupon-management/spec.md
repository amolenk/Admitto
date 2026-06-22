## MODIFIED Requirements

### Requirement: Public coupon details lookup endpoint
The system SHALL expose an API-key-protected public endpoint:

```
GET /api/events/{eventId}/coupons/{couponCode}
```

that returns the coupon's status and allowlisted ticket types. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventId}` and `{couponCode}` from the URL path. This allows the external event website to parse a coupon code received by the attendee and pre-select the correct ticket type in the registration form before the attendee begins filling in their details.

The response SHALL include:
- `status`: `"active"` | `"expired"` | `"redeemed"` | `"revoked"`
- `allowedTicketTypes`: array of `{ id, name }` objects
- `expiresAt`: ISO 8601 datetime (nullable for non-expiring coupons)

The target email SHALL NOT be returned.

The endpoint SHALL return `404 Not Found` when the coupon code does not exist for the specified event and API-key team scope.

#### Scenario: Look up an active waitlist coupon
- **WHEN** a public client requests coupon code "abc-123" for an event using a valid API key for the event's team and the coupon is active and allowlists ticket type "General Admission"
- **THEN** the response is 200 OK with `status: "active"`, `allowedTicketTypes` containing `{ id: "...", name: "General Admission" }`, and the `expiresAt` datetime

#### Scenario: Look up a redeemed coupon
- **WHEN** a public client requests coupon code "abc-456" which has already been redeemed using a valid API key for the event's team
- **THEN** the response is 200 OK with `status: "redeemed"`

#### Scenario: Look up a non-existent coupon code
- **WHEN** a public client requests coupon code "does-not-exist" for an event
- **THEN** the response is `404 Not Found`

#### Scenario: Look up a coupon that belongs to a different event
- **WHEN** a public client requests a valid coupon code but uses the ID of a different event
- **THEN** the response is `404 Not Found`

#### Scenario: Missing API key is rejected
- **WHEN** a public client requests coupon details without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the coupon details handler
