# VO-Typed Organization Primitives Specification

## Purpose

Replace primitive fields (`Guid`, `string`) on Organization module entities and domain events with typed value objects so that identity fields are strongly typed at compile time and cannot be misassigned.

## Requirements

### Requirement: ApiKey name uses a typed VO
The system SHALL introduce an `ApiKeyName` Vogen value object (string-backed, non-empty, max length enforced) in `Organization/Domain/ValueObjects/` and replace the `string Name` field on `ApiKey` with `ApiKeyName Name`.

#### Scenario: ApiKey is created with typed name
- **WHEN** `ApiKey.Create` is called with a valid name string
- **THEN** the resulting entity's `Name` property is of type `ApiKeyName`

#### Scenario: Empty ApiKey name is rejected
- **WHEN** code attempts to create an `ApiKeyName` from an empty or whitespace string
- **THEN** the VO validation rejects it with an appropriate validation error

### Requirement: TicketedEventCreatedDomainEvent uses CreationRequestId VO
The system SHALL replace the `Guid CreationRequestId` field on `TicketedEventCreatedDomainEvent` with the existing `CreationRequestId` VO.

#### Scenario: Domain event carries typed CreationRequestId
- **WHEN** a `TicketedEventCreatedDomainEvent` is raised
- **THEN** the `CreationRequestId` field is of type `CreationRequestId` VO, not `Guid`

#### Scenario: CreationRequestId is correctly round-tripped through outbox
- **WHEN** a `TicketedEventCreatedDomainEvent` is serialised to the outbox and then deserialised
- **THEN** the `CreationRequestId` value is identical to the original
