## ADDED Requirements

### Requirement: TeamName VO replaces DisplayName for teams
The system SHALL introduce a `TeamName` Vogen value object (string-backed) in `Shared/Kernel/ValueObjects/` and replace all uses of `DisplayName` for team names, including `Team.Name` and any domain events that carry the team's display name.

#### Scenario: Team name is strongly typed
- **WHEN** a `Team` entity is constructed with a name
- **THEN** the `Name` property is of type `TeamName`, not `DisplayName`

#### Scenario: Invalid team name is rejected at the VO boundary
- **WHEN** code attempts to construct a `TeamName` with an empty or whitespace value
- **THEN** the VO validation rejects it with an appropriate error

### Requirement: EventName VO replaces DisplayName for ticketed events
The system SHALL introduce an `EventName` Vogen value object (string-backed) in `Shared/Kernel/ValueObjects/` and replace all uses of `DisplayName` for event names, including `TicketedEvent.Name`, `TicketedEventCreationRequestedDomainEvent`, and `OtpCodeRequestedDomainEvent.EventName` (currently `string`).

#### Scenario: TicketedEvent name is strongly typed
- **WHEN** a `TicketedEvent` aggregate is constructed with a name
- **THEN** the `Name` property is of type `EventName`, not `DisplayName`

#### Scenario: Domain event carries EventName
- **WHEN** a `TicketedEventCreationRequestedDomainEvent` is raised
- **THEN** the event's name field is of type `EventName`

#### Scenario: OtpCodeRequestedDomainEvent carries EventName
- **WHEN** an `OtpCodeRequestedDomainEvent` is raised
- **THEN** the `EventName` property is of type `EventName` VO, not `string`

### Requirement: TicketTypeName VO replaces DisplayName for ticket types
The system SHALL introduce a `TicketTypeName` Vogen value object (string-backed) in `Registrations/Domain/ValueObjects/` and replace all uses of `DisplayName` for ticket type names, including `TicketType.Name` and `TicketTypeSnapshot.Name`.

#### Scenario: TicketType name is strongly typed
- **WHEN** a `TicketType` entity is constructed with a name
- **THEN** the `Name` property is of type `TicketTypeName`, not `DisplayName`

#### Scenario: TicketTypeSnapshot carries TicketTypeName
- **WHEN** a `TicketTypeSnapshot` record is created
- **THEN** its `Name` field is of type `TicketTypeName`, not `string`

### Requirement: DisplayName VO is removed
The system SHALL remove the shared `DisplayName` VO from `Shared/Kernel/ValueObjects/` once all usages have been replaced by the domain-specific name VOs.

#### Scenario: No remaining references to DisplayName
- **WHEN** the codebase is compiled after the split
- **THEN** no code references the `DisplayName` type
