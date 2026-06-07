## Context

The Badges module currently has three separate aggregates: `BadgeEvent` (entity, lifecycle gate), `BadgeType` (aggregate, configuration), and `BadgeInstance` (aggregate, manual instances). Every mutation handler loads `BadgeEvent` to call `EnsureEventActive()` before loading and mutating the actual target — a two-load pattern repeated across six handlers. Badge types are a small, event-scoped set (expected: a handful per event); instances can grow to hundreds.

## Goals / Non-Goals

**Goals:**
- Make `BadgeEvent` the aggregate root for badge-type configuration so the active-event invariant is enforced inside the aggregate, not by each handler independently.
- Remove `BadgeType` as a separate EF aggregate; store badge types as a JSON column on `badges_events`.
- Replace per-badge-type optimistic concurrency with event-level OCC (`BadgeEvent.Version`).
- Keep `BadgeInstance` as an independent aggregate with its own row-version.
- Preserve all existing functional behavior (mutation guards, name uniqueness, instance count in list, export).

**Non-Goals:**
- Changing badge instance storage, pagination, or query shape.
- Changing the export CSV format or the registrations-facade integration.
- Introducing CQRS read models / projections; queries read directly from the write model.
- Changing how `BadgeEvent` is created or archived (integration event handlers stay as-is).

## Decisions

### D1: Badge types stored as `jsonb` column on `badges_events`

Badge types are small (a handful per event) and only ever loaded as a set in the context of their owning event. There is no use case that reads badge types independently of the event. Storing them as `jsonb` removes an unnecessary table join, makes the aggregate boundary explicit in the database, and eliminates the need to track `BadgeType` as a separate EF entity.

Alternative considered: keep `badge_types` table but model it as an EF-owned collection. This would preserve relational structure but require EF Owned Entity configuration, shadow keys, and a join in every load — more complexity for no benefit given the small set size.

Alternative considered: keep `BadgeType` as a separate aggregate with a foreign key. This is the current model and forces the repeated `BadgeEvent` + `BadgeType` two-load pattern everywhere.

### D2: `BadgeEvent` promoted to `Aggregate<TicketedEventId>`

`BadgeEvent` currently extends `Entity`, which has no `Version`, no audit fields, and no domain events. Promoting it to `Aggregate` gives it `Version` (xmin), `CreatedAt`/`LastChangedAt`/`LastChangedBy` audit fields, and the domain event infrastructure — all of which are needed for the OCC contract and future extensibility.

`BadgeType` becomes a plain domain class (not extending `Entity` or `Aggregate`), owned by `BadgeEvent` via an in-memory list and serialised to `jsonb`.

### D3: Event-level OCC for badge-type mutations

Because badge types live inside the `BadgeEvent` document, any mutation bumps `BadgeEvent.Version`. Rename and delete badge-type endpoints accept `expectedVersion` against the event version. This is consistent with how other aggregates in the system (e.g. `TicketedEvent`, `Team`) expose their version to the client.

The UI currently reads `badgeType.version` from the list response for rename. After this change it reads `eventVersion` from the list response envelope. Instance mutations (`UpdateBadgeInstance`, `DeleteBadgeInstance`) continue to use `BadgeInstance.Version` unchanged.

Alternative considered: keep per-badge-type versions alongside the JSON column (e.g. a parallel jsonb array). This adds complexity and defeats the simplification goal.

### D4: `GetBadgeTypes` response wrapped with `eventVersion`

The list endpoint returns a new envelope `{ eventVersion, badgeTypes: [...] }` instead of a bare array. This is the natural place to expose the event version to the client without a separate round-trip. The per-item `version` field is removed.

### D5: Instance mutation handlers load `BadgeEvent` as aggregate root

Handlers for add/update/delete badge instance still load `BadgeEvent` (now as aggregate) to call `EnsureEventActive()` and (for add) to validate `kind == Standalone`. This is correct because `BadgeEvent` is the authority for those rules. `BadgeInstance` remains the target for the actual mutation.

### D6: Queries read directly from write model

`GetBadgeTypes` loads `BadgeEvent` and projects its `BadgeTypes` list. `GetBadgeInstances` and `ExportBadgeCsv` continue to query `badge_instances` directly (unchanged). No read-side projection or caching is introduced.

## Risks / Trade-offs

- **JSON loses DB-level referential integrity for `badge_instances.badge_type_id`** → Mitigated by enforcing the constraint in the aggregate: `DeleteBadgeType` cascades instance deletion within the same unit of work; the handler already does this manually. No orphaned instances can be created because adds go through the aggregate.

- **Concurrent badge-type mutations produce false conflicts** → Accepted trade-off. Badge-type configuration (add/rename/delete) is an infrequent, organiser-driven operation. Multiple organisers editing different badge types simultaneously is an uncommon scenario, and the small number of badge types per event means conflicts are unlikely in practice.

- **Admin UI breaking change** → The SDK must be regenerated after the API shape change. The UI rename dialog changes from `badgeType.version` to `eventVersion`. This is a contained, well-understood change in one component.

- **EF migration drops the `badge_types` table** → The migration is destructive. Any existing `badge_types` rows must be migrated into the `badges_events.badge_types` jsonb column before the table is dropped. The migration script reads existing rows and serialises them into the parent event's JSON column in a single transaction.

## Migration Plan

1. Write an EF migration that:
   a. Adds `badge_types jsonb NOT NULL DEFAULT '[]'` column to `badges_events`.
   b. Copies all existing `badge_types` rows into their parent event's new column as JSON.
   c. Drops the `badge_types` table.
2. Update `BadgesDbContext` model: `BadgeEvent` configured with `OwnsMany` / `ToJson`, remove `BadgeType` `DbSet`.
3. Update `IBadgesWriteStore`: remove `BadgeTypes` property.
4. Promote `BadgeEvent` to `Aggregate`, add `BadgeTypes` list with mutation methods.
5. Update all six mutation handlers and two query handlers.
6. Update API response shape and regenerate SDK.
7. Update Admin UI rename dialog to use `eventVersion`.
8. Update tests.

Rollback: restore the migration (EF supports `dotnet ef migrations remove` pre-deploy). No data loss if rolled back before the `badge_types` table drop step in production.

## Open Questions

*(none — all decisions made during exploration)*
