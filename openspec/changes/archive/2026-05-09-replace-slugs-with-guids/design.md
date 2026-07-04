## Context

Admitto currently routes all team and event resources via human-readable slug identifiers (e.g. `/admin/teams/{teamSlug}/events/{eventSlug}/...`). Slugs are also embedded in the domain model as first-class value objects and must be globally unique (teams) or unique within a team (events). This requires dedicated slug→ID resolution on every authenticated request (middleware, auth handlers, API key scope filter), a `Slug` domain value object, uniqueness indexes, and related error handling.

In an admin tool the audience is always authenticated and already holds team/event IDs from list responses. GUIDs are unambiguous, stable, and need no resolution layer.

The current state:
- `Team.Slug` — globally unique, immutable, used in all team routes
- `TicketedEvent.Slug` — unique within team, used in all event routes
- `OrganizationFacade.GetTeamIdAsync(slug)` — called by auth middleware and scope filter
- Admin UI dynamic routes: `[teamSlug]`, `[eventSlug]`
- Two unique database indexes: `Teams.Slug` and `(TeamId, Slug)` on `TicketedEvents`

**Out of scope:** Ticket-type slugs and time-slot slugs. These are domain identifiers within a catalog (unique within an event, meaningful in attendee-facing QR and self-service flows) and serve a different purpose from routing params.

## Goals / Non-Goals

**Goals:**
- Replace `{teamSlug}` with `{teamId}` in all admin API routes
- Replace `{eventSlug}` with `{eventId}` in all admin API routes
- Replace `{teamSlug}/{eventSlug}` with `{teamId}/{eventId}` in the public ticket-type endpoint
- Remove `Slug` from the `Team` aggregate and its creation DTO
- Remove `Slug` from the `TicketedEvent` aggregate and its creation DTO
- Remove slug→ID resolution from auth middleware and API key scope filter
- Remove unique slug indexes from the database (via EF Core migration)
- Update Admin UI routes and proxy calls accordingly
- Update existing specs to reflect new routing and removed slug requirement

**Non-Goals:**
- Changing ticket-type slugs or time-slot slugs (see above)
- Altering any other identifying or naming property of teams/events
- Changing the async two-phase event creation flow itself
- Versioning the API or maintaining backward compatibility with slug-based URLs

## Decisions

### D1 — Use the existing GUID as the route parameter

Teams and events already have a stable UUID primary key (`TeamId`, `EventId`). Reusing it as the sole URL segment keeps routing trivial and eliminates any lookup.

*Alternative considered:* Introduce a short opaque ID (e.g. NanoID). Rejected: adds complexity and a new uniqueness concern with no benefit over the existing UUID.

### D2 — Remove `Slug` from the domain model entirely

The `Slug` value object exists only to enforce route-level uniqueness. Once routes use IDs, retaining it would be dead weight (a validated field nobody reads). Removing it keeps the domain clean.

*Alternative considered:* Keep slug as an optional display hint. Rejected: optional fields with uniqueness constraints are harder to maintain than absent ones.

### D3 — Do not version the API

This is an intentional breaking change. The system is pre-1.0 and there are no documented external consumers beyond the Admin UI (which is owned in this repo). Maintaining a v1 slug-based surface alongside v2 ID-based routes would create perpetual duplication.

### D4 — Update all specs in a single delta, not incrementally

Because `{teamSlug}` and `{eventSlug}` appear in virtually every spec, a single cohesive change reduces the risk of leaving stale references.

## Risks / Trade-offs

- **[Risk] External consumers / API keys using slug URLs will break** → Mitigation: documented as intentional in the proposal; API key teams are internal beta testers at this stage.
- **[Risk] Existing integration tests hard-code slug-based paths** → Mitigation: tests will fail fast and clearly (404/binding failure); update as part of the same PR.
- **[Risk] EF Core migration may be tricky on production data** → Mitigation: dropping a column with a unique index is safe when no data needs to be preserved in that column; rollback = re-add column + index.
- **[Trade-off] URLs become less human-readable** → Acceptable: the Admin UI always navigates programmatically; direct URL manipulation is not a supported workflow.

## Migration Plan

1. Apply EF Core migrations: drop `Slug` column and unique index from `Teams`; drop `Slug` column and `(TeamId, Slug)` unique index from `TicketedEvents`.
2. Update backend endpoints: all admin and public routes swap slug path params for ID params.
3. Update Auth middleware and API key scope filter: resolve team by `{teamId}` directly (no facade lookup).
4. Update Admin UI: rename `[teamSlug]` → `[teamId]`, `[eventSlug]` → `[eventId]` in route segments and proxy calls; remove slug fields from create/settings forms.
5. Regenerate Admin UI SDK from updated OpenAPI spec.

Rollback: revert migrations + code in a single step; no data loss because slug removal does not destroy any other columns.

## Open Questions

- Should the `name` field on `Team` enforce a uniqueness constraint (to help admins distinguish teams at a glance) now that the slug no longer serves this purpose? Recommendation: no — names are for display, not identity; a uniqueness constraint would create a new class of errors without a clear benefit.
