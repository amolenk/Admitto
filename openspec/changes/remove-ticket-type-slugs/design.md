## Context

`TicketType` entities are currently keyed by a user-supplied slug (e.g. `"vip"`, `"early-bird"`). This was practical when a CLI was the primary interface — slugs were easy to type. With the Admin UI in place, users never type identifiers directly; they interact with display names. The slug is now just friction: a format-constrained, immutable identifier that users must also invent.

Email templates went through the same transition recently: they were previously identified by a `type` slug and were migrated to a `Name` display string + GUID `EmailTemplateId`. This change applies the same pattern to `TicketType`.

The `TimeSlot` VO is collateral: it is currently `sealed record TimeSlot(Slug)` — a thin wrapper that inherits the slug format constraint unnecessarily. Time slot identifiers such as `"Saturday Morning"` or `"AM Session"` carry no semantic meaning beyond being a shared label used to detect scheduling conflicts; there is no reason to constrain their format to lowercase-hyphenated strings.

## Goals / Non-Goals

**Goals:**
- Remove the slug field from the `AddTicketType` request; the server assigns a `TicketTypeId` (GUID) on creation.
- Enforce `TicketTypeName` uniqueness within a `TicketCatalog` (case-insensitive) as the sole uniqueness guarantee.
- Replace `sealed record TimeSlot(Slug)` with a proper Vogen `[ValueObject<string>]` struct with its own validation (non-empty, max length 64), removing slug format constraints from time slot identifiers.
- Update all referencing layers (commands, handlers, endpoints, contracts, Email module, integration events) to use `TicketTypeId` where slug strings were used.

**Non-Goals:**
- Data migration (project is pre-production/greenfield — no live data).
- Renaming the `TimeSlot` type itself (keep the name, change its implementation).
- Changing `TicketType` mutability rules beyond what the new identity model requires.
- Changing how events, teams, or other aggregates are identified.

## Decisions

### D1 — Use the existing `TicketTypeId` Vogen struct

`TicketTypeId` already exists as `[ValueObject<Guid>]` in the Registrations module. No new VO is needed; `TicketType` simply changes its base from `Entity<string>` to `Entity<TicketTypeId>`, and the ID is generated server-side in the `AddTicketTypeHandler`.

**Alternatives considered:** Reusing `string` with a machine-generated slug prefix (e.g. `tt-{guid}`) — rejected because it provides no benefit over a plain GUID and would still look strange in error messages.

### D2 — Name uniqueness via DB-level case-insensitive unique index

The `TicketCatalog` aggregate enforces a domain-level duplicate-name check at add time (mirroring the current duplicate-slug check). Additionally, a DB-level functional unique index on `(ticketed_event_id, lower(name))` is added to the `ticket_types` table as the authoritative constraint. This mirrors the pattern already used for `EmailTemplate` (`IX_email_templates_scope_scope_id_name`).

**Alternatives considered:** Application-only uniqueness check — rejected because concurrent inserts could bypass it. Composite unique constraint on `(ticketed_event_id, name)` without `lower()` — rejected because it would allow "VIP" and "vip" as separate names.

### D3 — `TimeSlot` becomes a Vogen `[ValueObject<string>]`

The `sealed record TimeSlot(Slug)` is replaced by a proper `[ValueObject<string>]` partial struct following the same pattern as `TicketTypeName`. Validation: non-empty, max length 64. No format constraint.

`TicketType.TimeSlotSlugs: Slug[]` (stored column) and the computed `TimeSlots` property are collapsed into a single `TimeSlots: TimeSlot[]` that EF stores as a primitive collection of strings.

`TicketTypeSnapshot` similarly updates its `Slug[] TimeSlots` to `TimeSlot[]`.

### D4 — `TicketTypeSnapshot` carries `TicketTypeId`

The snapshot stored inside each `Registration` changes from `(Slug, TicketTypeName, Slug[])` to `(TicketTypeId, TicketTypeName, TimeSlot[])`. Carrying the ID preserves the ability to correlate a registration's historical snapshot to the current catalog entry (used by the `GetRegistrations` filter and the `RegistrationsFacade`).

The `Name` is still stored in the snapshot for historical display — this is unchanged.

### D5 — API routes and contracts use `{id:guid}` / `TicketTypeId`

All endpoints that previously used a slug path parameter (`/{slug}`) or a slug in the request body switch to `/{id:guid}` / GUID in the request body. This applies to:
- `UpdateTicketType` and `CancelTicketType` routes
- `ChangeAttendeeTickets` request body (`ticketTypeIds`)
- `GetRegistrations` query filter (`ticketTypeIds`)
- `CreateCoupon` request body (`allowedTicketTypeIds`)
- BulkEmail source filter (`ticketTypeIds`)

The `AddTicketType` response returns the newly assigned `id` (GUID) instead of the slug.

## Risks / Trade-offs

- **Error message details change** — Error detail dictionaries previously used `["slug"]` keys; these will become `["id"]` or `["name"]`. This is an API contract change on error payloads (low risk given pre-production status).
- **Integration event contract change** — `TicketTypeItem(slug, name)` in `AttendeeRegisteredIntegrationEvent` and `AttendeeTicketsChangedIntegrationEvent` becomes `TicketTypeItem(id, name)`. Any consumers of these events must update their deserialization.

## Migration Plan

Project is pre-production (greenfield). No data migration required.

A single new EF Core migration will:
1. Drop the `ticket_types` PK on the slug column; add `id` (UUID) as the new PK.
2. Drop the old string `id` column; rename or recreate columns as needed.
3. Add a functional unique index on `(ticketed_event_id, lower(name))`.
4. Update `registrations` ticket snapshot column (JSON) structure to use `id` field instead of `slug`.
5. Update `coupons.allowed_ticket_type_ids` column (previously `allowed_ticket_type_slugs`).
