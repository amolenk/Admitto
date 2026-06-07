# VO-Typed Email Primitives Specification

## Purpose

Replace primitive fields (`Guid`, `string`) on Email module entities with typed value objects so that identity and contact fields are strongly typed at compile time and cannot be misassigned.

## Requirements

### Requirement: EmailLog uses typed VOs for identity and recipient fields
The system SHALL replace primitive fields on `EmailLog` with typed VOs:
- `Guid TeamId` → `TeamId` VO
- `Guid TicketedEventId` → `TicketedEventId` VO
- `Guid? RegistrationId` → `RegistrationId?` VO
- `string Recipient` → `EmailAddress` VO

#### Scenario: EmailLog is created with typed fields
- **WHEN** an `EmailLog` entity is constructed
- **THEN** `TeamId`, `TicketedEventId`, `RegistrationId`, and `Recipient` use the respective VO types

### Requirement: EmailSettings uses explicit scope VOs
The system SHALL use the shared `TeamId` value object and optional `TicketedEventId` value object on `EmailSettings`; it SHALL NOT use a generic email scope id.

#### Scenario: EmailSettings is created with explicit scope ids
- **WHEN** `EmailSettings.Create` is called
- **THEN** `TeamId` is required and `TicketedEventId` is nullable

### Requirement: EmailTemplate uses explicit scope VOs
The system SHALL use the shared `TeamId` value object and optional `TicketedEventId` value object on `EmailTemplate`; it SHALL NOT use a generic email scope id.

#### Scenario: EmailTemplate is created with explicit scope ids
- **WHEN** `EmailTemplate.Create` is called
- **THEN** `TeamId` is required and `TicketedEventId` is nullable

### Requirement: ExternalListItem uses EmailAddress VO
The system SHALL replace the `string Email` field on `ExternalListItem` with the existing `EmailAddress` VO.

#### Scenario: ExternalListItem carries typed email
- **WHEN** an `ExternalListItem` record is constructed
- **THEN** the `Email` field is of type `EmailAddress`, not `string`

### Requirement: BulkEmailRecipient uses typed VOs for email and registration id
The system SHALL replace primitive fields on `BulkEmailRecipient`:
- `string Email` → `EmailAddress` VO
- `Guid? RegistrationId` → `RegistrationId?` VO

#### Scenario: BulkEmailRecipient is constructed with typed email
- **WHEN** a `BulkEmailRecipient` is created with a valid email address
- **THEN** the `Email` property is of type `EmailAddress`, not `string`

#### Scenario: BulkEmailRecipient is constructed with typed registration id
- **WHEN** a `BulkEmailRecipient` is created with a registration id
- **THEN** the `RegistrationId` property is of type `RegistrationId?` VO, not `Guid?`
