## ADDED Requirements

### Requirement: Ticket type operations are scoped to the owning team
All commands and queries that operate on a `TicketCatalog` or its ticket types SHALL verify that the owning `TicketedEvent` belongs to the team identified by `teamId` in the route. If the event does not exist or belongs to a different team, the system SHALL return 404.

This applies to: add ticket type, update ticket type, list ticket types.

#### Scenario: Ticket type operation on event belonging to the requested team
- **WHEN** an organizer of team "team-a" adds a ticket type to an event that belongs to "team-a"
- **THEN** the ticket type is created successfully

#### Scenario: Ticket type operation on event belonging to a different team
- **WHEN** an organizer of team "team-a" attempts to add a ticket type to an event that belongs to "team-b"
- **THEN** the request returns 404
