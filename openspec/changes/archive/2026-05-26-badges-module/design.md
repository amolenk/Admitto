## Context

No badge capability currently exists in Admitto. Organizers producing printed name badges must export attendee data manually from separate tools and assemble the badge list by hand. The Badges module introduces structured badge-type configuration per event, standalone badge-instance management, and CSV export — all within the established `Admitto.Core` module architecture.

The module touches the Registrations module's read surface (to fetch registration names and additional details for export) and consumes Registrations integration events (to track event lifecycle). No changes are required to Organization or Email modules.

## Goals / Non-Goals

**Goals:**
- Introduce `Admitto.Core.Module.Badges` as a first-class module, following the existing Domain/Application/Infrastructure/Contracts folder structure and all crosscutting conventions.
- Allow organizers to define badge types per event: either *ticket-based* (one badge per unique registrant across one or more linked ticket types, deduplicated by registration ID) or *standalone* (a manually managed list of instances).
- Allow organizers to manage instances of standalone badge types (add, update, delete).
- Provide on-demand CSV export per badge type, pulling first/last name, email, ticket type, and additional-detail values for ticket-based types, and organizer-supplied fields for standalone types.
- Consume `TicketedEventCreated`, `TicketedEventCancelled`, and `TicketedEventArchived` integration events to gate badge-type mutations against lifecycle.

**Non-Goals:**
- Badge rendering or PDF/print generation — CSV export only.
- QR code generation on badges.
- Public-facing endpoints for badges (admin only in this iteration).
- Real-time sync of badge data (export is always on-demand).
- Per-attendee badge editing for ticket-based types (derived entirely from live Registration data).

## Decisions

### D1 — New module, not an extension of Registrations

**Decision:** `Admitto.Core.Module.Badges` is a standalone module namespace.

**Rationale:** Badge types introduce a new aggregate (`BadgeType`), a new entity (`BadgeInstance`), and a new export concern that has no natural home in Registrations. Registrations owns `Registration` and `TicketCatalog`; mixing badge configuration there would violate single-responsibility and couple two orthogonal concerns. The Badges module reads Registrations data via its public facade, which is the sanctioned cross-module read pattern.

**Alternative considered:** Extend `TicketCatalog` with badge type metadata. Rejected: `TicketCatalog` is capacity-management only; adding badge concern there would violate its responsibility, and standalone badge types have no `TicketTypeId` anchor at all.

---

### D2 — BadgeType aggregate tracks event lifecycle with a projected BadgesEvent entity

**Decision:** The Badges module maintains a lightweight `BadgesEvent` entity (columns: `EventId`, `Status: Active|Archived`) created in response to `TicketedEventCreated` and transitioned by `TicketedEventCancelled` / `TicketedEventArchived`. Badge-type mutation handlers load this entity to gate writes — analogous to how `TicketCatalog` projects `EventStatus`.

**Rationale:** Badge-type mutations must be guarded against archived/cancelled events. Rather than hitting the Registrations DbContext (forbidden by architecture rules), the Badges module maintains a minimal projection of event lifecycle. This is the established pattern (see `TicketCatalog.EventStatus`).

**Alternative considered:** Call `IRegistrationsFacade` on every mutation to check event status. Rejected: the facade is read-only and must remain side-effect-free, but more importantly this adds a synchronous cross-module query to every write path, whereas the projection is local and fast.

---

### D3 — BadgeType and BadgeInstance are separate aggregates

**Decision:** `BadgeType` is the aggregate root (one per badge definition per event). `BadgeInstance` is a separate small aggregate owned by `BadgeType.Id` (not a child entity) to keep `BadgeType` aggregate size bounded. A ticket-based `BadgeType` holds an `IReadOnlyList<TicketTypeId>` (at least one entry required) rather than a single nullable `TicketTypeId`.

**Rationale:** Standalone badge types can accumulate many instances (e.g., hundreds of guests). Modeling instances as a collection on the aggregate root would cause large EF load payloads on every mutation. A separate `BadgeInstance` aggregate with an `BadgeTypeId` foreign key allows loading only what is needed.

---

### D4 — CSV export is computed on demand via IRegistrationsFacade

**Decision:** The badge export handler builds the CSV in memory on request. For ticket-based badge types it fetches registrations via `IRegistrationsFacade` filtered by the badge type's list of `TicketTypeId`s, **deduplicates by `RegistrationId`** (a registrant who holds multiple of the linked ticket types appears exactly once), then streams the rows. No caching or materialised view is maintained.

**Rationale:** Events typically have tens to low hundreds of registrations. On-demand generation avoids the complexity of maintaining a materialised export store. Deduplication by `RegistrationId` is the correct key because a `Registration` is person-scoped — one person, one row, regardless of how many of the badge's ticket types they hold. If export latency becomes a problem at scale, a cached export file can be added in a future iteration without changing the public contract.

**IRegistrationsFacade extension:** The existing `QueryRegistrationsAsync` method returns `RegistrationQueryResult` items with `FirstName`, `LastName`, `Status`, and `TicketTypeIds`. The export additionally needs `Email` and `AdditionalDetails` values. `IRegistrationsFacade` will be extended with a new method `QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds)` (note: `ticketTypeIds` is `IReadOnlyList<TicketTypeId>`) returning a deduplicated collection of `BadgeExportRegistrationDto` items containing those fields. Deduplication is performed inside the Registrations implementation (one row per unique `RegistrationId` that has at least one ticket of the requested types). The facade lives in `Admitto.Core.Module.Registrations.Contracts`, so the Badges module can depend on it without crossing module DbContexts.

---

### D5 — No integration events published, no outbox required

**Decision:** The Badges module consumes integration events but does not publish any in this iteration. The `BadgesDbContext` does NOT implement `IOutboxDbContext` and no `outbox_messages` table is created for this module.

**Rationale:** The `DomainEventsInterceptor` dispatches domain events synchronously to `IDomainEventHandler<T>` implementations; it has no dependency on `IOutboxDbContext`. The `IOutbox` service is only registered for a module when its DbContext implements `IOutboxDbContext` — skipping that implementation is safe when no commands or integration events need to be enqueued for async delivery. Since no other module reacts to badge changes, the outbox adds complexity with no benefit in this iteration. If the module needs to publish integration events in the future, `IOutboxDbContext` and the migration can be added then.

---

### D6 — Standalone badge instance fields are name + optional free-text notes only

**Decision:** Each `BadgeInstance` carries a `DisplayName` (required, max 200 chars) and a `Notes` field (optional, max 500 chars). There are no arbitrary key/value fields in this iteration.

**Rationale:** Arbitrary key/value per-instance fields add schema complexity (JSON column, dynamic CSV headers) with unclear use cases. The common scenario is recording a name for printing. Notes cover the "spouse of speaker" annotation. Structured key/value fields can be added in a follow-up if needed.

## Risks / Trade-offs

- **IRegistrationsFacade extension couples Badges to Registrations contracts.** This is by design (cross-module read via facade is the sanctioned pattern), but any change to `RegistrationQueryResult` or the new `BadgeExportRegistrationDto` must be coordinated. → Mitigation: keep the DTO minimal and versioned; prefer adding optional fields over breaking changes.

- **Export latency for large events.** On-demand CSV generation is O(n) in registration count. For events with thousands of registrations this could be slow. → Mitigation: streaming response (no full in-memory buffering); future caching if needed.

- **BadgesEvent projection can lag behind Registrations lifecycle.** Outbox delivery is async; a badge-type mutation could theoretically succeed between a `TicketedEventArchived` being emitted and the Badges module projecting it. → Mitigation: this is an acceptable window given the use-case (badge management, not financial transactions); same trade-off accepted for `TicketCatalog`.

## Migration Plan

1. Create `Admitto.Core.Module.Badges` namespace with Domain/Application/Infrastructure layers.
2. Add EF Core migration for `badge_events`, `badge_types`, and `badge_instances` tables (badges schema). No `outbox_messages` table is needed.
3. Register the new module in the API host's `Program.cs` and `AdminEndpoints.cs`.
4. Consume existing integration events (`TicketedEventCreated`, `TicketedEventCancelled`, `TicketedEventArchived`) — add handlers in the Badges module.
5. Extend `IRegistrationsFacade` and its implementation in Registrations with `QueryRegistrationsForBadgeExportAsync`.
6. Implement badge-type management endpoints, standalone-instance endpoints, and export endpoint.
7. Update Admin UI SDK (regenerate from updated OpenAPI spec) and add management + export pages.
8. Update ArchTests to include the Badges module namespace in allowed module list.

**Rollback:** Remove the migration and the Badges module registration. No existing data is touched.

## Open Questions

- Should the CSV export filename be `<badge-type-name>-<event-slug>-badges.csv` or something else? (UX decision, does not affect implementation structure.)
- Should deleting a badge type with existing ticket-based registrations be permitted, or should it be blocked/soft-deleted? Current proposal: allowed (badge types are config, not historical records).
- Should `BadgesEvent` be transitioned on both `TicketedEventCancelled` and `TicketedEventArchived`, or only `TicketedEventArchived`? (Current proposal: both, matching `TicketCatalog` behaviour — badge management is disabled for cancelled events too.)
