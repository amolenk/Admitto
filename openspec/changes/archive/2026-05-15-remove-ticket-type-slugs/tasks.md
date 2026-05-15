## 1. Domain — Value Objects

- [x] 1.1 Replace `sealed record TimeSlot(Slug)` with a proper Vogen `[ValueObject<string>]` partial struct: non-empty, max length 64, no format constraint
- [x] 1.2 Update `TicketTypeSnapshot` record: replace `Slug Slug` with `TicketTypeId Id`, replace `Slug[] TimeSlots` with `TimeSlot[]`

## 2. Domain — TicketType Entity

- [x] 2.1 Change `TicketType` base from `Entity<string>` to `Entity<TicketTypeId>`
- [x] 2.2 Remove slug constructor parameter; constructor now receives `TicketTypeId id` as first argument
- [x] 2.3 Replace `TimeSlotSlugs: Slug[]` stored property and computed `TimeSlots` with a single `TimeSlots: TimeSlot[]` property
- [x] 2.4 Update `TicketType.Errors` to reference `TicketTypeId` instead of slug strings

## 3. Domain — TicketCatalog Aggregate

- [x] 3.1 Update `AddTicketType` signature: remove `Slug slug` param, add `TicketTypeId id`, change duplicate check from slug equality to name equality (case-insensitive); update `DuplicateTicketTypeSlug` error to `DuplicateTicketTypeName`
- [x] 3.2 Update `UpdateTicketType`, `CancelTicketType`, `FindTicketType`, `GetTicketType` signatures: replace `Slug` with `TicketTypeId`
- [x] 3.3 Update `ValidateSelection` and `Claim` and `Release` to operate on `IReadOnlyList<TicketTypeId>` (or `string`-mapped GUIDs) instead of slug strings
- [x] 3.4 Update all `Errors` inner class entries: rename slug references to id/name as appropriate

## 4. Domain — Coupon Entity

- [x] 4.1 Rename `_allowedTicketTypeSlugs`/`AllowedTicketTypeSlugs` to `_allowedTicketTypeIds`/`AllowedTicketTypeIds`, typed as `List<TicketTypeId>`
- [x] 4.2 Update `Coupon.Create` and validation logic to use `TicketTypeId` lookups
- [x] 4.3 Update `TicketTypeInfo` record used in coupon creation: replace `Slug` key with `TicketTypeId`

## 5. Application — Ticket Type Management

- [x] 5.1 `AddTicketTypeCommand`: remove `Slug` field; handler generates `TicketTypeId.New()` internally
- [x] 5.2 `AddTicketTypeHttpRequest`: remove `Slug` field; response returns `id` (Guid) instead of slug
- [x] 5.3 `AddTicketTypeValidator`: remove slug validation rule; validate `Name` is parseable as `TicketTypeName`
- [x] 5.4 `AddTicketTypeHttpEndpoint`: update response location header and body to use `id` (Guid)
- [x] 5.5 `UpdateTicketTypeCommand` and handler: replace slug parameter with `TicketTypeId`
- [x] 5.6 `UpdateTicketTypeHttpEndpoint`: route `/{slug}` → `/{id:guid}`
- [x] 5.7 `CancelTicketTypeCommand` and handler: replace slug parameter with `TicketTypeId`
- [x] 5.8 `CancelTicketTypeHttpEndpoint`: route `/{slug}` → `/{id:guid}`
- [x] 5.9 `GetTicketTypesHandler` and `TicketTypeDto`: replace `slug` field with `id` (Guid)
- [x] 5.10 `GetPublicTicketTypesHandler` and `PublicTicketTypeDto`: replace `slug` field with `id` (Guid)

## 6. Application — Registrations

- [x] 6.1 `RegisterAttendeeHandler`: update snapshot construction to use `TicketTypeId` and `TimeSlot[]`
- [x] 6.2 `ChangeAttendeeTicketsCommand`: rename `TicketTypeSlugs` → `TicketTypeIds`, typed as `IReadOnlyList<Guid>`
- [x] 6.3 `ChangeAttendeeTicketsHandler`: update all slug string operations to use `TicketTypeId`
- [x] 6.4 `ChangeAttendeeTicketsValidator`: update validation rule for `TicketTypeIds`
- [x] 6.5 `ChangeAttendeeTicketsHttpEndpoint`: update request binding from `TicketTypeSlugs` to `TicketTypeIds`
- [x] 6.6 `GetRegistrationsHandler`: update filter from `TicketTypeSlugs` to `TicketTypeIds`; update projection to emit `id` instead of `slug`
- [x] 6.7 `GetRegistrationDetailsHandler`: update ticket projection to use `TicketTypeId`
- [x] 6.8 `ReleaseTicketsHandler`: update slug extraction to use `TicketTypeId`
- [x] 6.9 `RegistrationsFacade`: update `TicketTypeSlugs` to `TicketTypeIds` in contract DTO

## 7. Application — Coupon Management

- [x] 7.1 `CreateCouponCommand` and `CreateCouponHandler`: rename `AllowedTicketTypeSlugs` → `AllowedTicketTypeIds`
- [x] 7.2 `CreateCouponHttpRequest` and `CreateCouponValidator`: update field name and validation
- [x] 7.3 `GetCouponDetailsHandler` and `CouponDetailsDto`: replace `AllowedTicketTypeSlugs` with `AllowedTicketTypeIds`
- [x] 7.4 `ListCouponsHandler` and `ListCouponsResult`: replace slug field with id field

## 8. Contracts

- [x] 8.1 `RegistrationListItemDto`: rename `TicketTypeSlugs` → `TicketTypeIds`, typed as `IReadOnlyCollection<Guid>`
- [x] 8.2 `QueryRegistrationsDto`: rename `TicketTypeSlugs` filter → `TicketTypeIds`
- [x] 8.3 `RegistrationsIntegrationEventPublisher`: update `TicketTypeItem` construction to use `Id` instead of `Slug`
- [x] 8.4 Integration event contracts: update `TicketTypeItem` record — replace `string Slug` with `Guid Id`

## 9. Email Module

- [x] 9.1 `BulkEmailSourceHttpDto`: rename `TicketTypeSlugs` → `TicketTypeIds`; update `ToCommand` mapping
- [x] 9.2 `BulkEmailRecipientResolver`: update `ticket_type_slugs` template variable key to `ticket_type_ids` (or keep as `ticket_types` and pass names — verify what templates use)

## 10. Activity Log

- [x] 10.1 `TicketsChangedDomainEventHandler` (activity log): update `from`/`to` arrays to use ticket type ids or names instead of slug values

## 11. Infrastructure — EF Configuration & Migration

- [x] 11.1 `TicketCatalogEntityConfiguration`: update `ticket_types` owned entity — change PK from string to `TicketTypeId` (UUID), replace `TimeSlotSlugs` primitive collection with `TimeSlots` (strings), add case-insensitive unique index on `(ticketed_event_id, lower(name))`
- [x] 11.2 `RegistrationEntityConfiguration`: update ticket snapshot `TimeSlots` primitive collection to store `TimeSlot` (string) values
- [x] 11.3 `CouponEntityConfiguration`: rename `allowed_ticket_type_slugs` column to `allowed_ticket_type_ids`
- [x] 11.4 `RegistrationsPostgresExceptionMapping`: update duplicate slug exception mapping to duplicate name
- [x] 11.5 Generate EF Core migration

## 12. Tests

- [x] 12.1 Run architecture tests (`Admitto.Core.ArchTests`) and fix any violations
- [x] 12.2 Update domain tests for `TicketCatalog` and `TicketType`: replace slug-based setup with id/name-based setup
- [x] 12.3 Update domain tests for `Coupon`: replace `AllowedTicketTypeSlugs` with `AllowedTicketTypeIds`
- [x] 12.4 Update integration/API tests for ticket type management endpoints
- [x] 12.5 Update integration/API tests for coupon management, change-attendee-tickets, and registration listing
- [x] 12.6 Regenerate Admin UI SDK (`aspire start --isolated` → `aspire wait api` → `curl spec` → `pnpm openapi-ts`)
