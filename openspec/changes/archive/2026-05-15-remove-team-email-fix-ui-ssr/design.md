rfdfrf## Context

The `Team` aggregate currently holds an `EmailAddress` value object that was originally envisioned as the "from" address for outgoing emails. That role is now served by the `SmtpSettings` stored per team. The field is never read for email dispatch and is displayed only in the Team Settings form, where it confuses admins. Removing it simplifies the domain model and eliminates a misleading UI field.

Separately, the Admin UI renders team names and event names client-side via the `useTeams()` React Query hook and a Zustand store. On first load or hard refresh the store is empty; affected layouts fall back to showing the raw UUID from the URL (`params.teamId`, `params.eventId`) until the async fetch resolves. Converting the layout shell components from `"use client"` to Next.js Server Components means the team/event name is fetched during SSR and is always present in the initial HTML — zero flash.

## Goals / Non-Goals

**Goals:**
- Remove `EmailAddress` end-to-end: domain entity, commands, DTOs, validators, EF migration, API contract, Admin UI forms.
- Eliminate the GUID flash in the team-settings and event-settings layout breadcrumbs/headings by fetching names server-side.
- Keep existing tests green; update specs and tests that reference team email.

**Non-Goals:**
- Changing any email-sending behaviour (SMTP settings, from-address logic, templates).
- Converting _all_ pages to SSR — only the layout shell components (breadcrumbs, headings, sidebar nav) that currently exhibit the GUID flash.
- Removing the `useTeams()` hook or Zustand team store from the rest of the app.

## Decisions

### Decision 1: Remove `EmailAddress` from the `Team` aggregate
**Rationale:** The field is functionally dead — no code reads it for email sending. Keeping it in the domain model means it participates in commands, validators, migrations, and the API contract for no benefit. Removing it is cleaner than deprecating.  
**Alternative considered:** Mark it as deprecated and stop showing it in the UI. Rejected because it still pollutes the domain model, the database, and the API response shape.

### Decision 2: Convert layout shell components to Next.js Server Components
**Rationale:** `layout.tsx` files in Next.js are Server Components by default. The team-settings and event-settings layouts are currently marked `"use client"` solely to access `useParams()` and `useTeams()`. By switching to async Server Components, we can receive `params` as a prop (Next.js passes them automatically) and fetch team/event data directly using the session's access token, eliminating the client-side loading state. The nav links that need the active-state highlight remain interactive via a thin client `NavLinks` child component that uses `usePathname()`.  
**Alternative considered:** Keep client layouts but hydrate from a server-side cookie or a `<Suspense>` boundary. Rejected as more complex than simply fetching in the server layout.

### Decision 3: Team/event fetch in layout goes through the same internal API routes
**Rationale:** The Admin UI already has `/api/teams` and `/api/teams/[teamId]/events/[eventId]` proxy routes. Server Components can call these routes using the `internalApiClient` helper (or equivalent fetch with the session token forwarded from `headers()`). This avoids duplicating the API client logic.  
**Alternative considered:** Call the backend API directly from the Server Component. Possible but leaks the backend URL into Next.js server code; the proxy layer is already there.

### Decision 4: EF Core migration to drop the `Email` column
**Rationale:** The column must be removed cleanly. A migration is generated via `dotnet ef migrations add`. No data migration is needed (the column is unused).

## Risks / Trade-offs

- **API breaking change** → any external consumer calling `POST /teams` or `PATCH /teams/{id}` with an `email` field will have it silently ignored (or rejected if we add strict deserialization). This is acceptable — the API is internal / admin-only today.
- **Layout SSR adds a network call per page render** → team details and event details are small payloads; caching headers on the proxy routes mitigate this. The UX improvement justifies the trade-off.
- **Active nav-link highlight requires `usePathname()`** → the nav list must remain a Client Component child. The layout wrapper itself becomes a Server Component; only the `<NavLinks>` child is `"use client"`. This is the standard Next.js pattern.

## Migration Plan

1. Backend: remove `EmailAddress` from domain, commands, DTOs, add EF migration.
2. API layer: remove email fields from request/response shapes and validators.
3. Regenerate Admin UI OpenAPI SDK (`pnpm openapi-ts`).
4. Admin UI: remove email fields from Create Team and Team Settings forms.
5. Admin UI: convert team-settings and event-settings layouts to Server Components.
6. Update specs (delta) and tests.
7. Deploy: migration runs on startup; no rollback needed (column drop with no data dependency).

## Open Questions

- None. All decisions are clear.
