## Context

The Admin UI is a Next.js 15 App Router application using shadcn/ui, React Hook Form with Zod validation, and TanStack React Query for data fetching. The backend already exposes `POST /admin/teams` (create) and `PUT /admin/teams/{teamSlug}` (update) endpoints with FluentValidation. The generated HeyAPI OpenAPI client provides typed SDK functions (`createTeam`, `updateTeam`, `getTeam`). Server-side proxy routes in `app/api/` forward requests to the backend via `callAdmittoApi()`.

Currently, the team switcher in the sidebar has an "Add Team" button that does nothing, and there is no team settings page. The add-event form (`teams/[teamSlug]/events/add/`) serves as the canonical pattern for new forms.

## Goals / Non-Goals

**Goals:**
- Allow admins to create a team (slug, name, email) through the Admin UI
- Allow team owners to update a team's details (slug, name, email) with optimistic concurrency
- Follow existing UI patterns (form structure, proxy routes, error handling)
- Display server-side validation errors inline on form fields

**Non-Goals:**
- Team archiving UI (separate feature)
- Team member management UI (separate feature)
- Redesigning the team switcher component beyond wiring the "Add Team" button

## Decisions

### 1. Reuse existing form infrastructure
**Decision:** Use `useCustomForm` hook with Zod + React Hook Form, and `FormError` for server error mapping — matching the add-event pattern.
**Rationale:** Consistent UX and developer experience. The `useCustomForm` hook already handles general errors and per-field server validation mapping via `FormError`.

### 2. Server-side proxy routes for mutations
**Decision:** Add `POST /api/teams` and `PUT /api/teams/[teamSlug]` proxy routes that call the HeyAPI SDK server-side.
**Rationale:** All API calls go through Next.js server routes to handle auth tokens server-side. This is the established pattern (see `GET /api/teams`).

### 3. Nested settings routes at `/teams/[teamSlug]/settings/...`
**Decision:** Use nested routes under `/teams/[teamSlug]/settings/` with a shared settings layout containing a side-nav. The "General" sub-page (`/settings` or `/settings/general`) holds the team details form. Future sub-pages (`/settings/members`, `/settings/danger`) can be added without restructuring. The sidebar shows a single "Settings" entry that links to the general page.
**Rationale:** Each settings concern gets its own focused page. Deep-linkable. Maps naturally to Next.js App Router layout nesting (`settings/layout.tsx`). Extensible for team membership management and archive/danger-zone features later.
**Alternatives considered:** (a) Tabbed single page — harder to deep-link, bigger component. (b) Flat sidebar entries — doesn't scale, no natural home for archive.

### 4. Redirect to new team after creation
**Decision:** After successful team creation, redirect to the new team's events page and update the team switcher.
**Rationale:** Gives immediate feedback and puts the admin in context of their new team.

### 5. Client-side form validation mirrors backend rules
**Decision:** Zod schemas enforce the same constraints as the backend's `Slug`, `DisplayName`, and `EmailAddress` value objects (non-empty, length limits, format).
**Rationale:** Provides instant feedback without a round-trip while the backend remains the authoritative validator.

## Risks / Trade-offs

- **[Slug change on update]** Changing a team's slug updates the URL, which could break bookmarks or shared links. → Accepted: this matches the backend's design; the UI will redirect to the new slug after update.
- **[Optimistic concurrency UX]** The settings form always sends the `expectedVersion` that was loaded with the team data. On a 422 concurrency conflict, the form displays a general error asking the user to reload. Auto-retry or merge UX is out of scope — acceptable for an admin tool with low concurrent usage.
