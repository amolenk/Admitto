## ADDED Requirements

### Requirement: Badge operations are scoped to the owning team
All commands and queries that operate on a `BadgesEvent` SHALL verify that the event belongs to the team identified by `teamId` in the route. If the event does not exist or belongs to a different team, the system SHALL return 404.

This applies to all badge type and badge instance operations: add/rename/delete badge type, list badge types, add/update/delete badge instance, list badge instances, export badge CSV.

#### Scenario: Badge operation on event belonging to the requested team
- **WHEN** an organizer of team "team-a" lists badge types for an event that belongs to "team-a"
- **THEN** the request succeeds and returns the badge types

#### Scenario: Badge operation on event belonging to a different team
- **WHEN** an organizer of team "team-a" attempts to list badge types for an event that belongs to "team-b"
- **THEN** the request returns 404

### Requirement: BadgesEvent projection captures team ownership
The `BadgesEvent` projection in the Badges module SHALL store the `TeamId` of the owning team. The `TeamId` SHALL be set when the projection is first created (from `TicketedEventCreatedIntegrationEvent`) and SHALL NOT change after creation.

#### Scenario: BadgesEvent created with TeamId
- **WHEN** a `TicketedEventCreatedIntegrationEvent` is processed for team "team-a" and event "conf-2026"
- **THEN** a `BadgesEvent` is created with `TeamId` set to "team-a"'s ID
