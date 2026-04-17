## Context

The Admin UI has a team settings area with three tabs defined in a settings layout: General (active), Members (disabled placeholder), and Danger Zone (disabled placeholder). The backend already provides full REST APIs for team membership management (`GET/POST /members`, `PUT/DELETE /members/{email}`) and team archiving (`POST /archive`). The UI simply hasn't been built for these features yet.

Additionally, the backend's `UpdateTeam` command currently accepts an optional `Slug` field and the `Team` entity exposes a `ChangeSlug()` method. The `admin-ui-team-crud` spec includes a scenario for slug updates. However, slugs serve as URL identifiers (e.g., `/teams/{slug}/settings`) and changing them creates broken bookmarks, stale caches, and cross-module reference issues. Making slugs immutable after creation aligns with their role as stable identifiers.

Finally, the team switcher updates `selectedTeamSlug` in the Zustand store but does not navigate the user away from their current page. If a user is on `/teams/acme/settings` and switches to team "beta", the main content area still shows Acme's settings until they explicitly navigate elsewhere — a confusing UX.

## Goals / Non-Goals

**Goals:**
- Enable the Members tab with full CRUD UI for team memberships (list, add, change role, remove)
- Enable the Danger Zone tab with an Archive Team action including confirmation
- Remove slug mutability from the update flow (backend command, handler, entity, API request, UI form)
- Navigate to dashboard root (`/`) on team switch to clear stale content

**Non-Goals:**
- Implementing team deletion (only archive is supported per domain rules)
- Adding pagination or search/filter to the members list (can be added later)
- Adding role-based visibility within the UI (all team owners see all tabs)
- Changing how the CLI handles team updates (it already doesn't expose slug updates)

## Decisions

### D1: Members tab as a data table with inline actions

The members list will use a simple table layout showing email and role, with actions (change role, remove) available per row. Adding a member will use an inline form above the table (email input + role dropdown + add button) rather than a separate page, keeping the interaction lightweight.

**Alternatives considered:**
- Separate page for add/edit member — rejected as over-engineered for a simple form
- Modal dialogs for each action — rejected for consistency with the existing settings form pattern

### D2: Danger Zone uses a confirmation dialog with slug re-typing

Archiving is irreversible and has cascading effects (blocks all mutations, event creation). The Danger Zone page will show archive as a destructive action behind a confirmation dialog that requires the user to type the team slug to confirm. This matches common patterns in GitHub/Vercel for destructive actions.

**Alternatives considered:**
- Simple "Are you sure?" dialog — rejected as too easy to accidentally confirm
- Two-step flow with a separate confirmation page — rejected as unnecessarily heavy

### D3: Remove `ChangeSlug` from the domain entity

Rather than just removing slug from the command/handler while leaving the entity method intact, we'll remove `ChangeSlug()` from the `Team` entity entirely. This makes the immutability constraint explicit at the domain level, preventing future accidental re-introduction.

**Alternatives considered:**
- Keep `ChangeSlug()` but only remove from handler — rejected because it leaves a misleading API surface on the entity
- Add a domain guard that throws — rejected because removing the method entirely is cleaner than a runtime check

### D4: Navigate to `/` on team switch

When `setSelectedTeamSlug` is called from the team switcher, the app will additionally call `router.push("/")`. The dashboard root page (`/`) renders an empty content area, providing a clean slate. This is the simplest approach that avoids showing stale data.

**Alternatives considered:**
- Navigate to the equivalent page for the new team (e.g., `/teams/{newSlug}/settings`) — rejected because we can't reliably know which page makes sense for the new team, and the page might not exist
- Clear content with a React context reset — over-engineered for the problem

### D5: BFF API routes for membership

The Admin UI uses BFF (Backend for Frontend) pattern — React client calls `/api/teams/...` routes that proxy to the Admitto API. New BFF routes will be added at `/api/teams/[teamSlug]/members/` to proxy the membership CRUD endpoints, following the same pattern as existing team and event routes.

## Risks / Trade-offs

- **[Breaking API change — slug immutability]** → Clients that send `slug` in PUT `/admin/teams/{teamSlug}` will have the field ignored (or rejected). Mitigated by the fact that the CLI already doesn't use slug updates, and the Admin UI is the only other known client.
- **[Archive is irreversible]** → Once archived, a team cannot be unarchived. Mitigated by the confirmation dialog requiring slug re-typing. The domain already enforces this — the UI just needs to make the consequences clear.
- **[Navigation reset loses context]** → Switching teams navigates away from the current page, which could be mildly annoying if the user wants to compare teams. Accepted as the lesser evil compared to showing stale data from a different team.
