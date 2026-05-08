## 1. Domain — Organization Module

- [x] 1.1 Remove `Slug` property and related logic from the `Team` aggregate
- [x] 1.2 Remove `Slug` parameter from `Team.Create(...)` factory method and from `CreateTeamCommand` / `CreateTeamRequest`
- [x] 1.3 Remove the `TeamSlug` value object (or keep if used elsewhere; otherwise delete)
- [x] 1.4 Remove slug uniqueness check from the `CreateTeamHandler` (duplicate-slug guard)
- [x] 1.5 Update `GetTeamIdAsync` on `OrganizationFacade` — remove method or replace with ID-based lookup; update all callers (`TeamMembershipAuthorizationHandler`, `ApiKeyTeamScopeFilter`)
- [x] 1.6 Remove `Slug` from `TicketedEventCreationRequested` integration event payload (if present)

## 2. Domain — Registrations Module

- [x] 2.1 Remove `Slug` property and related validation from the `TicketedEvent` aggregate
- [x] 2.2 Remove `slug` parameter from `TicketedEvent.Create(...)` and from `CreateTicketedEventCommand`
- [x] 2.3 Remove the slug-uniqueness check in the `CreateTicketedEventHandler` (no more duplicate-slug guard)
- [x] 2.4 Update any domain/integration events that carry `EventSlug` to use `EventId` or remove the field

## 3. Persistence — EF Core Migrations

- [x] 3.1 Add migration to drop the `Slug` column and its unique index from the `Teams` table (Organization DbContext)
- [x] 3.2 Add migration to drop the `Slug` column and the `(TeamId, Slug)` unique index from the `TicketedEvents` table (Registrations DbContext)
- [x] 3.3 Remove `Slug` property configuration from both entity type configurations

## 4. API — Route Updates

- [x] 4.1 Rename all admin route templates from `{teamSlug}` → `{teamId}` under `AdminEndpoints`
- [x] 4.2 Rename all admin route templates from `{eventSlug}` → `{eventId}` under `AdminEndpoints`
- [x] 4.3 Update the public endpoint `GET /events/{teamSlug}/{eventSlug}/ticket-types` → `GET /events/{teamId}/{eventId}/ticket-types`
- [x] 4.4 Update `ApiKeyTeamScopeFilter` to read `{teamId}` from the route and resolve team scope by ID (removing slug-based `GetTeamIdAsync` call)
- [x] 4.5 Update `TeamMembershipAuthorizationHandler` to resolve team scope by ID from the route (removing slug-based resolution)
- [x] 4.6 Update all command/query endpoint bindings that previously destructured `teamSlug` / `eventSlug` to bind `teamId` / `eventId` instead
- [x] 4.7 Remove `GetTeamIdAsync(slug)` (and equivalent for events) from `IOrganizationFacade` and its implementation

## 5. API — Response DTOs

- [x] 5.1 Remove `Slug` field from `TeamListItemDto` / `TeamDetailsDto` (if present)
- [x] 5.2 Remove `Slug` field from `TicketedEventListItemDto` / `TicketedEventDetailsDto` (if present)
- [x] 5.3 Ensure `TeamId` and `EventId` are always present in list and detail response DTOs so callers can construct routes

## 6. Admin UI — Route Renaming

- [ ] 6.1 Rename all Next.js dynamic route directories: `[teamSlug]` → `[teamId]`
- [ ] 6.2 Rename all Next.js dynamic route directories: `[eventSlug]` → `[eventId]`
- [ ] 6.3 Update all intra-app links and `router.push(...)` calls that reference team/event routes to use IDs

## 7. Admin UI — Proxy Routes & SDK

- [ ] 7.1 Update all proxy route files under `app/api/teams/[teamId]/...` to pass `teamId` (not `teamSlug`) to backend calls
- [ ] 7.2 Update all proxy route files under `app/api/teams/[teamId]/events/[eventId]/...` to pass `eventId`
- [ ] 7.3 Regenerate the Admin UI OpenAPI SDK after backend route changes are applied (`pnpm openapi-ts`)

## 8. Admin UI — Forms

- [ ] 8.1 Remove the slug input field from the "Create Team" form
- [ ] 8.2 Remove the slug read-only display from the "Team Settings" page
- [ ] 8.3 Remove the slug input field from the "Create Event" form
- [ ] 8.4 Remove the slug read-only display from the event General-tab settings form
- [ ] 8.5 Update post-creation redirect to use the team/event ID returned by the backend

## 9. Tests

- [ ] 9.1 Update all end-to-end / integration tests that construct slug-based API paths to use ID-based paths
- [ ] 9.2 Update test fixtures and builders that create teams/events with slugs
- [ ] 9.3 Add or update acceptance scenario tests to cover the updated SC-* scenarios in the modified specs
- [ ] 9.4 Verify no remaining compilation errors or runtime 404s from stale slug references

## 10. Documentation

- [ ] 10.1 Update `docs/arc42/` sections that reference slug-based routing or the Slug value object
- [ ] 10.2 Sync updated delta specs to main specs via `openspec sync` (or archive the change)
