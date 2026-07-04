# VO-Typed Registrations Primitives Specification

## Purpose

Replace primitive fields (`Guid`, `string`, `string[]`) on Registrations module entities with typed value objects so that all domain primitives are strongly typed at compile time and cannot be misassigned.

## Requirements

### Requirement: ActivityLog uses RegistrationId VO
The system SHALL replace the `Guid RegistrationId` field on `ActivityLog` with the existing `RegistrationId` VO.

#### Scenario: ActivityLog is created with typed RegistrationId
- **WHEN** `ActivityLog.Create` is called
- **THEN** the `RegistrationId` property is of type `RegistrationId` VO, not `Guid`

### Requirement: TicketType uses TicketTypeId as entity key
The system SHALL change `TicketType` from `Entity<string>` to `Entity<TicketTypeId>`, using the existing `TicketTypeId` VO. All construction sites and lookup code MUST be updated to wrap the raw string id via `TicketTypeId.From(rawString)`.

#### Scenario: TicketType entity has a typed id
- **WHEN** a `TicketType` entity is accessed
- **THEN** its `Id` property is of type `TicketTypeId`, not `string`

### Requirement: TicketType uses Slug array for time slot slugs
The system SHALL replace the `string[] TimeSlotSlugs` property on `TicketType` with `Slug[]`, using the existing `Slug` VO.

#### Scenario: TicketType time slot slugs are strongly typed
- **WHEN** a `TicketType` entity exposes its time slot slugs
- **THEN** each slug element is of type `Slug`, not `string`

### Requirement: TicketTypeSnapshot uses typed VOs for all fields
The system SHALL update the `TicketTypeSnapshot` record so that:
- `Slug` is of type `Slug` VO (not `string`)
- `Name` is of type `TicketTypeName` VO (not `string`)
- `TimeSlots` is of type `Slug[]` (not `string[]`)

#### Scenario: TicketTypeSnapshot is created with typed fields
- **WHEN** a `TicketTypeSnapshot` record is constructed
- **THEN** its `Slug`, `Name`, and `TimeSlots` properties use the respective VO types
