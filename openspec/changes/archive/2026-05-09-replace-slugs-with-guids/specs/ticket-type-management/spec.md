## MODIFIED Requirements

### Requirement: Public endpoint lists self-service-enabled ticket types
The system SHALL expose a public endpoint `GET /events/{teamId}/{eventId}/ticket-types`
that requires API key authentication and returns all active (not cancelled),
self-service-enabled ticket types for the event. This endpoint is intended for
external websites to determine which ticket types to present to attendees.
Cancelled ticket types and ticket types with `SelfServiceEnabled = false` SHALL
be excluded from the response. Each ticket type in the response SHALL include:
slug, name, time slots, max capacity (null if unlimited), and used capacity.

#### Scenario: Returns only self-service-enabled, active ticket types
- **GIVEN** an event has "general" (selfServiceEnabled: true, active), "vip" (selfServiceEnabled: false, active), and "early-bird" (selfServiceEnabled: true, cancelled)
- **WHEN** an external caller fetches `GET /events/{teamId}/{eventId}/ticket-types`
- **THEN** only "general" is returned (vip is admin-only, early-bird is cancelled)

#### Scenario: Returns empty list when no self-service ticket types exist
- **GIVEN** an event has only admin-only ticket types
- **WHEN** an external caller fetches `GET /events/{teamId}/{eventId}/ticket-types`
- **THEN** an empty list is returned

#### Scenario: Returns 404 when event does not exist
- **WHEN** an external caller fetches ticket types for a non-existent team ID or event ID
- **THEN** the response is HTTP 404 Not Found
