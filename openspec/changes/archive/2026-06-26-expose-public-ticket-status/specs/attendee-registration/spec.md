## ADDED Requirements

### Requirement: Public ticket discovery exposes ticket status
The public ticket-type listing at `GET /api/events/{eventId}/ticket-types` SHALL return every ticket type that is enabled for attendee self-service, regardless of remaining capacity. Sold-out ticket types SHALL remain in the response so that partner websites can render them.

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
