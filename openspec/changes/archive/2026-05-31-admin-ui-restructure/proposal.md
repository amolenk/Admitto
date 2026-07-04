## Why

The Admin UI has accumulated navigation debt: redundant breadcrumbs that duplicate the sidebar, inconsistently used dialogs and page-level layouts, and a separate "Settings" sub-area for event configuration that fragments the organizer's workflow. The goal is a cleaner, more consistent, mobile-friendly navigation model before more features are added on top of the current structure.

## What Changes

- **Remove breadcrumbs** from all event and team pages. The sidebar already shows full context; breadcrumbs add visual noise and contain known bugs (empty labels, wrong link targets).
- **Simplify the top header** to sidebar-toggle + theme-toggle only. Each page owns its own content-area `<h1>` heading.
- **Convert action dialogs to Sheets** (slide-in from right/bottom). Affects: add/edit ticket type, add/edit badge type, send bulk email. Sheets are mobile-friendlier and avoid the "modal trap" on small screens.
- **Unify the Email area** into a single tabbed page with three tabs: **Campaigns** (bulk email list + detail), **Templates** (email template editor), **Setup** (SMTP + sender config). This resolves the current split where operational email features lived under `emails/` and configuration lived under `settings/email/`.
- **Consolidate event settings** into a single tabbed **Edit Event** page placed after Dashboard in the sidebar. Three tabs: **General** (event details), **Policies** (registration policy + additional details + reconfirmation policy combined), **Danger zone**. The shared `settings/` sub-layout and left-side sub-nav are removed. **BREAKING** – URL paths change.
- **Update the event sidebar** to replace the "Settings" item with "Edit Event" (linking to `/edit/general`) after Dashboard, and add "Email" (linking to the tabbed email page).
- **Delete** the orphaned `settings/registration/ticket-types-section.tsx` component file (never imported).

## Capabilities

### New Capabilities

- None — this change restructures existing capabilities, it does not introduce new user-facing features.

### Modified Capabilities

- `admin-ui-event-management`: The tabbed `settings/` sub-layout and URL structure (`/teams/{teamId}/events/{eventId}/settings`) are replaced by a new tabbed Edit Event page at `/teams/{teamId}/events/{eventId}/edit`. **BREAKING** URL changes throughout.
- `admin-ui-bulk-emails`: The bulk email list and detail pages move from `emails/` and `emails/{jobId}` to `emails/campaigns/` and `emails/campaigns/{jobId}`. **BREAKING** URL change.
- `admin-ui-email-templates`: Template pages move from `settings/email/templates/` to `emails/templates/`. **BREAKING** URL change.

## Impact

- **Files deleted**: `settings/layout.tsx`, `settings/nav-links.tsx`, all `settings/` sub-pages after migration, `settings/registration/ticket-types-section.tsx` (orphan), `header-context.tsx`, `PageLayout` breadcrumb machinery.
- **Files moved/renamed**: ~6 page files relocated to new route paths (see design for full mapping).
- **UI components**: All pages need breadcrumb calls removed; `AppHeader` simplified; ~3 dialog components converted to Sheet.
- **No backend changes** required — all changes are frontend routing and UI component structure.
- **No API contract changes**.
- **Internal links** in the Admin UI that reference `settings/*` paths must be updated to the new route paths.
