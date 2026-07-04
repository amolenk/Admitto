## 1. Module Scaffolding

- [x] 1.1 Create `Admitto.Core.Module.Badges` namespace with `Domain/`, `Application/`, `Infrastructure/`, and `Contracts/` sub-folders
- [x] 1.2 Create `BadgesDbContext` with `DbSet<BadgesEvent>`, `DbSet<BadgeType>`, and `DbSet<BadgeInstance>` (no `IOutboxDbContext` — Badges module does not publish integration events)
- [x] 1.3 Register the Badges module's EF Core services (`AddModuleDatabaseServices`) and `IRegistrationsFacade` dependency in the module's DI registration helper
- [x] 1.4 Add EF Core migration for `badge_events`, `badge_types`, and `badge_instances` tables (badges schema; no `outbox_messages` table)
- [x] 1.5 Update `Admitto.Core.ArchTests` to include the Badges module namespace in the allowed module list

## 2. Domain Model

- [x] 2.1 Create `BadgesEvent` entity (`EventId`, `Status: Active|Archived`) with domain methods `MarkArchived()`
- [x] 2.2 Create `BadgeType` aggregate root (`BadgeTypeId`, `EventId`, `Name`, `Type: TicketBased|Standalone`, `TicketTypeIds: IReadOnlyList<TicketTypeId>` — empty list for standalone) with `Create(...)` factory and `Rename(...)` mutator including uniqueness precondition support; validate that ticket-based types have at least one entry in `TicketTypeIds`
- [x] 2.3 Create `BadgeInstance` aggregate (`BadgeInstanceId`, `BadgeTypeId`, `DisplayName`, `Notes`) with `Create(...)` factory and `Update(...)` mutator
- [x] 2.4 Create value objects: `BadgeTypeName` (max 200 chars), `BadgeInstanceDisplayName` (max 200 chars), `BadgeInstanceNotes` (max 500 chars)

## 3. IRegistrationsFacade Extension

- [x] 3.1 Add `BadgeExportRegistrationDto` record to `Admitto.Core.Module.Registrations.Contracts` with fields: `FirstName`, `LastName`, `Email`, `TicketTypeName`, `AdditionalDetails`
- [x] 3.2 Add `QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds: IReadOnlyList<TicketTypeId>)` method to `IRegistrationsFacade` interface
- [x] 3.3 Implement `QueryRegistrationsForBadgeExportAsync` in the Registrations module's facade implementation (filter by `Status = Registered` and any matching `TicketTypeId` in the list, deduplicate by `RegistrationId`, join with — or include — ticket type names)

## 4. Integration Event Handling — Event Lifecycle

- [x] 4.1 Create `CreateBadgesEvent` slice: handler creates a `BadgesEvent(Active)` in response to `TicketedEventCreated` integration event
- [x] 4.2 Create `ArchiveBadgesEvent` slice: handler transitions `BadgesEvent` to `Archived` in response to both `TicketedEventCancelled` and `TicketedEventArchived` integration events (idempotent)

## 5. Badge Type Management

- [x] 5.1 Create `AddBadgeType` slice: command, handler (validates event Active, name uniqueness; for ticket-based validates `ticketTypeIds` is non-empty), HTTP endpoint `POST /admin/teams/{teamSlug}/events/{eventId}/badge-types`, request DTO + validator
- [x] 5.2 Create `RenameBadgeType` slice: command, handler (validates event Active, name uniqueness, type not changed), HTTP endpoint `PUT /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}`
- [x] 5.3 Create `DeleteBadgeType` slice: command, handler (validates event Active, cascades delete of instances for standalone types), HTTP endpoint `DELETE /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}`
- [x] 5.4 Create `ListBadgeTypes` slice: query, handler (returns id, name, type, ticketTypeId, instanceCount for standalone), HTTP endpoint `GET /admin/teams/{teamSlug}/events/{eventId}/badge-types`
- [x] 5.5 Register all badge-type endpoints in the Badges module's endpoint registration entry point and wire into `AdminEndpoints.cs`

## 6. Standalone Badge Instance Management

- [x] 6.1 Create `AddBadgeInstance` slice: command, handler (validates event Active, badge type exists and is Standalone), HTTP endpoint `POST /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/instances`
- [x] 6.2 Create `UpdateBadgeInstance` slice: command, handler (validates event Active), HTTP endpoint `PUT /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/instances/{instanceId}`
- [x] 6.3 Create `DeleteBadgeInstance` slice: command, handler (validates event Active), HTTP endpoint `DELETE /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/instances/{instanceId}`
- [x] 6.4 Create `ListBadgeInstances` slice: query, handler (validates badge type is Standalone), HTTP endpoint `GET /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/instances`
- [x] 6.5 Register all badge-instance endpoints

## 7. Badge Export

- [x] 7.1 Create `ExportBadgeCsv` slice: query, handler (ticket-based: calls `IRegistrationsFacade.QueryRegistrationsForBadgeExportAsync`, builds CSV with dynamic additional-detail columns; standalone: reads badge instances, builds CSV with DisplayName + Notes), HTTP endpoint `GET /admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/export`
- [x] 7.2 Ensure the export endpoint returns `Content-Type: text/csv` and a `Content-Disposition: attachment; filename="<badge-type-name>-badges.csv"` header

## 8. Tests

- [x] 8.1 Domain tests: `BadgeType.Create` and `Rename` validation scenarios (duplicate name, inactive event guard)
- [x] 8.2 Domain tests: `BadgeInstance.Create` and `Update` validation scenarios (empty display name, max-length)
- [x] 8.3 Integration tests: `AddBadgeType` — success (ticket-based and standalone), duplicate name rejection, inactive event rejection
- [x] 8.4 Integration tests: `RenameBadgeType` — success, duplicate name rejection
- [x] 8.5 Integration tests: `DeleteBadgeType` — success (ticket-based), cascade delete instances (standalone)
- [x] 8.6 Integration tests: `AddBadgeInstance`, `UpdateBadgeInstance`, `DeleteBadgeInstance` — success and guard scenarios
- [x] 8.7 Integration tests: `ExportBadgeCsv` — ticket-based single ticket type (correct columns, cancelled registrations excluded), ticket-based multiple ticket types with deduplication, standalone (DisplayName + Notes), empty export (header only)
- [x] 8.8 Integration tests: lifecycle event handling — `BadgesEvent` created on `TicketedEventCreated`, archived on `TicketedEventCancelled` and `TicketedEventArchived` (idempotency)
- [x] 8.9 Run `Admitto.Core.ArchTests` and confirm no violations

## 9. Admin UI

- [x] 9.1 Regenerate Admin UI SDK (`aspire start --isolated` → `aspire wait api` → `curl /openapi/v1.json` → `pnpm openapi-ts`)
- [x] 9.2 Add badge types list page under event detail (table with name, type, instance count, actions)
- [x] 9.3 Add create/edit badge type dialog (name field; ticket type selector for ticket-based; type toggle on creation only)
- [x] 9.4 Add standalone badge instances sub-page or drawer (list with add/edit/delete actions)
- [x] 9.5 Add export CSV button per badge type that triggers file download via the export endpoint
