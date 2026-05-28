## ADDED Requirements

### Requirement: Event operations are scoped to the owning team
All commands and queries that operate on a `TicketedEvent` SHALL verify that the event belongs to the team identified by `teamId` in the route. If the event does not exist or belongs to a different team, the system SHALL return a 404 response. A 403 SHALL NOT be returned, as it would reveal that the event exists under a different team.

This applies to: view event details, archive event, update event details, update time zone, configure registration policy, configure reconfirm policy, update additional detail schema.

#### Scenario: Event belongs to the requested team
- **WHEN** an organizer of team "team-a" requests details for an event that belongs to "team-a"
- **THEN** the request succeeds and returns the event details

#### Scenario: Event belongs to a different team
- **WHEN** an organizer of team "team-a" requests details for an event that belongs to "team-b"
- **THEN** the request returns 404
