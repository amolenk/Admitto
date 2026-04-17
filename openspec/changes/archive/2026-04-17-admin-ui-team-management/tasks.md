## 1. Proxy Routes

- [x] 1.1 Add `POST /api/teams` proxy route that calls `createTeam` from the HeyAPI SDK and returns the response or error
- [x] 1.2 Add `GET /api/teams/[teamSlug]` proxy route that calls `getTeam` from the HeyAPI SDK to fetch a single team's details
- [x] 1.3 Add `PUT /api/teams/[teamSlug]` proxy route that calls `updateTeam` from the HeyAPI SDK with the request body

## 2. Create Team Page

- [x] 2.1 Create the create-team form component (`app/(dashboard)/teams/add/create-team-form.tsx`) with fields for slug, name, and email address, using `useCustomForm` with Zod validation
- [x] 2.2 Create the create-team page (`app/(dashboard)/teams/add/page.tsx`) using `PageLayout`
- [x] 2.3 Wire the form submission to `POST /api/teams`, handle success (redirect to new team's events page, refresh team list) and errors (display `FormError` inline)
- [x] 2.4 Wire the "Add Team" button in `team-switcher.tsx` to navigate to `/teams/add`

## 3. Team Settings Layout & General Page

- [x] 3.1 Create the settings layout (`app/(dashboard)/teams/[teamSlug]/settings/layout.tsx`) with a side-nav listing "General" (active now), "Members" (placeholder/disabled), and "Danger Zone" (placeholder/disabled)
- [x] 3.2 Create the team-settings form component (`app/(dashboard)/teams/[teamSlug]/settings/team-settings-form.tsx`) with pre-filled fields for slug, name, and email address, using `useCustomForm` with Zod validation
- [x] 3.3 Create the general settings page (`app/(dashboard)/teams/[teamSlug]/settings/page.tsx`) that fetches the current team details and renders the form
- [x] 3.4 Wire the form submission to `PUT /api/teams/[teamSlug]` with partial update (only changed fields) and `expectedVersion`, handle success (refresh team data, redirect if slug changed) and concurrency errors

## 4. Sidebar Navigation

- [x] 4.1 Add a "Settings" navigation entry to the sidebar (`app-sidebar.tsx` or `nav-events.tsx`) that links to `/teams/[teamSlug]/settings`

## 5. Verification

- [x] 5.1 Verify the UI builds without errors (`pnpm build`)
- [x] 5.2 Manually verify create team flow: form renders, validation works, team is created, redirect and team switcher update work
- [x] 5.3 Manually verify update team flow: form pre-fills, partial update works, slug change redirects, concurrency error displays
- [x] 5.4 Use Playwright (via `playwright-cli` skill) to screenshot the create-team and team-settings pages and review that the UI is clear, well-laid-out, and user-friendly
