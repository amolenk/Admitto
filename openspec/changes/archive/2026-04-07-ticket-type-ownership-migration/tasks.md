## 1. Registrations Domain — TicketCatalog aggregate and value objects

- [x] 1.1 Move `Capacity` value object from `Admitto.Module.Organization/Domain/ValueObjects/Capacity.cs` to `Admitto.Module.Registrations/Domain/ValueObjects/Capacity.cs`; update namespace to `Amolenk.Admitto.Module.Registrations.Domain.ValueObjects`
- [x] 1.2 Move `TimeSlot` value object from `Admitto.Module.Organization/Domain/ValueObjects/TimeSlot.cs` to `Admitto.Module.Registrations/Domain/ValueObjects/TimeSlot.cs`; update namespace
- [x] 1.3 Create `EventLifecycleStatus` enum in `Admitto.Module.Registrations/Domain/ValueObjects/EventLifecycleStatus.cs` with values `Active = 0`, `Cancelled = 1`, `Archived = 2`
- [x] 1.4 Create `TicketType` entity in `Admitto.Module.Registrations/Domain/Entities/TicketType.cs`: inherits `Entity<string>` (keyed by slug), properties `DisplayName Name`, `TimeSlot[] TimeSlots`, `int? MaxCapacity`, `int UsedCapacity`, `bool IsCancelled`; methods `UpdateName`, `UpdateCapacity`, `Cancel`, `ClaimWithEnforcement`, `ClaimUncapped` (port claim logic from `TicketCapacity`)
- [x] 1.5 Create `TicketCatalog` aggregate in `Admitto.Module.Registrations/Domain/Entities/TicketCatalog.cs`: inherits `Aggregate<TicketedEventId>`, owns `List<TicketType>`; methods `AddTicketType(slug, name, timeSlots, capacity?)` (rejects duplicates), `UpdateTicketType(slug, name?, capacity?)`, `CancelTicketType(slug)`, `Claim(slugs, enforce)` (delegates to each TicketType), `GetTicketType(slug)`; static factory `Create(eventId)`
- [x] 1.6 Add `EventLifecycleStatus EventLifecycleStatus` property to `EventRegistrationPolicy` (default `Active`); add methods `SetCancelled()` and `SetArchived()` (idempotent — no-op if already in target state); add `IsEventActive => EventLifecycleStatus == EventLifecycleStatus.Active` computed property
- [x] 1.7 Write domain-level unit tests for `TicketCatalog` in `tests/Admitto.Module.Registrations.Domain.Tests`: add ticket type, reject duplicate slug, update capacity, update name, cancel ticket type, reject double-cancel, claim with enforcement (success, at capacity, null capacity), claim uncapped, claim multiple slugs
- [x] 1.8 Write domain-level unit tests for `EventRegistrationPolicy` lifecycle status: default Active, set Cancelled, set Archived, idempotent Cancelled, idempotent Archived, `IsEventActive` returns false when Cancelled/Archived

## 2. Organization Domain — Slim TicketedEvent and add lifecycle events

- [x] 2.1 Create `TicketedEventCancelledDomainEvent` in `Admitto.Module.Organization/Domain/DomainEvents/` with property `TicketedEventId TicketedEventId`
- [x] 2.2 Create `TicketedEventArchivedDomainEvent` in `Admitto.Module.Organization/Domain/DomainEvents/` with property `TicketedEventId TicketedEventId`
- [x] 2.3 Update `TicketedEvent.Cancel()`: remove ticket type cascade loop, raise `TicketedEventCancelledDomainEvent`
- [x] 2.4 Update `TicketedEvent.Archive()`: raise `TicketedEventArchivedDomainEvent`
- [x] 2.5 Remove `AddTicketType`, `UpdateTicketType`, `CancelTicketType` methods and the `_ticketTypes` / `TicketTypes` collection from `TicketedEvent`
- [x] 2.6 Delete `TicketType.cs` value object from `Admitto.Module.Organization/Domain/ValueObjects/`
- [x] 2.7 Delete `Capacity.cs` and `TimeSlot.cs` from `Admitto.Module.Organization/Domain/ValueObjects/`
- [x] 2.8 Delete `TicketTypeAddedDomainEvent.cs` and `TicketTypeCapacityChangedDomainEvent.cs` from `Admitto.Module.Organization/Domain/DomainEvents/`
- [x] 2.9 Update `TicketedEventEntityConfiguration` in Organization Infrastructure: remove `OwnsMany<TicketType>` JSON column mapping for `ticket_types`
- [x] 2.10 Update Organization domain tests in `TicketedEventTests.cs`: remove scenarios that test ticket type CRUD methods; update `Cancel()` tests to assert `TicketedEventCancelledDomainEvent` is raised (no cascade); add `Archive()` test asserting `TicketedEventArchivedDomainEvent` is raised; update `TicketedEventBuilder` to remove `WithTicketType` helper

## 3. Organization Contracts — Update module events and facade

- [x] 3.1 Create `TicketedEventCancelledModuleEvent` record in `Admitto.Module.Organization.Contracts/` implementing `IModuleEvent` with `Guid TicketedEventId`
- [x] 3.2 Create `TicketedEventArchivedModuleEvent` record in `Admitto.Module.Organization.Contracts/` implementing `IModuleEvent` with `Guid TicketedEventId`
- [x] 3.3 Delete `TicketTypeAddedModuleEvent.cs` from `Admitto.Module.Organization.Contracts/`
- [x] 3.4 Delete `TicketTypeCapacityChangedModuleEvent.cs` from `Admitto.Module.Organization.Contracts/`
- [x] 3.5 Delete `TicketTypeDto.cs` from `Admitto.Module.Organization.Contracts/`
- [x] 3.6 Remove `GetTicketTypesAsync` and `IsEventActiveAsync` methods from `IOrganizationFacade` interface

## 4. Organization Application — Message policy, facade, and remove ticket type use cases

- [x] 4.1 Update `OrganizationMessagePolicy`: remove `TicketTypeAddedDomainEvent` and `TicketTypeCapacityChangedDomainEvent` mappings; add `TicketedEventCancelledDomainEvent → TicketedEventCancelledModuleEvent` and `TicketedEventArchivedDomainEvent → TicketedEventArchivedModuleEvent` mappings
- [x] 4.2 Update `OrganizationFacade`: remove `GetTicketTypesAsync` and `IsEventActiveAsync` implementations; remove `GetTicketTypes` using/import
- [x] 4.3 Delete `AddTicketType/` folder (command, handler, AdminApi endpoint, request, validator) from `Application/UseCases/TicketedEvents/`
- [x] 4.4 Delete `UpdateTicketType/` folder (command, handler, AdminApi endpoint, request, validator) from `Application/UseCases/TicketedEvents/`
- [x] 4.5 Delete `CancelTicketType/` folder (command, handler, AdminApi endpoint, request) from `Application/UseCases/TicketedEvents/`
- [x] 4.6 Delete `GetTicketTypes/` folder (query, handler) from `Application/UseCases/TicketedEvents/`
- [x] 4.7 Update `OrganizationApiEndpoints`: remove `.MapAddTicketType()` from `eventGroup`, remove `.MapUpdateTicketType()` and `.MapCancelTicketType()` from `eventGroup.MapGroup("/ticket-types")`; if the `/ticket-types` sub-group is empty, remove it entirely
- [x] 4.8 Update `GetTicketedEvent` handler and response DTO: remove ticket type data from the response (ticket types are now a Registrations concern)
- [x] 4.9 Delete Organization integration tests for ticket type use cases: `AddTicketType/` (fixture + tests), `UpdateTicketType/` (fixture + tests), `CancelTicketType/` (fixture + tests) from `tests/Admitto.Module.Organization.Tests/Application/UseCases/TicketedEvents/`

## 5. Registrations Infrastructure — EF configuration

- [x] 5.1 Create `TicketCatalogConfiguration` in `Admitto.Module.Registrations/Infrastructure/`: map `TicketCatalog` to `ticket_catalogs` table in registrations schema, `TicketType` as owned JSON collection, configure `TicketedEventId` as primary key
- [x] 5.2 Update `EventRegistrationPolicyConfiguration`: add `EventLifecycleStatus` column (integer, default `Active`)
- [x] 5.3 Update `RegistrationsDbContext`: add `DbSet<TicketCatalog> TicketCatalogs`; register `TicketCatalogConfiguration` in `OnModelCreating`
- [x] 5.4 Update `IRegistrationsWriteStore`: add `DbSet<TicketCatalog> TicketCatalogs` property

## 6. Registrations Application — Ticket type CRUD use cases

- [x] 6.1 Create `AddTicketType` use case under `Application/UseCases/TicketTypeManagement/AddTicketType/`: `AddTicketTypeCommand` (TicketedEventId, Slug, Name, TimeSlots, Capacity?), `AddTicketTypeHandler` (load `EventRegistrationPolicy` → check `IsEventActive`, load or create `TicketCatalog` → call `AddTicketType`, save), FluentValidation validator, admin API endpoint (`POST /teams/{teamSlug}/events/{eventSlug}/ticket-types`)
- [x] 6.2 Create `UpdateTicketType` use case under `Application/UseCases/TicketTypeManagement/UpdateTicketType/`: `UpdateTicketTypeCommand` (TicketedEventId, Slug, Name?, Capacity?), `UpdateTicketTypeHandler` (load policy → check active, load catalog → update), validator, admin API endpoint (`PUT /teams/{teamSlug}/events/{eventSlug}/ticket-types/{slug}`)
- [x] 6.3 Create `CancelTicketType` use case under `Application/UseCases/TicketTypeManagement/CancelTicketType/`: `CancelTicketTypeCommand` (TicketedEventId, Slug), `CancelTicketTypeHandler` (load policy → check active, load catalog → cancel), admin API endpoint (`POST /teams/{teamSlug}/events/{eventSlug}/ticket-types/{slug}/cancel`)
- [x] 6.4 Create `GetTicketTypes` use case under `Application/UseCases/TicketTypeManagement/GetTicketTypes/`: `GetTicketTypesQuery` (TicketedEventId), `GetTicketTypesHandler` (load `TicketCatalog`, map to response DTOs), admin API endpoint (`GET /teams/{teamSlug}/events/{eventSlug}/ticket-types`)
- [x] 6.5 Wire all four ticket type endpoints in `RegistrationsModule.MapRegistrationsAdminEndpoints()` under the `/teams/{teamSlug}/events/{eventSlug}` group

## 7. Registrations Application — Event lifecycle sync handlers

- [x] 7.1 Create `HandleEventCancelled` use case under `Application/UseCases/EventLifecycleSync/HandleEventCancelled/`: `HandleEventCancelledCommand` (TicketedEventId), `HandleEventCancelledHandler` (load or create `EventRegistrationPolicy` → call `SetCancelled()`, save), `EventHandlers/TicketedEventCancelledModuleEventHandler` (maps module event to command via `DeterministicCommandId`)
- [x] 7.2 Create `HandleEventArchived` use case under `Application/UseCases/EventLifecycleSync/HandleEventArchived/`: `HandleEventArchivedCommand` (TicketedEventId), `HandleEventArchivedHandler` (load or create `EventRegistrationPolicy` → call `SetArchived()`, save), `EventHandlers/TicketedEventArchivedModuleEventHandler` (maps module event to command via `DeterministicCommandId`)

## 8. Registrations Application — Update registration and coupon handlers

- [x] 8.1 Update `SelfRegisterAttendeeHandler`: remove `IOrganizationFacade` dependency; replace `IsEventActiveAsync()` call with loading `EventRegistrationPolicy` and checking `IsEventActive` / `EventLifecycleStatus`; replace `GetTicketTypesAsync()` with loading `TicketCatalog` locally; replace `EventCapacity.Claim()` with `TicketCatalog.Claim(slugs, enforce: true)`; update ticket type validation to use `TicketCatalog.TicketTypes` instead of `TicketTypeDto[]`; build `TicketTypeSnapshot` from local `TicketType` entities
- [x] 8.2 Update `RegisterWithCouponHandler`: same changes as 8.1 but with `enforce: false` for `Claim()`; ensure coupon creates missing `TicketCapacity` entries by using `TicketCatalog.Claim(slugs, enforce: false)`; remove facade import
- [x] 8.3 Update `CreateCouponHandler`: remove `IOrganizationFacade` dependency; replace `IsEventActiveAsync()` with `EventRegistrationPolicy.IsEventActive` check; replace `GetTicketTypesAsync()` with loading `TicketCatalog` to build `TicketTypeInfo` list from local `TicketType` entities

## 9. Remove old capacity sync code

- [x] 9.1 Delete `InitializeTicketCapacity/` folder (command, handler, EventHandlers/) from `Application/UseCases/Capacity/`
- [x] 9.2 Delete `UpdateTicketCapacity/` folder (command, handler, EventHandlers/) from `Application/UseCases/Capacity/`
- [x] 9.3 Delete `EventCapacity.cs` and `TicketCapacity.cs` from `Admitto.Module.Registrations/Domain/Entities/`
- [x] 9.4 Delete `EventCapacityConfiguration.cs` (or equivalent `TicketedEventCapacityEntityConfiguration`) from Registrations Infrastructure
- [x] 9.5 Remove `DbSet<EventCapacity> EventCapacities` from `RegistrationsDbContext` and `IRegistrationsWriteStore`
- [x] 9.6 Delete `CapacitySyncTests.cs` and `TicketedEventCapacityBuilder.cs` from `tests/Admitto.Module.Registrations.Tests/`

## 10. CLI commands

- [x] 10.1 Update `AddTicketTypeCommand.cs` in `src/Admitto.Cli/Commands/Events/TicketType/` to route through Registrations module endpoints (URL patterns unchanged — verify HTTP method and path are still correct)
- [x] 10.2 Update `UpdateTicketTypeCommand.cs` — same URL pattern, now served by Registrations
- [x] 10.3 Update `CancelTicketTypeCommand.cs` — same URL pattern, now served by Registrations

## 11. Database migrations

- [x] 11.1 Add an Organization module EF migration to remove the `ticket_types` JSON column from the `ticketed_events` table
- [x] 11.2 Add a Registrations module EF migration: create `ticket_catalogs` table (with `TicketType[]` as JSON column), add `event_lifecycle_status` column to `event_registration_policies`, drop `event_capacity` / `ticket_capacities` tables
- [x] 11.3 Run `dotnet run --project src/Admitto.Migrations` against a local database and verify all migrations apply cleanly

## 12. Tests

- [x] 12.1 Create Registrations integration tests for `AddTicketType`: add to active event, add with no capacity, reject duplicate slug, reject on cancelled event, reject on archived event
- [x] 12.2 Create Registrations integration tests for `UpdateTicketType`: update capacity, update name, reject on cancelled event
- [x] 12.3 Create Registrations integration tests for `CancelTicketType`: cancel active type, reject double-cancel, reject on cancelled event
- [x] 12.4 Create Registrations integration tests for `GetTicketTypes`: list with mixed active/cancelled types, empty list
- [x] 12.5 Create Registrations integration tests for lifecycle sync: `HandleEventCancelled` synced to existing policy, creates policy if none, idempotent; `HandleEventArchived` synced to existing/cancelled policy, creates policy if none, idempotent
- [x] 12.6 Update `SelfRegisterAttendeeTests` and `SelfRegisterAttendeeFixture`: remove Organization facade mocking, seed `TicketCatalog` and `EventRegistrationPolicy` (with lifecycle status) directly; verify existing scenarios still pass
- [x] 12.7 Update `RegisterWithCouponTests` and `RegisterWithCouponFixture`: same changes as 12.6; verify existing scenarios still pass
- [x] 12.8 Update `CreateCouponTests` and `CreateCouponFixture`: remove Organization facade mocking, seed local data; verify existing scenarios still pass
- [x] 12.9 Run `dotnet test tests/Admitto.Module.Organization.Domain.Tests` — all pass
- [x] 12.10 Run `dotnet test tests/Admitto.Module.Organization.Tests` — all pass
- [x] 12.11 Run `dotnet test tests/Admitto.Module.Registrations.Domain.Tests` — all pass
- [x] 12.12 Run `dotnet test tests/Admitto.Module.Registrations.Tests` — all pass
- [x] 12.13 Run `dotnet test tests/Admitto.Api.Tests` — all pass

## 13. Documentation

- [x] 13.1 Update `docs/arc42/05-building-block-view.md`: move ticket type management from Organization to Registrations; update module boundary diagrams
- [x] 13.2 Update `docs/arc42/08-crosscutting-concepts.md`: update module event taxonomy (remove ticket type sync events, add lifecycle events); update cross-module data flow description

## 14. Final verification

- [x] 14.1 Run the Aspire AppHost (`dotnet run --project src/Admitto.AppHost`) and verify all services (API, Worker, Migrations) start and report healthy on the Aspire dashboard
