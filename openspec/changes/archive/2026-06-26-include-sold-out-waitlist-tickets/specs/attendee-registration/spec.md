## ADDED Requirements

### Requirement: Public ticket discovery exposes sold-out and waitlist state
The public ticket-type listing at `GET /api/events/{eventId}/ticket-types` SHALL return every ticket type that is enabled for attendee self-service, regardless of remaining capacity. Sold-out ticket types SHALL remain in the response so that partner websites can render them.

For each returned ticket type the response SHALL expose a boolean `soldOut` flag instead of raw capacity values. `soldOut` SHALL be `true` when the ticket type has a configured capacity and used capacity has reached it, and `false` otherwise. A ticket type with no configured capacity SHALL always have `soldOut = false`.

For each returned ticket type the response SHALL expose a boolean `requiresWaitlist` flag. `requiresWaitlist` SHALL be `true` only when the ticket type is sold out and currently accepts waitlist joins. Whenever `requiresWaitlist` is `true`, `soldOut` SHALL also be `true`.

The response SHALL NOT expose raw current/maximum capacity counters or the internal waitlist-configuration and waitlist-state flags.

The server SHALL remain authoritative at submission time: a ticket type with `soldOut = false` is registerable via `registerTicketTypeIds`, and a ticket type with `requiresWaitlist = true` is waitlistable via `waitlistTicketTypeIds` or the dedicated waitlist endpoint. Clients MAY use `soldOut` and `requiresWaitlist` to choose what to present, but client-side rendering is not constrained by this specification.

#### Scenario: Available ticket
- **WHEN** a self-service-enabled ticket type has remaining capacity and is not in waitlist mode
- **THEN** the response includes the ticket type with `soldOut = false` and `requiresWaitlist = false`

#### Scenario: Ticket with no capacity limit is never sold out
- **WHEN** a self-service-enabled ticket type has no configured capacity
- **THEN** the response includes the ticket type with `soldOut = false` and `requiresWaitlist = false`

#### Scenario: Sold-out ticket with an available waitlist
- **WHEN** a self-service-enabled ticket type is sold out and currently accepts waitlist joins
- **THEN** the response includes the ticket type with `soldOut = true` and `requiresWaitlist = true`

#### Scenario: Sold-out ticket without a waitlist
- **WHEN** a self-service-enabled ticket type is sold out and does not currently accept waitlist joins
- **THEN** the response includes the ticket type with `soldOut = true` and `requiresWaitlist = false`

#### Scenario: Non-self-service ticket is not returned
- **WHEN** a ticket type is not enabled for self-service
- **THEN** the response does not include the ticket type

#### Scenario: Response omits internal capacity and waitlist fields
- **WHEN** a client reads any ticket type from the response
- **THEN** the ticket type exposes `soldOut` and `requiresWaitlist` and does not expose raw capacity counters or the internal waitlist-configuration and waitlist-state flags
