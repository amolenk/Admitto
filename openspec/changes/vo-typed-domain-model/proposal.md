## Why

Now that Vogen is in use across the codebase, the domain model should be fully type-safe end-to-end. Several entities, domain events, and composite value objects still carry raw `string`, `int`, or `Guid` fields where a typed VO already exists or should exist. These primitives weaken the model's expressiveness, allow incorrect assignments to compile silently, and make cross-module contracts harder to reason about. Fixing them now keeps the pattern consistent before more features are added on top.

## What Changes

- **Split `DisplayName`** into domain-specific name VOs (`TeamName`, `EventName`, `TicketTypeName`) so that a team name cannot be accidentally assigned to an event and vice versa.
- **Organization module** – Replace primitive fields in `ApiKey` (`Name`, `KeyPrefix`, `CreatedBy`) with typed VOs. Fix `TicketedEventCreatedDomainEvent.CreationRequestId` (`Guid` → `CreationRequestId`).
- **Registrations module** – Fix `ActivityLog.RegistrationId` (`Guid` → `RegistrationId`). Fix primitive fields inside composite VOs `TicketTypeSnapshot` and `AdditionalDetailField`. Fix `OtpCodeRequestedDomainEvent.EventName` (`string` → `EventName`). Fix `TicketType` entity key (`Entity<string>` → `Entity<TicketTypeId>`). Fix `TimeSlotSlugs: string[]` → `Slug[]`.
- **Email module** – Replace `Guid TeamId`, `Guid TicketedEventId`, `Guid? RegistrationId`, and `string Recipient` in `EmailLog` with typed VOs. Replace `Guid ScopeId` in `EmailSettings` and `EmailTemplate` with a discriminated typed id. Replace primitive fields in `ExternalListItem` and `BulkEmailRecipient` with typed VOs (`EmailAddress`, `RegistrationId`). Replace `string EmailType` in `BulkEmailJob` with a typed VO.

## Capabilities

### New Capabilities

- `split-display-name`: Replace the shared generic `DisplayName` VO with domain-specific name VOs — `TeamName`, `EventName`, and `TicketTypeName` — and update all usages across entities, events, and composite VOs.
- `vo-typed-organization-primitives`: Replace remaining primitive fields in the Organization module's entities and domain events with typed VOs (`ApiKeyName`, `ApiKeyPrefix`, `CreatedBy`→`UserId`, `CreationRequestId`).
- `vo-typed-registrations-primitives`: Replace remaining primitives in the Registrations module — `ActivityLog.RegistrationId`, `TicketType` entity key, `TimeSlotSlugs`, fields inside `TicketTypeSnapshot` and `AdditionalDetailField`, and `OtpCodeRequestedDomainEvent.EventName`.
- `vo-typed-email-primitives`: Replace remaining primitives in the Email module — `EmailLog` Guid fields and `Recipient`, `EmailSettings`/`EmailTemplate` `ScopeId`, and primitive fields in `ExternalListItem`, `BulkEmailRecipient`, and `BulkEmailJob`.

### Modified Capabilities

## Impact

- **All three modules** (Organization, Registrations, Email) — entity and event property types change; EF Core column mappings may need converter updates if the underlying type changes.
- **Composite VOs** — `TicketTypeSnapshot`, `AdditionalDetailField`, `ExternalListItem`, `BulkEmailRecipient` — their construction sites and usages change.
- **Cross-module contracts** — `Contracts/` facades that surface affected types may need updates.
- **Integration / module event payloads** — events carrying newly typed fields must remain serialisation-compatible or have a migration path.
- **Admin UI generated SDK** — any changed request/response shapes require an SDK regeneration.
- **Tests** — builder helpers and fixture factories that construct the affected types need updating.
