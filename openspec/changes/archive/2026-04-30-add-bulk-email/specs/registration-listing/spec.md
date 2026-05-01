## MODIFIED Requirements

### Requirement: Admin can list registrations for a ticketed event
The system SHALL expose a query that returns every registration belonging to a single ticketed event, with enough information to identify the attendee, the tickets they hold, when they registered, the registration's lifecycle `Status`, and whether the attendee has reconfirmed.

#### Scenario: SC001 Empty event returns empty list
- **WHEN** an admin requests the registrations of an active event with no registrations
- **THEN** the system returns an empty collection

#### Scenario: SC002 Returns one item per registration
- **GIVEN** an event has three registrations
- **WHEN** an admin requests the registrations of that event
- **THEN** the system returns three items
- **AND** each item exposes the registration id, the attendee `email`, `firstName`, `lastName`, the list of tickets (slug + display name), the registered-at timestamp, the registration `status` (`Registered`/`Cancelled`), and `hasReconfirmed` (with `reconfirmedAt` when true)

#### Scenario: SC003 Multiple tickets are surfaced together
- **GIVEN** a registration was created with two ticket types
- **WHEN** an admin requests the registrations
- **THEN** the corresponding item exposes both tickets with their slug and current display name

#### Scenario: SC004 Items belong only to the requested event
- **GIVEN** two events of the same team each have registrations
- **WHEN** an admin requests the registrations of one event
- **THEN** only registrations of that event are returned

#### Scenario: SC005 Unknown team returns 404
- **WHEN** an admin requests registrations using an unknown team slug
- **THEN** the system returns a not-found result

#### Scenario: SC006 Unknown event returns 404
- **WHEN** an admin requests registrations using a known team but unknown event slug
- **THEN** the system returns a not-found result

#### Scenario: SC010 Cancelled registrations are included with status=Cancelled
- **GIVEN** an event has one `Registered` and one `Cancelled` registration
- **WHEN** an admin requests the registrations of that event
- **THEN** both items are returned and each carries its own `status`

#### Scenario: SC011 Reconfirmed registration carries the timestamp
- **GIVEN** an attendee has reconfirmed their registration at `2026-04-20T08:30Z`
- **WHEN** an admin requests the registrations of the event
- **THEN** the corresponding item carries `hasReconfirmed=true` and `reconfirmedAt="2026-04-20T08:30Z"`
