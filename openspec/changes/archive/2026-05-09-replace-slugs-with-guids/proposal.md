## Why

Slugs add operational complexity — uniqueness constraints, dedicated value objects, slug→ID resolution on every request, and a mandatory human-chosen identifier at creation time — without providing meaningful value in an admin tool where IDs are already available. Removing them simplifies the domain model, eliminates ambiguity in routing, and removes a class of "duplicate slug" errors that currently surface asynchronously.

## What Changes

- **BREAKING** All admin API routes replace `{teamSlug}` with `{teamId}` (UUID).
- **BREAKING** All admin API routes replace `{eventSlug}` with `{eventId}` (UUID).
- **BREAKING** The public attendee endpoint `GET /events/{teamSlug}/{eventSlug}/ticket-types` switches to `GET /events/{teamId}/{eventId}/ticket-types`.
- `Team` aggregate no longer carries a `Slug` value object; the `name` field is sufficient for display.
- `TicketedEvent` aggregate no longer carries a `Slug` field; creation no longer requires a slug.
- The Organization module's slug→ID resolution facade methods (`GetTeamIdAsync`, etc.) are removed; callers receive and pass IDs directly.
- `ApiKeyTeamScopeFilter` and `TeamMembershipAuthorizationHandler` are updated to resolve team scope by `{teamId}` from the route directly.
- All integration/domain events that currently carry `TeamSlug` or `EventSlug` are updated to carry only IDs (or the field is dropped where it was only needed for routing).
- Admin UI dynamic routes rename `[teamSlug]` segments to `[teamId]` and `[eventSlug]` segments to `[eventId]`.
- **Out of scope**: Ticket type slugs and time-slot slugs. These are domain identifiers meaningful *within* a catalog (unique within an event, used in self-service and QR flows) and are not routing slugs — they remain unchanged.

## Capabilities

### New Capabilities
<!-- None — this is a simplification, not an extension. -->

### Modified Capabilities
- `team-management`: Remove slug from create-team request; remove "view team by slug" requirement; route param changes to `{teamId}`.
- `event-management`: Remove slug from create-event request; remove slug-uniqueness enforcement; route param changes to `{eventId}`.
- `admin-ui-team-crud`: All routes and API proxy calls updated from `[teamSlug]` to `[teamId]`; slug field removed from create/settings forms.
- `admin-ui-event-management`: All routes and API proxy calls updated from `[eventSlug]` to `[eventId]`; slug field removed from create/general-settings forms.
- `ticket-type-management`: Public ticket-type lookup endpoint changes from `/events/{teamSlug}/{eventSlug}/ticket-types` to `/events/{teamId}/{eventId}/ticket-types`.

## Impact

- **API (all modules)**: Every endpoint under `/admin/teams/{teamSlug}/...` and `/admin/teams/{teamSlug}/events/{eventSlug}/...` must be re-mapped.
- **Domain — Organization**: `Team` entity loses `Slug` property and its associated uniqueness index; `OrganizationFacade` loses slug-resolution methods.
- **Domain — Registrations**: `TicketedEvent` entity loses `Slug` property; the unique index on `(TeamId, Slug)` is dropped; `TicketedEventCreationRequested` integration event no longer carries `EventSlug`.
- **Auth / Middleware**: `ApiKeyTeamScopeFilter` and `TeamMembershipAuthorizationHandler` switch from slug-based resolution to ID-based lookup.
- **Admin UI**: All Next.js dynamic-route segments, proxy routes, and sidebar navigation links updated.
- **EF Core**: Migrations required to drop slug columns and uniqueness indexes on both `Teams` and `TicketedEvents` tables.
- **Existing API consumers / API keys**: Any external integrations using current slug-based URLs will break; this is an intentional breaking change.
