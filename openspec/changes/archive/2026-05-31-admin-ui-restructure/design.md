## Context

The Admin UI is a Next.js App Router application. Event pages are nested under `(dashboard)/teams/[teamId]/events/[eventId]/`. The current navigation model has three problems:

1. **Breadcrumbs** — A `header-context.tsx` React Context propagates `title` and `breadcrumbs` state from page components up to `AppHeader`. The breadcrumbs duplicate what the sidebar already shows, contain known bugs (empty event-name label, wrong link targets), and add visual noise with no navigation benefit.

2. **Dialog components** — Add/edit forms for ticket types, badge types, and bulk email use `Dialog` modals, which trap focus on small screens. `Sheet` (slide-in panel) is a better primitive: on desktop it slides from the right, on mobile from the bottom — both feel more natural for forms.

3. **Event settings split** — Event configuration lives under a `settings/` sub-route with its own left-side sub-nav (`settings/layout.tsx`). This creates a separate navigation layer inside an already sidebar-driven layout, and forces organizers to leave the main sidebar context to reach configuration. Bulk email configuration (SMTP, templates) is split from the operational email list, requiring two separate sidebar sections.

```
CURRENT EVENT SIDEBAR               REVISED EVENT SIDEBAR
─────────────────────               ─────────────────────
Dashboard                           Dashboard
Registrations                       Edit Event ── /events/{id}/edit
Ticket types                                      └── tabs: General | Policies | Danger zone
Waitlist                            Registrations
Badges                              Ticket types
Emails ──── emails/                 Waitlist
Settings ─┐                         Badges
           ├── General              Email ──── /events/{id}/emails
           ├── Registration                    └── tabs: Campaigns | Templates | Setup
           ├── Reconfirmation
           ├── Email ──────────── settings/email/
           │    └── Templates ─── settings/email/templates/
           ├── Danger zone
```

## Goals / Non-Goals

**Goals:**
- Remove all breadcrumb rendering and the `header-context` machinery entirely
- Consolidate the `settings/` sub-area into a single tabbed **Edit Event** page at `edit/` with three sub-routes
- Unify the Email area into a single tabbed page (Campaigns | Templates | Setup)
- Convert action dialogs (add/edit ticket type, add/edit badge type, send bulk email) to Sheet
- Add Next.js redirects so old URLs don't become dead links
- Keep all existing functionality — no features removed, just restructured

**Non-Goals:**
- Changing any backend API contracts
- Modifying team-level settings (`/teams/[teamId]/settings/`) — those are unaffected
- Redesigning the content of any settings form
- Adding new features

## Decisions

### 1. Delete the `header-context` entirely, not just breadcrumbs

**Decision**: Remove `header-context.tsx`, simplify `PageLayout` to a plain `<div className="space-y-6">` wrapper, and reduce `AppHeader` to sidebar-toggle + theme-toggle only. Each page component owns its `<h1>` heading directly in its content area.

**Alternatives considered**:
- _Keep context, just hide breadcrumbs_ — More code for zero benefit; the context still propagates title state that nothing uses.
- _Keep title propagation, remove breadcrumbs only_ — Still requires the client-component machinery in `PageLayout`. Not worth it when every page already has (or should have) an inline heading.

**Rationale**: Simpler component tree, no client-side context overhead for navigation chrome.

---

### 2. Email tabs use Next.js sub-routes, not query params

**Decision**: Create `emails/layout.tsx` with tab navigation. Sub-routes: `emails/campaigns/` (list + `[jobId]/` detail), `emails/templates/` (list + `[id]/` editor), `emails/setup/` (SMTP form). The `emails/` root redirects to `emails/campaigns/`.

**Alternatives considered**:
- _Query param tabs (`?tab=campaigns`)_ — Simpler file structure, but breaks browser back/forward expectations for tab state, harder to deep-link from the sidebar, and prevents route-level code-splitting.
- _Client-side state tabs_ — Same downsides as query params.

**Rationale**: Sub-routes are idiomatic Next.js App Router, each tab is independently navigable, and the deep link from the sidebar to "Email" defaults to the Campaigns list naturally.

---

### 3. Event settings use a tabbed Edit Event page, not scattered top-level routes

**Decision**: Create an `edit/` folder at the event level with a `layout.tsx` housing tab navigation (General | Policies | Danger zone) and three sub-routes: `edit/general/`, `edit/policies/`, `edit/danger/`. The sidebar shows a single "Edit Event" item. The bare `/edit` path redirects to `/edit/general`.

The **Policies** tab combines what was two separate pages — Registration policy and Reconfirmation policy — into a single scrollable page. This reduces clicks for a common workflow (configure both at once) and keeps the tab count minimal.

**File mapping**:

| Old path | New path |
|---|---|
| `settings/page.tsx` | `edit/general/page.tsx` |
| `settings/registration/page.tsx` | `edit/policies/page.tsx` (combines registration + reconfirmation) |
| `settings/reconfirm/page.tsx` | merged into `edit/policies/page.tsx` |
| `settings/email/page.tsx` | `emails/setup/page.tsx` |
| `settings/email/templates/page.tsx` | `emails/templates/page.tsx` |
| `settings/email/templates/[id]/page.tsx` | `emails/templates/[id]/page.tsx` |
| `settings/danger/page.tsx` | `edit/danger/page.tsx` |

**Redirects registered in `next.config.ts`** (permanent, 308):

| Source | Destination |
|---|---|
| `/teams/:teamId/events/:eventId/settings` | `/teams/:teamId/events/:eventId/edit/general` |
| `/teams/:teamId/events/:eventId/settings/registration` | `/teams/:teamId/events/:eventId/edit/policies` |
| `/teams/:teamId/events/:eventId/settings/reconfirm` | `/teams/:teamId/events/:eventId/edit/policies` |
| `/teams/:teamId/events/:eventId/settings/danger` | `/teams/:teamId/events/:eventId/edit/danger` |
| `/teams/:teamId/events/:eventId/settings/email` | `/teams/:teamId/events/:eventId/emails/setup` |
| `/teams/:teamId/events/:eventId/settings/email/templates` | `/teams/:teamId/events/:eventId/emails/templates` |
| `/teams/:teamId/events/:eventId/settings/email/templates/:type` | `/teams/:teamId/events/:eventId/emails/templates/:type` |
| `/teams/:teamId/events/:eventId/emails` | `/teams/:teamId/events/:eventId/emails/campaigns` |
| `/teams/:teamId/events/:eventId/emails/:jobId` | `/teams/:teamId/events/:eventId/emails/campaigns/:jobId` |

**Alternatives considered**:
- _Separate top-level routes (details/, policy/, reconfirm/, danger/)_ — More sidebar items, more disruptive. A single "Edit Event" entry point is less intimidating and keeps configuration grouped.
- _Query param tabs (`?tab=general`)_ — Same pattern concerns as decision #2. Sub-routes are consistent with the Email page.

---

### 4. Convert Dialogs to `Sheet` from shadcn/ui

**Decision**: Replace `Dialog`/`DialogContent` with `Sheet`/`SheetContent side="right"` in the three affected components. Keep the internal form JSX unchanged; only the wrapper component and trigger change.

**Affected files**:
- `ticket-types/page.tsx` — add/edit ticket type
- `badge-types/page.tsx` — add/edit badge type
- `emails/send-bulk-email-dialog.tsx` → renamed `send-bulk-email-sheet.tsx`

**Rationale**: `Sheet` is already in the shadcn/ui installation. No new dependency. On mobile it slides from the bottom, avoiding the fixed-height Dialog overflow issue.

---

### 5. Sidebar shows "Edit Event" after Dashboard, "Email" before existing operational items

**Decision**: In `nav-event-pages.tsx`, place "Edit Event" (→ `/edit/general`) as the second item after Dashboard. Move "Email" (→ `/emails/campaigns`) to the end of the operational list. Remove "Settings". No separator needed — "Edit Event" and "Email" are grouped naturally with the other event items.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Bookmarked or hardcoded `settings/*` URLs break | Permanent redirects in `next.config.ts` catch all known patterns |
| Internal `href` references to old paths missed | Grep for all `settings/` and `emails/[^c]` href refs and update atomically with the route move |
| `emails/campaigns/[jobId]` URL change breaks email job links stored elsewhere (e.g. in emails sent to users) | There are no user-facing links to job detail pages — it's an internal organizer admin page only |
| `Sheet` z-index conflicts with existing modals | shadcn's Sheet uses a portal; no known conflicts in current codebase |

## Migration Plan

Since the Admin UI is a single-page app served to organizers (not public users), migration is straightforward:

1. Implement all route/file changes in a single PR
2. Add all redirects to `next.config.ts` in the same PR
3. Update all internal `href` references in the same PR
4. Deploy; no database migration, no phased rollout needed
5. Rollback: revert the PR (redirects in config make the new URLs temporarily dead on rollback, but old URLs work again)

## Open Questions

- None — all decisions made during the planning/explore session.
