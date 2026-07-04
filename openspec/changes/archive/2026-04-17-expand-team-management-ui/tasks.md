## 1. Make Slug Immutable (Backend)

- [x] 1.1 Remove `ChangeSlug()` method from `Team` entity (`src/Admitto.Module.Organization/Domain/Entities/Team.cs`)
- [x] 1.2 Remove `Slug` parameter from `UpdateTeamCommand` record (`UpdateTeamCommand.cs`)
- [x] 1.3 Remove slug-change logic from `UpdateTeamHandler` (`UpdateTeamHandler.cs`)
- [x] 1.4 Remove `Slug` field from `UpdateTeamHttpRequest` and update `UpdateTeamValidator` to no longer validate slug
- [x] 1.5 Update or remove any existing tests that verify slug-change behavior in `UpdateTeam` tests
- [x] 1.6 Update the `team-management` spec in `openspec/specs/` to reflect slug immutability

## 2. Make Slug Read-Only (Admin UI)

- [x] 2.1 Update the team settings form (`team-settings-form.tsx`) to display slug as a read-only field (disabled input with descriptive text)
- [x] 2.2 Remove slug from the form schema's editable fields and from the update request payload
- [x] 2.3 Remove the slug-change redirect logic from the form's `onSubmit` handler

## 3. Reset Content on Team Switch

- [x] 3.1 Update `team-switcher.tsx` to navigate to `/` when selecting a different team (not on initial auto-select)
- [x] 3.2 Verify that auto-selecting the first team on app load does not trigger a navigation

## 4. Enable Settings Tabs

- [x] 4.1 Enable the "Members" and "Danger Zone" nav items in the settings layout (`layout.tsx`) by setting `enabled: true`

## 5. Members Tab — BFF API Routes

- [x] 5.1 Create BFF route `GET /api/teams/[teamSlug]/members` proxying to `GET /admin/teams/{teamSlug}/members`
- [x] 5.2 Create BFF route `POST /api/teams/[teamSlug]/members` proxying to `POST /admin/teams/{teamSlug}/members`
- [x] 5.3 Create BFF route `PUT /api/teams/[teamSlug]/members/[email]` proxying to `PUT /admin/teams/{teamSlug}/members/{email}`
- [x] 5.4 Create BFF route `DELETE /api/teams/[teamSlug]/members/[email]` proxying to `DELETE /admin/teams/{teamSlug}/members/{email}`

## 6. Members Tab — UI Page

- [x] 6.1 Create the members page at `/teams/[teamSlug]/settings/members/page.tsx` that fetches and displays team members
- [x] 6.2 Implement the members data table with columns for email and role, and a row action to remove a member
- [x] 6.3 Implement the inline "Add Member" form (email input + role select + submit button) above the table
- [x] 6.4 Implement role change via a dropdown select in each table row that calls the change-role endpoint on selection
- [x] 6.5 Implement remove-member action with a confirmation prompt before deletion
- [x] 6.6 Handle and display server-side errors (duplicate member, member not found) inline

## 7. Danger Zone Tab — BFF API Route

- [x] 7.1 Create BFF route `POST /api/teams/[teamSlug]/archive` proxying to `POST /admin/teams/{teamSlug}/archive`

## 8. Danger Zone Tab — UI Page

- [x] 8.1 Create the danger zone page at `/teams/[teamSlug]/settings/danger/page.tsx` with the archive team action
- [x] 8.2 Implement the archive confirmation dialog requiring the user to type the team slug to confirm
- [x] 8.3 On successful archive, invalidate team queries, remove from team switcher, and navigate to `/`
- [x] 8.4 Handle and display error messages for active-events guard and concurrency conflicts

## 9. Testing

- [x] 9.1 Add/update domain tests to verify `Team` no longer has `ChangeSlug()` method
- [x] 9.2 Add/update integration tests for `UpdateTeamHandler` to confirm slug field is not accepted
- [x] 9.3 Verify all existing Organization module tests pass after slug immutability changes
