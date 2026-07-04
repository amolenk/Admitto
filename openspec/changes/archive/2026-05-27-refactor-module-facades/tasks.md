## 1. Registrations Facade Contract

- [x] 1.1 Rename `GetTicketedEventEmailContextAsync` → `GetEventRegistrationSnapshotAsync` in `IRegistrationsFacade`
- [x] 1.2 Rename `TicketedEventEmailContextDto` → `EventRegistrationSnapshotDto`
- [x] 1.3 Rename `QueryRegistrationsAsync` → `GetRegistrationsAsync` in `IRegistrationsFacade`; change `eventId` parameter type from `TicketedEventId` to `Guid`
- [x] 1.4 Change `GetReconfirmTriggerSpecAsync` `eventId` parameter type from `TicketedEventId` to `Guid` in `IRegistrationsFacade`
- [x] 1.5 Change `GetAdditionalDetailSchemaAsync` `eventId` parameter type from `TicketedEventId` to `Guid` in `IRegistrationsFacade`
- [x] 1.6 Remove `QueryRegistrationsForBadgeExportAsync` from `IRegistrationsFacade`
- [x] 1.7 Remove `BadgeExportRegistrationDto`

## 2. Registrations Facade Implementation

- [x] 2.1 Update `RegistrationsFacade` to implement the renamed `GetEventRegistrationSnapshotAsync` and `GetRegistrationsAsync` methods with `Guid` parameters (wrap to VO internally)
- [x] 2.2 Update `GetReconfirmTriggerSpecAsync` and `GetAdditionalDetailSchemaAsync` in `RegistrationsFacade` to accept `Guid` (wrap to VO internally)
- [x] 2.3 Remove `QueryRegistrationsForBadgeExportAsync` implementation from `RegistrationsFacade`

## 3. Organization Facade

- [x] 3.1 Rename `ValidateApiKeyAsync` → `LookupApiKeyOwnerAsync` in `IOrganizationFacade`
- [x] 3.2 Rename the method in `OrganizationFacade` and `CachingOrganizationFacade` implementations

## 4. Update Callers

- [x] 4.1 Update `ApiKeyAuthenticationHandler` to call `LookupApiKeyOwnerAsync`
- [x] 4.2 Update all Email module event handlers calling `GetTicketedEventEmailContextAsync` → `GetEventRegistrationSnapshotAsync`; pass `Guid` instead of `TicketedEventId`
- [x] 4.3 Update `RequestReconfirmationsJob` and `BulkEmailRecipientResolver` to call `GetRegistrationsAsync`; pass `Guid` instead of `TicketedEventId`
- [x] 4.4 Update `ReconcileReconfirmationSchedulingHandler` and schedule event handlers to call `GetReconfirmTriggerSpecAsync` / `GetActiveReconfirmTriggerSpecsAsync` with `Guid`
- [x] 4.5 Update `ExportBadgeCsvHandler` (Badges): replace `QueryRegistrationsForBadgeExportAsync` with `GetRegistrationsAsync(eventId.Value, new QueryRegistrationsDto(RegistrationStatus: Registered, TicketTypeIds: ticketTypeIds.Select(id => id.Value).ToList()))` and project `RegistrationListItemDto` → FirstName/LastName/Email/AdditionalDetails locally
- [x] 4.6 Update `GetAdditionalDetailSchemaAsync` call in `ExportBadgeCsvHandler` to pass `eventId.Value`

## 5. Verify

- [x] 5.1 Run architecture tests (`dotnet test tests/Admitto.Core.ArchTests/...`)
- [x] 5.2 Run domain and integration tests (`dotnet test tests/Admitto.Core.DomainTests/...` and `dotnet test tests/Admitto.Core.IntegrationTests/...`)
- [x] 5.3 Run API tests (`dotnet test tests/Admitto.Api.Tests/...`)
