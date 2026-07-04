## 1. UI scaffolding and proxy

- [x] 1.1 Add a Next.js API proxy at `app/api/teams/[teamSlug]/email-settings/route.ts` mirroring `app/api/teams/[teamSlug]/events/[eventSlug]/email-settings/route.ts` (forward `GET`, `PUT`, `DELETE` to the backend with auth-token plumbing)
- [x] 1.2 Add an "Email" entry to `navItems` in `app/(dashboard)/teams/[teamSlug]/settings/layout.tsx` (icon `Mail`, href `/email`, description "SMTP and sender identity")

## 2. Refactor `EmailSettingsForm` to be scope-agnostic

- [x] 2.1 Change the form props to accept a scope-agnostic config: `{ apiUrl, queryKey, hasPassword?: boolean, version: number | null, initialValues }` instead of `teamSlug`/`eventSlug`
- [x] 2.2 Update the existing event Email tab (`settings/email/page.tsx` and `email-settings-form.tsx`) to pass the new props; verify the visible form output is unchanged
- [x] 2.3 Move shared types (the zod schema, `Values` type) into a sibling file so the team page can import them

## 3. Team Email page

- [x] 3.1 Create `app/(dashboard)/teams/[teamSlug]/settings/email/page.tsx` that fetches `GET /api/teams/{teamSlug}/email-settings` (404 → null, matching the event page pattern) and mounts the refactored form
- [x] 3.2 Wire the form's `apiUrl` to `/api/teams/{teamSlug}/email-settings` and `queryKey` to `["team-email-settings", teamSlug]`
- [x] 3.3 Add a "Delete team email settings" action that is rendered only when settings exist; show a confirmation dialog and on confirm issue `DELETE /api/teams/{teamSlug}/email-settings` with the loaded `Version`
- [x] 3.4 On successful delete, invalidate the team-scoped query key so the page re-renders in the empty/no-settings state and the delete action is hidden

## 4. Inheritance indicator on the event Email tab

- [x] 4.1 In the event `settings/email/page.tsx`, fire a parallel `GET /api/teams/{teamSlug}/email-settings` (404 → null) alongside the existing event-scoped GET
- [x] 4.2 Compute the UI state from the four (event-row × team-row) combinations as defined in `design.md`
- [x] 4.3 Render an "Inherited from team settings" callout when only team-scoped settings exist, with a link to `/teams/{teamSlug}/settings/email`
- [x] 4.4 Render an "Overriding team settings" callout when both scopes exist, with a link to `/teams/{teamSlug}/settings/email`
- [x] 4.5 Render no callout when team-scoped settings do not exist (regardless of event-scope state)

## 5. Manual verification

- [ ] 5.1 Start the dev server and confirm the team settings sidebar shows four entries (General, Members, Email, Danger zone) with correct active states
- [ ] 5.2 Walk through the three team-Email states: no settings → create; with settings → update; with settings → delete (confirmation + return to empty)
- [ ] 5.3 Walk through the four event-Email states described in the design table and confirm callouts (or the absence of callouts) match expectations
- [ ] 5.4 Verify password masking behaviour on both pages (saved password kept when blank on update; required when authMode=basic and no row exists yet)
- [ ] 5.5 Verify a non-team-member receives a `403`-derived error (or appropriate empty state) when navigating directly to `/teams/{teamSlug}/settings/email`

## 6. Spec maintenance

- [x] 6.1 Run `openspec validate add-team-email-settings-page` and resolve any reported issues
- [ ] 6.2 After implementation lands, archive the change with `openspec archive add-team-email-settings-page` so the new `admin-ui-team-email-settings` capability and the modified `admin-ui-event-management` requirement are folded into `openspec/specs/`
