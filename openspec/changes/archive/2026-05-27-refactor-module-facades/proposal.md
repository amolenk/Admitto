## Why

The cross-module facades (`IRegistrationsFacade`, `IOrganizationFacade`) have grown
too use-case focused: method names reference their consumers ("Email", "Badge"), some
methods are redundant with each other, return types are consumer-specific projections
rather than general contracts, and method parameters leak internal domain value objects
into the public facade boundary.

The goal is facades that expose general, reusable methods — small enough to be minimal,
broad enough to serve multiple callers without adding a new method per use case.

## What Changes

- **RENAMED** `IRegistrationsFacade.QueryRegistrationsAsync` → `GetRegistrationsAsync`
- **RENAMED** `IRegistrationsFacade.GetTicketedEventEmailContextAsync` →
  `GetEventRegistrationSnapshotAsync`; return type `TicketedEventEmailContextDto` →
  `EventRegistrationSnapshotDto`
- **REMOVED** `IRegistrationsFacade.QueryRegistrationsForBadgeExportAsync` — redundant
  with `GetRegistrationsAsync`; Badges caller projects from `RegistrationListItemDto`
- **REMOVED** `BadgeExportRegistrationDto` — was a caller-specific projection, not a
  cross-module contract
- **FIXED** All facade method parameters that used domain value objects (`TicketedEventId`)
  now use `Guid`; the facade boundary must not expose domain VOs even if they are shared
- **RENAMED** `IOrganizationFacade.ValidateApiKeyAsync` → `LookupApiKeyOwnerAsync` to
  accurately reflect the return value (`Guid?` owner id) rather than implying a boolean

## Capabilities

### New Capabilities

*(none)*

### Modified Capabilities

- `organization` (internal): `IOrganizationFacade` — method rename only
- `registrations` (internal): `IRegistrationsFacade` — method renames, parameter type
  fixes, one method and one DTO removed
- `badges`: `ExportBadgeCsvHandler` — switches from removed
  `QueryRegistrationsForBadgeExportAsync` to `GetRegistrationsAsync` with an equivalent
  filter; projects `RegistrationListItemDto` locally instead of receiving
  `BadgeExportRegistrationDto`

## Impact

**`IRegistrationsFacade` (Contracts)**
- `GetTicketedEventEmailContextAsync` → `GetEventRegistrationSnapshotAsync`
- `QueryRegistrationsAsync(TicketedEventId, ...)` → `GetRegistrationsAsync(Guid, ...)`
- `GetReconfirmTriggerSpecAsync(TicketedEventId, ...)` → `GetReconfirmTriggerSpecAsync(Guid, ...)`
- `GetActiveReconfirmTriggerSpecsAsync` — unchanged
- `GetAdditionalDetailSchemaAsync(TicketedEventId, ...)` → `GetAdditionalDetailSchemaAsync(Guid, ...)`
- `QueryRegistrationsForBadgeExportAsync` — **deleted**

**DTOs (Contracts)**
- `TicketedEventEmailContextDto` → `EventRegistrationSnapshotDto`
- `BadgeExportRegistrationDto` — **deleted**

**`RegistrationsFacade` (implementation)**
- Update all method signatures to match interface changes
- Remove `QueryRegistrationsForBadgeExportAsync` implementation

**`IOrganizationFacade` (Contracts)**
- `ValidateApiKeyAsync` → `LookupApiKeyOwnerAsync`

**`OrganizationFacade` + `CachingOrganizationFacade` (implementations)**
- Rename method to match interface

**Callers**
- `ApiKeyAuthenticationHandler` — update call site (`LookupApiKeyOwnerAsync`)
- `ExportBadgeCsvHandler` — replace `QueryRegistrationsForBadgeExportAsync` call with
  `GetRegistrationsAsync(eventId, new QueryRegistrationsDto(RegistrationStatus: Registered, TicketTypeIds: ticketTypeIds))`
  and project `RegistrationListItemDto` → FirstName/LastName/Email/AdditionalDetails locally
- All Email module callers of `GetTicketedEventEmailContextAsync` → `GetEventRegistrationSnapshotAsync`
- All callers passing `TicketedEventId` to facade methods — pass `.Value` (Guid) instead
