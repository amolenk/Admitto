## ADDED Requirements

### Requirement: EmailLog uses typed VOs for identity and recipient fields
The system SHALL replace primitive fields on `EmailLog` with typed VOs:
- `Guid TeamId` → `TeamId` VO
- `Guid TicketedEventId` → `TicketedEventId` VO
- `Guid? RegistrationId` → `RegistrationId?` VO
- `string Recipient` → `EmailAddress` VO

#### Scenario: EmailLog is created with typed fields
- **WHEN** an `EmailLog` entity is constructed
- **THEN** `TeamId`, `TicketedEventId`, `RegistrationId`, and `Recipient` use the respective VO types

### Requirement: EmailSettings uses EmailScopeId VO
The system SHALL introduce an `EmailScopeId` Vogen value object (Guid-backed) in `Email/Domain/ValueObjects/` and replace the `Guid ScopeId` field on `EmailSettings` with `EmailScopeId ScopeId`.

#### Scenario: EmailSettings is created with typed ScopeId
- **WHEN** `EmailSettings.Create` is called
- **THEN** the `ScopeId` property is of type `EmailScopeId` VO, not `Guid`

### Requirement: EmailTemplate uses EmailScopeId VO
The system SHALL replace the `Guid ScopeId` field on `EmailTemplate` with `EmailScopeId ScopeId`, using the same `EmailScopeId` VO introduced for `EmailSettings`.

#### Scenario: EmailTemplate is created with typed ScopeId
- **WHEN** `EmailTemplate.Create` is called
- **THEN** the `ScopeId` property is of type `EmailScopeId` VO, not `Guid`

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
