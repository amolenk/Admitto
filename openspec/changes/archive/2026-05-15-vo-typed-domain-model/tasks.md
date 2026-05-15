## 1. Split DisplayName VO

- [x] 1.1 Create `TeamName` Vogen VO (`Shared/Kernel/ValueObjects/TeamName.cs`) — string-backed, non-empty, same validation as current DisplayName
- [x] 1.2 Create `EventName` Vogen VO (`Shared/Kernel/ValueObjects/EventName.cs`) — string-backed, non-empty, same validation as current DisplayName
- [x] 1.3 Create `TicketTypeName` Vogen VO (`Registrations/Domain/ValueObjects/TicketTypeName.cs`) — string-backed, non-empty
- [x] 1.4 Replace `DisplayName` with `TeamName` on `Team.Name` and update all construction sites, EF config, and tests
- [x] 1.5 Replace `DisplayName` with `EventName` on `TicketedEvent.Name`, `TicketedEventCreationRequestedDomainEvent`, `TicketedEventCreationRequestedDomainEvent` handler, and all construction sites, EF config, and tests
- [x] 1.6 Replace `string EventName` on `OtpCodeRequestedDomainEvent` with `EventName` VO and update handler
- [x] 1.7 Replace `DisplayName` with `TicketTypeName` on `TicketType.Name` and update all construction sites, EF config, and tests
- [x] 1.8 Replace `string Name` in `TicketTypeSnapshot` record with `TicketTypeName`
- [x] 1.9 Delete `Shared/Kernel/ValueObjects/DisplayName.cs` and confirm no remaining references compile

## 2. Organization Module — Typed Primitives

- [x] 2.1 Create `ApiKeyName` Vogen VO (`Organization/Domain/ValueObjects/ApiKeyName.cs`) — string-backed, non-empty, max 100 chars
- [x] 2.2 Replace `string Name` on `ApiKey` entity with `ApiKeyName Name`; update `ApiKey.Create`, command, HTTP request, EF config, response DTOs, and tests
- [x] 2.3 Replace `Guid CreationRequestId` on `TicketedEventCreatedDomainEvent` with `CreationRequestId` VO; update all construction sites and event handlers

## 3. Registrations Module — Typed Primitives

- [x] 3.1 Replace `Guid RegistrationId` on `ActivityLog` with `RegistrationId` VO; update `ActivityLog.Create`, EF config, and tests
- [x] 3.2 Change `TicketType` from `Entity<string>` to `Entity<TicketTypeId>`; update all construction sites to call `TicketTypeId.From(rawString)`, update EF config, and tests
- [x] 3.3 Replace `string[] TimeSlotSlugs` on `TicketType` with `Slug[]`; update construction sites, EF config, and tests
- [x] 3.4 Update `TicketTypeSnapshot` record: `string Slug` → `Slug`, `string[] TimeSlots` → `Slug[]` (Name was handled in task 1.8); update all construction sites and tests

## 4. Email Module — Typed Primitives

- [x] 4.1 Create `EmailScopeId` Vogen VO (`Email/Domain/ValueObjects/EmailScopeId.cs`) — Guid-backed
- [x] 4.2 Replace `Guid ScopeId` on `EmailSettings` with `EmailScopeId ScopeId`; update `EmailSettings.Create`, EF config, query handlers, and tests
- [x] 4.3 Replace `Guid ScopeId` on `EmailTemplate` with `EmailScopeId ScopeId`; update `EmailTemplate.Create`, EF config, query handlers, and tests
- [x] 4.4 Replace `string Email` on `ExternalListItem` with `EmailAddress` VO; update all construction sites
- [x] 4.5 Replace `string Email` on `BulkEmailRecipient` with `EmailAddress` VO; update construction sites, fan-out worker, and tests
- [x] 4.6 Replace `Guid? RegistrationId` on `BulkEmailRecipient` with `RegistrationId?` VO; update construction sites and tests
- [x] 4.7 Replace `Guid TeamId`, `Guid TicketedEventId`, `Guid? RegistrationId`, and `string Recipient` on `EmailLog` with `TeamId`, `TicketedEventId`, `RegistrationId?`, and `EmailAddress`; update `EmailLog` construction, EF config, query handlers, and tests

## 5. Verification

- [x] 5.1 Run architecture tests: `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`
- [x] 5.2 Run domain tests: `dotnet test tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj`
- [x] 5.3 Run integration tests: `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj`
- [x] 5.4 Run API tests: `tests/Admitto.Api.Tests/bin/Debug/net10.0/Admitto.Api.Tests`
- [x] 5.5 Regenerate Admin UI SDK and verify TypeScript build: `aspire start --isolated && aspire wait api && curl -sf http://<api-url>/openapi/v1.json -o src/Admitto.UI.Admin/openapi-spec.json && cd src/Admitto.UI.Admin && pnpm openapi-ts && pnpm build`
