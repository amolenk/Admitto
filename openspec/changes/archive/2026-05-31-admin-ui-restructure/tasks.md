## 1. Breadcrumb Removal

- [x] 1.1 Remove `setBreadcrumbs` calls and breadcrumb imports from all event pages (dashboard, registrations, ticket-types, waitlist, badge-types, emails, and all settings sub-pages)
- [x] 1.2 Remove `setBreadcrumbs` calls and breadcrumb imports from all team pages (settings sub-pages, membership, api-keys, etc.)
- [x] 1.3 Remove `breadcrumbs` prop and breadcrumb rendering logic from `AppHeader` component; leave only sidebar-toggle and theme-toggle
- [x] 1.4 Remove breadcrumb-related state and exports from `header-context.tsx`; simplify or delete the context if only title state remains
- [x] 1.5 Remove `breadcrumbs` prop from `PageLayout` component; simplify `PageLayout` to a plain `<div className="space-y-6">` wrapper (remove client component requirement if no longer needed)
- [x] 1.6 Delete `header-context.tsx` if fully empty after cleanup; update any remaining consumers

## 2. Header Simplification

- [x] 2.1 Remove title display (`<h1>` or title rendering) from `AppHeader` — the header bar shows only sidebar-toggle and theme-toggle
- [x] 2.2 Audit all event pages and confirm each has a content-area `<h1>` heading; add any missing headings
- [x] 2.3 Audit all team pages and confirm each has a content-area `<h1>` heading; add any missing headings
- [x] 2.4 Remove `setTitle` calls from all pages now that header no longer renders a title

## 3. Dialogs to Sheets

- [x] 3.1 Convert add/edit ticket type Dialog to `Sheet` in `ticket-types/page.tsx` (replace `Dialog`/`DialogContent` with `Sheet`/`SheetContent side="right"`)
- [x] 3.2 Convert add/edit badge type Dialog to `Sheet` in `badge-types/page.tsx`
- [x] 3.3 Convert send bulk email Dialog to `Sheet`; rename `send-bulk-email-dialog.tsx` → `send-bulk-email-sheet.tsx` and update the import in `emails/page.tsx`

## 4. Email Page Unification

- [x] 4.1 Create `emails/layout.tsx` with a tab navigation bar (Campaigns | Templates | Setup) as a Next.js layout shared by all `emails/*` sub-routes
- [x] 4.2 Move `emails/page.tsx` → `emails/campaigns/page.tsx` (bulk email list; update any internal link from `emails/[jobId]` to `emails/campaigns/[jobId]`)
- [x] 4.3 Move `emails/[jobId]/page.tsx` → `emails/campaigns/[jobId]/page.tsx`; update "Back" link to point to `emails/campaigns`
- [x] 4.4 Move `settings/email/page.tsx` → `emails/setup/page.tsx`; update "Templates" link to point to `emails/templates`; update "Inherited from team" callout link (stays `/teams/{teamSlug}/settings/email`)
- [x] 4.5 Create `emails/templates/` directory and move `settings/email/templates/page.tsx` → `emails/templates/page.tsx`; update row click href to `emails/templates/{type}`
- [x] 4.6 Move `settings/email/templates/[id]/page.tsx` → `emails/templates/[id]/page.tsx`; update "Back to templates" link to `emails/templates`
- [x] 4.7 Add a redirect from `emails/` (bare) to `emails/campaigns` in `next.config.ts`

## 5. Edit Event Tabbed Page

- [x] 5.1 Create `edit/layout.tsx` with tab navigation (General | Policies | Danger zone) as a Next.js layout shared by `edit/*` sub-routes; add a redirect from bare `/edit` → `/edit/general`
- [x] 5.2 Create `edit/general/page.tsx` by moving content from `settings/page.tsx` (general event info form); ensure page has a content-area `<h1>`
- [x] 5.3 Create `edit/policies/page.tsx` by combining `settings/registration/page.tsx` (RegistrationPolicyForm + AdditionalDetailsEditor) and `settings/reconfirm/page.tsx` (ReconfirmPolicyForm) into a single scrollable page; ensure page has a content-area `<h1>`
- [x] 5.4 Create `edit/danger/page.tsx` by moving content from `settings/danger/page.tsx`; ensure page has a content-area `<h1>`
- [x] 5.5 Add all `settings/*` → new path redirects to `next.config.ts` (see design.md redirect table for full list)
- [x] 5.6 Search codebase for all `href` references to old `settings/*` paths and update to new routes (including post-event-creation redirect → `/edit/general`)

## 6. Sidebar Update

- [x] 6.1 Update `nav-event-pages.tsx`: remove the "Settings" navigation item
- [x] 6.2 Add "Edit Event" item after Dashboard, linking to `/edit/general`
- [x] 6.3 Ensure "Email" item links to `/emails/campaigns` (or `/emails` — redirects to campaigns)

## 7. Settings Teardown

- [x] 7.1 Delete `settings/layout.tsx`
- [x] 7.2 Delete `settings/nav-links.tsx`
- [x] 7.3 Delete `settings/page.tsx` (content moved to `edit/general/page.tsx`)
- [x] 7.4 Delete `settings/registration/page.tsx` and `settings/registration/` directory (content merged into `edit/policies/page.tsx`)
- [x] 7.5 Delete `settings/reconfirm/page.tsx` and directory (content merged into `edit/policies/page.tsx`)
- [x] 7.6 Delete `settings/email/` directory tree (all files moved to `emails/`)
- [x] 7.7 Delete `settings/danger/page.tsx` and directory
- [x] 7.8 Delete orphaned `settings/registration/ticket-types-section.tsx` (never imported)
- [x] 7.9 Verify no remaining imports reference deleted files; run `pnpm build` to confirm no broken references

## 8. Tests and Validation

- [x] 8.1 Run existing Admin UI tests (`pnpm test` if any) and confirm no regressions
- [x] 8.2 Manually verify all new routes load correctly in the running app
- [x] 8.3 Manually verify old `settings/*` and `emails/*` URLs redirect correctly
- [x] 8.4 Verify Sheet components open/close correctly for ticket type, badge type, and send bulk email
- [x] 8.5 Verify the Email tabbed page shows correct active tab for each sub-route
- [x] 8.6 Verify sidebar shows separator and all new navigation items with correct active states
