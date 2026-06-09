## 1. Domain model

- [ ] 1.1 Promote `BadgeEvent` from `Entity` to `Aggregate<TicketedEventId>`: change base class, add audit fields, add `Version` (inherited from `Aggregate`)
- [ ] 1.2 Create `BadgeType` as a plain domain class (no EF base class): properties `Id`, `Name`, `Kind`, `TicketTypeIds`
- [ ] 1.3 Add `IReadOnlyList<BadgeType> BadgeTypes` to `BadgeEvent` with private backing list
- [ ] 1.4 Add `BadgeEvent.AddBadgeType(name, kind, ticketTypeIds)`: enforces active-event, name uniqueness, ticket-based has ticket types; returns new `BadgeTypeId`
- [ ] 1.5 Add `BadgeEvent.RenameBadgeType(badgeTypeId, newName)`: enforces active-event, finds child, enforces name uniqueness
- [ ] 1.6 Add `BadgeEvent.DeleteBadgeType(badgeTypeId)`: enforces active-event, finds child, removes from list; returns `BadgeKind` so caller can cascade instance deletion
- [ ] 1.7 Add `BadgeEvent.EnsureCanManageInstances(badgeTypeId)`: enforces active-event, finds child, enforces `Kind == Standalone`
- [ ] 1.8 Move all `BadgeEvent.Errors` (EventNotActive) and add new errors (BadgeTypeNotFound, BadgeTypeNameAlreadyExists, NotStandaloneBadgeType) as entity-nested errors on `BadgeEvent`

## 2. Persistence

- [ ] 2.1 Update `BadgesEventEntityConfiguration`: configure `BadgeEvent` as aggregate (add audit columns, xmin row-version); configure `BadgeTypes` as JSON column (`badge_types jsonb`)
- [ ] 2.2 Remove `BadgeTypeEntityConfiguration` entirely
- [ ] 2.3 Update `BadgesDbContext`: remove `DbSet<BadgeType> BadgeTypes`, add `xmin` row-version and audit column conventions for `BadgeEvent`
- [ ] 2.4 Update `IBadgesWriteStore`: remove `BadgeTypes` property
- [ ] 2.5 Generate EF migration: adds `badge_types jsonb NOT NULL DEFAULT '[]'` and audit columns to `badges_events`; migrates existing `badge_types` rows into JSON; drops `badge_types` table

## 3. Mutation handlers

- [ ] 3.1 Rewrite `AddBadgeTypeHandler`: load `BadgeEvent` (tracked), call `badgeEvent.AddBadgeType(...)`, remove manual `BadgeTypes.Add` call
- [ ] 3.2 Rewrite `RenameBadgeTypeHandler`: load `BadgeEvent` with `expectedVersion` (tracked), call `badgeEvent.RenameBadgeType(...)`, remove separate `BadgeType` load
- [ ] 3.3 Rewrite `DeleteBadgeTypeHandler`: load `BadgeEvent` (tracked), call `badgeEvent.DeleteBadgeType(...)`, cascade instance deletion if standalone
- [ ] 3.4 Rewrite `AddBadgeInstanceHandler`: load `BadgeEvent` (untracked for guard), call `badgeEvent.EnsureCanManageInstances(badgeTypeId)`, then create and add `BadgeInstance`
- [ ] 3.5 Rewrite `UpdateBadgeInstanceHandler`: load `BadgeEvent` (untracked for guard), call `badgeEvent.EnsureEventActive()`, then load and update `BadgeInstance` with `expectedVersion`
- [ ] 3.6 Rewrite `DeleteBadgeInstanceHandler`: load `BadgeEvent` (untracked for guard), call `badgeEvent.EnsureEventActive()`, then load and remove `BadgeInstance`
- [ ] 3.7 Update `RenameBadgeTypeCommand` and `RenameBadgeTypeHttpRequest`: `ExpectedVersion` now applies to `BadgeEvent` (no change to field name/type, but semantics change)

## 4. Query handlers and response shape

- [ ] 4.1 Create `GetBadgeTypesResponse` DTO: `{ uint EventVersion, IReadOnlyList<BadgeTypeListItemDto> BadgeTypes }` — update `BadgeTypeListItemDto` to remove `Version` field
- [ ] 4.2 Rewrite `GetBadgeTypesHandler`: load `BadgeEvent` (untracked), project `badgeEvent.BadgeTypes` list, query `badge_instances` count for standalone types, return new envelope with `EventVersion = badgeEvent.Version`
- [ ] 4.3 Rewrite `GetBadgeInstancesHandler`: load `BadgeEvent` (untracked), call `badgeEvent.EnsureCanManageInstances` guard (throws if ticket-based), then query `badge_instances`
- [ ] 4.4 Rewrite `ExportBadgeCsvHandler`: load `BadgeEvent` (untracked) to get badge type definition instead of querying `badge_types` table
- [ ] 4.5 Update `GetBadgeTypesHttpEndpoint` return type to `GetBadgeTypesResponse`

## 5. API contract and SDK

- [ ] 5.1 Start Aspire, wait for API, fetch updated OpenAPI spec
- [ ] 5.2 Regenerate Admin UI SDK (`pnpm openapi-ts` in `src/Admitto.UI.Admin`)

## 6. Admin UI

- [ ] 6.1 Update badge-types page: read `response.eventVersion` (instead of `badgeType.version`) and pass as `expectedVersion` in rename request
- [ ] 6.2 Review instances page: verify `expectedVersion` for update/delete still uses `instance.version` (should be unchanged)

## 7. Tests

- [ ] 7.1 Update `BadgesApiFixture`: remove `BadgeTypeVersion` tracking; add `BadgeEventVersion` accessor read from seeded `BadgeEvent`
- [ ] 7.2 Update `RenameBadgeTypeTests`: supply `eventVersion` instead of `badgeTypeVersion` in requests; add scenario for stale event version → 409
- [ ] 7.3 Update `AddBadgeTypeTests` (integration): reflect new domain model setup (no separate `BadgeType` seeding)
- [ ] 7.4 Add/update `BadgeEventAggregateTests` (domain tests): cover `AddBadgeType`, `RenameBadgeType`, `DeleteBadgeType`, `EnsureCanManageInstances` — all invariants
- [ ] 7.5 Verify all existing badge-type and badge-instance end-to-end tests still pass after handler rewrites
- [ ] 7.6 Run architecture tests (`dotnet test --project tests/Admitto.Core.ArchTests/...`)
