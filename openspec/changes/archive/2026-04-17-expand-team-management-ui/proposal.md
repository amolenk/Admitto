## Why

The team settings UI has placeholder tabs for Members and Danger Zone that aren't functional yet, while the backend APIs for team membership management and team archiving already exist. Additionally, the slug field is still editable on the settings form even though slugs should be immutable after creation (they're used as stable URL identifiers). Finally, switching teams in the sidebar doesn't reset the main content area, which can leave stale content from a previously selected team visible and confuse users.

## What Changes

- **Implement Members settings tab**: Build a UI page for listing, adding, changing roles of, and removing team members — backed by the existing `/admin/teams/{teamSlug}/members` endpoints.
- **Implement Danger Zone settings tab**: Build a UI page for destructive team actions, starting with the Archive Team action — backed by the existing `/admin/teams/{teamSlug}/archive` endpoint.
- **Make team slug immutable**: **BREAKING** — Remove the ability to change a team's slug after creation. Remove the `Slug` field from the update team request/command/handler/form. The slug field in the settings form becomes read-only (displayed but not editable).
- **Reset content on team switch**: When the user selects a different team in the team switcher, navigate to the dashboard root (`/`) so the main content area resets. This prevents showing stale content from the previously selected team.

## Capabilities

### New Capabilities
- `admin-ui-team-membership`: Admin UI pages for the Members settings tab — list members, add a member, change a member's role, and remove a member.
- `admin-ui-team-danger-zone`: Admin UI page for the Danger Zone settings tab — archive a team with confirmation dialog and appropriate guards.

### Modified Capabilities
- `team-management`: Make slug immutable after team creation — remove slug from update command and reject slug changes.
- `admin-ui-team-crud`: Remove slug editing from the settings form (display as read-only), and navigate to dashboard root on team switch to reset content.

## Impact

- **Backend (Organization module)**: `UpdateTeamCommand`, `UpdateTeamHandler`, `UpdateTeamHttpRequest`, `UpdateTeamValidator` — remove `Slug` parameter. `Team` entity — remove `ChangeSlug` method.
- **Admin UI**: Settings layout (`layout.tsx`) — enable Members and Danger Zone tabs. New pages at `/teams/[teamSlug]/settings/members` and `/teams/[teamSlug]/settings/danger`. Team settings form — make slug read-only. Team switcher / store — navigate to `/` on team change.
- **API contract**: The `PUT /admin/teams/{teamSlug}` endpoint will no longer accept a `slug` field. This is a breaking change for any client sending slug updates.
- **CLI**: Any CLI commands that update team slug will need to be updated to remove that capability.
- **Tests**: Existing tests for slug update scenarios need to be updated or removed.
