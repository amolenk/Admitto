## MODIFIED Requirements

### Requirement: Public registration endpoints use API-key team scope
The system SHALL expose public attendee registration endpoints under `/api/events/{eventSlug}` and SHALL require a valid active `X-Api-Key` for each request. Public registration endpoint handlers SHALL derive `TeamId` from the authenticated API-key principal, SHALL resolve `{eventSlug}` to the event's internal ID within that team scope, and SHALL NOT accept team ID or team slug in the URL.

Self-service registration SHALL be exposed at `POST /api/events/{eventSlug}/registrations`. Coupon-based registration SHALL be exposed at `POST /api/events/{eventSlug}/registrations/coupon`.

#### Scenario: Self-service registration uses API-key team scope
- **WHEN** an attendee posts a valid self-service registration request to `POST /api/events/{eventSlug}/registrations` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId` and the event ID resolved from the route `{eventSlug}`

#### Scenario: Coupon registration uses API-key team scope
- **WHEN** an attendee posts a valid coupon registration request to `POST /api/events/{eventSlug}/registrations/coupon` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId` and the event ID resolved from the route `{eventSlug}`

#### Scenario: Registration without API key is rejected
- **WHEN** an attendee posts to either public registration endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the registration handler

---

### Requirement: Public ticket discovery exposes ticket status
The public ticket-type listing at `GET /api/events/{eventSlug}/ticket-types` SHALL return every ticket type that is enabled for attendee self-service for the event resolved from `{eventSlug}` within the API-key owner's team scope, regardless of remaining capacity. Sold-out ticket types SHALL remain in the response so that partner websites can render them.

For each returned ticket type the response SHALL expose a `status` field with one of these exact values:

- `available`: the ticket type is not sold out and can be requested through `registerTicketTypeIds`.
- `waitlist`: the ticket type is sold out and currently accepts waitlist joins through `waitlistTicketTypeIds` or the dedicated waitlist endpoint.
- `soldOut`: the ticket type is sold out and does not currently accept waitlist joins.

A ticket type with no configured capacity SHALL always have `status = "available"`. The response SHALL NOT expose raw current/maximum capacity counters, internal waitlist-configuration or waitlist-state flags, or the previous public `soldOut` / `requiresWaitlist` booleans.

The server SHALL remain authoritative at submission time. Clients MAY use `status` to choose what to present and which submission path to call, but client-side rendering is not constrained by this specification.

#### Scenario: Available ticket
- **WHEN** a self-service-enabled ticket type has remaining capacity and is not in waitlist mode
- **THEN** the response includes the ticket type with `status = "available"`

#### Scenario: Ticket with no capacity limit is available
- **WHEN** a self-service-enabled ticket type has no configured capacity
- **THEN** the response includes the ticket type with `status = "available"`

#### Scenario: Sold-out ticket with an available waitlist
- **WHEN** a self-service-enabled ticket type is sold out and currently accepts waitlist joins
- **THEN** the response includes the ticket type with `status = "waitlist"`

#### Scenario: Sold-out ticket without a waitlist
- **WHEN** a self-service-enabled ticket type is sold out and does not currently accept waitlist joins
- **THEN** the response includes the ticket type with `status = "soldOut"`

#### Scenario: Non-self-service ticket is not returned
- **WHEN** a ticket type is not enabled for self-service
- **THEN** the response does not include the ticket type

#### Scenario: Response omits internal and prior public fields
- **WHEN** a client reads any ticket type from the response
- **THEN** the ticket type exposes `status` and does not expose raw capacity counters, internal waitlist flags, `soldOut`, or `requiresWaitlist`
