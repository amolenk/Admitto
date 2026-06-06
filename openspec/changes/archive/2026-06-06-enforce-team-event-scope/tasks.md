## 1. Badges — Data Model & Migration

- [x] 1.1 Add `TeamId` property to `BadgesEvent` entity (`src/Admitto.Core/Badges/Domain/Entities/BadgesEvent.cs`) and update its factory method to accept `TeamId`
- [x] 1.2 Update `BadgesEventEntityConfiguration` to map `team_id` column (non-nullable `uuid`)
- [x] 1.3 Generate EF Core migration: add `team_id uuid NULL` to `badges_events`, backfill from `registrations.ticketed_events`, then alter to `NOT NULL`
- [x] 1.4 Add `TeamId` to `CreateBadgesEventCommand` and propagate it from `TicketedEventCreatedIntegrationEventHandler` (already carries `TeamId`) through the command handler

## 2. Badges — Handler Scope Enforcement

- [x] 2.1 Add `TeamId` to all badge command objects and their endpoint wiring: `AddBadgeTypeCommand`, `RenameBadgeTypeCommand`, `DeleteBadgeTypeCommand`, `AddBadgeInstanceCommand`, `UpdateBadgeInstanceCommand`, `DeleteBadgeInstanceCommand`
- [x] 2.2 Add `TeamId` to all badge query objects and their endpoint wiring: `ListBadgeTypesQuery`, `ListBadgeInstancesQuery`, `ExportBadgeCsvQuery`
- [x] 2.3 Update all badge handlers to load `BadgesEvent` with `e => e.Id == eventId && e.TeamId == teamId` predicate (9 handlers total)

## 3. Registrations — TicketedEvent Handler Scope Enforcement

- [x] 3.1 Add `TeamId` to `ArchiveTicketedEventCommand` and update endpoint + handler to scope load by team
- [x] 3.2 Add `TeamId` to `ConfigureRegistrationPolicyCommand` and update endpoint + handler
- [x] 3.3 Add `TeamId` to `ConfigureReconfirmPolicyCommand` and update endpoint + handler
- [x] 3.4 Add `TeamId` to `UpdateTicketedEventTimeZoneCommand` and update endpoint + handler
- [x] 3.5 Add `TeamId` to `UpdateAdditionalDetailSchemaCommand` and update endpoint + handler
- [x] 3.6 Add `TeamId` to `UpdateTicketedEventDetailsCommand` and update endpoint + handler
- [x] 3.7 Add `TeamId` to `GetTicketedEventDetailsQuery` and update endpoint + handler to scope read by team

## 4. Registrations — TicketCatalog Data Model & Handler Scope Enforcement

- [x] 4.0 Add `TeamId` property to `TicketCatalog` entity and update its `Create` factory to accept `TeamId`
- [x] 4.1 Update `TicketCatalogEntityConfiguration` to map `team_id` column (non-nullable `uuid`)
- [x] 4.2 Generate EF Core migration: add `team_id uuid NULL` to `ticket_catalogs`, backfill from `registrations.ticketed_events`, then alter to `NOT NULL`
- [x] 4.3 Propagate `TeamId` into `TicketCatalog.Create()` in the handler that creates the catalog (integration event handler for `TicketedEventCreatedIntegrationEvent`)
- [x] 4.4 Add `TeamId` to `AddTicketTypeCommand` and update endpoint + handler to scope load with `tc => tc.Id == eventId && tc.TeamId == teamId`
- [x] 4.5 Add `TeamId` to `UpdateTicketTypeCommand` and update endpoint + handler similarly
- [x] 4.6 Add `TeamId` to `GetTicketTypesQuery` and update endpoint + handler similarly

## 5. Registrations — Coupon Handler Scope Enforcement

- [x] 5.1 Add `TeamId` to `ListCouponsQuery` and update endpoint + handler to add `teamId` filter
- [x] 5.2 Add `TeamId` to `GetCouponDetailsQuery` and update endpoint + handler to scope load by `(couponId, teamId)`
- [x] 5.3 Add `TeamId` to `RevokeCouponCommand` and update endpoint + handler to scope load by `teamId`

## 6. Registrations — Registration & Waitlist Handler Scope Enforcement

- [x] 6.1 Add `TeamId` to `CancelRegistrationCommand` and update endpoint + handler to include `teamId` in the registration load predicate
- [x] 6.2 Add `TeamId` to `GetWaitlistDetailsQuery` and update endpoint + handler to scope load with `w => w.EventId == eventId && w.TeamId == teamId` (Waitlist already stores `TeamId` in DB)
- [x] 6.3 Add `TeamId` to `RemoveWaitlistEntryCommand` and update endpoint + handler similarly

## 7. Email — BulkEmail Handler Scope Enforcement

- [x] 7.1 Add `TeamId` to `GetBulkEmailsQuery` and update endpoint + handler to filter by both `teamId` and `ticketedEventId`
- [x] 7.2 Add `TeamId` + `TicketedEventId` to `GetBulkEmailQuery` and update endpoint + handler to scope load by `(bulkEmailJobId, teamId)`
- [x] 7.3 Add `TeamId` + `TicketedEventId` to `CancelBulkEmailCommand` and update endpoint + handler to scope load by `(bulkEmailJobId, teamId)`
- [x] 7.4 Fix `GetAttendeeEmailsHandler`: the `TeamId` is already in `GetAttendeeEmailsQuery` but unused — add it to the `Where` predicate

## 8. Tests

- [x] 8.1 Add integration test for cross-team badge operation rejection (one representative handler, e.g. `AddBadgeType`)
- [x] 8.2 Add integration test for cross-team TicketedEvent operation rejection (e.g. `GetTicketedEventDetails`)
- [x] 8.3 Add integration test for cross-team Coupon operation rejection (e.g. `GetCouponDetails`)
- [x] 8.4 Add integration test for cross-team BulkEmail operation rejection (e.g. `GetBulkEmails`)
- [x] 8.5 Add integration test verifying `GetAttendeeEmails` actually filters by `TeamId`

## 9. Architecture Test

- [x] 9.1 Add `SecurityConventionTests.cs` to `Admitto.Core.ArchTests` with a Roslyn-based test that scans all handler files in `UseCases/`, finds `GetAsync` lambda predicates containing `EventId`, and asserts each also contains `TeamId` — failing CI immediately on any future regression
