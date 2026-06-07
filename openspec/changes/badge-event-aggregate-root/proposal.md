## Why

Every badge-type mutation handler (add, rename, delete) and every badge-instance mutation handler (add, update, delete) independently loads `BadgeEvent` and calls `EnsureEventActive()`. That guard is not a `BadgeType` or `BadgeInstance` rule — it is an event-scoped invariant. Making `BadgeEvent` the aggregate root for badge-type configuration centralises the invariant where it belongs and eliminates the repeated two-step load pattern across all six handlers.

## What Changes

- `BadgeEvent` is promoted from a plain `Entity` to an `Aggregate` root. It owns badge types as a JSON column (`badge_types jsonb`) on the `badges_events` table; the separate `badge_types` table and `BadgeType` aggregate are removed.
- `BadgeEvent.Version` (PostgreSQL `xmin`) replaces the per-badge-type optimistic concurrency token. All badge-type mutation endpoints accept `expectedVersion` against the event version, not the badge type version.
- The `GET /badge-types` response **BREAKING** gains a top-level `eventVersion` field and drops the per-item `version` field.
- `BadgeInstance` remains a separate aggregate with its own `xmin` version; instance mutations continue to supply `expectedVersion` per instance.
- Instance mutation handlers still enforce `BadgesEvent.Status == Active` and badge-type kind validation by loading the `BadgeEvent` aggregate (not a separate entity check).
- Export and read queries load badge types from the `BadgeEvent` aggregate directly (no separate table query needed).
- An EF Core migration drops `badge_types`, adds `badge_types jsonb` to `badges_events`, and adds `xmin` row-version tracking to `badges_events`.

## Capabilities

### New Capabilities

*(none)*

### Modified Capabilities

- `badge-type-management`: Badge types are no longer stored in a separate table or loaded as independent aggregates. OCC token is now the event version. The list response shape changes (event version at the top level, no per-type version).
- `standalone-badge-instances`: Instance mutation handlers enforce the active-event and badge-type-kind rules through the `BadgeEvent` aggregate rather than separate entity lookups.

## Impact

- **Backend**: `BadgeEvent`, `BadgeType` domain classes, `BadgesDbContext`, all six badge-type and badge-instance mutation handlers, the `GetBadgeTypes` query handler, the `ExportBadgeCsv` handler, EF entity configurations, and one new migration.
- **API contract**: `GET /badge-types` response shape changes (top-level `eventVersion`, no per-item `version`). **BREAKING** for clients that read `badgeType.version`.
- **Admin UI**: The rename dialog currently sends `expectedVersion: badgeType.version`; it must instead send `expectedVersion: eventVersion` from the list response. The generated SDK will need to be regenerated.
- **Tests**: `BadgesApiFixture`, all badge-type and badge-instance integration/end-to-end tests that assert on or supply a `BadgeType` version need updating.
