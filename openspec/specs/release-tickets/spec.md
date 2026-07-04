## Purpose

TBD

## Requirements

### Requirement: Ticket capacity is released when a registration is cancelled

The system SHALL automatically release the ticket capacity held by a cancelled registration. When a `RegistrationCancelledDomainEvent` is raised, the system SHALL decrement `UsedCapacity` by 1 on each `TicketType` in the `TicketCatalog` whose slug matches a ticket on the cancelled registration. The `UsedCapacity` SHALL NOT be decremented below zero.

#### Scenario: SC001 Cancelling a registration decrements ticket type capacity

- **GIVEN** an event with a ticket type that has `UsedCapacity` > 0
- **AND** a registration for that event holding one or more ticket type slugs
- **WHEN** the registration is cancelled
- **THEN** `UsedCapacity` for each matching ticket type is decremented by 1

#### Scenario: SC002 Release is skipped when no ticket catalog exists

- **GIVEN** a registration for an event that has no `TicketCatalog` (e.g. coupon-only)
- **WHEN** the registration is cancelled
- **THEN** no error occurs and no capacity change is made

#### Scenario: SC003 UsedCapacity does not go below zero

- **GIVEN** a ticket type with `UsedCapacity` of 0
- **WHEN** a release is attempted for that ticket type
- **THEN** `UsedCapacity` remains 0

#### Scenario: SC004 Release skips unknown ticket type slugs

- **GIVEN** a registration whose ticket snapshot contains a slug that no longer exists in the catalog
- **WHEN** the registration is cancelled
- **THEN** the unknown slug is silently skipped and no error is raised
