# Proposal: remove-reply-to-ui-and-sort-teams

## Why

The team reply-to email address is no longer used by the email sending pipeline, so exposing it in the Admin UI settings form invites confusion. A full backend cleanup is deferred; for now only the UI surface is removed. Separately, the team switcher in the Admin UI lists teams in undefined (database) order, which makes finding a team harder as the number of teams grows.

## What Changes

- Remove the "Reply-to email" field from the Team Settings form in the Admin UI (`team-settings-form.tsx`): schema field, default value, submit logic (`replyToEmailAddress` / `clearReplyToEmailAddress`), and the form field markup.
- Keep the reply-to setting in the API endpoint (`UpdateTeamHttpRequest`, `TeamDto`), domain model, and database untouched — cleanup happens in a later change.
- Order teams alphabetically by name (case-insensitive) in `GetTeamsHandler` for both the admin branch and the member branch, so the team switcher (and any other consumer of the team list) shows teams in alphabetical order.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `admin-ui-team-crud`: The Team Settings form no longer shows or submits a reply-to email address; the team switcher displays teams in alphabetical order.
- `team-management`: Team list endpoints (admin list and "my teams" list) return teams ordered alphabetically by name.

## Impact

- **Admin UI**: `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/settings/team-settings-form.tsx` (remove reply-to field). No proxy route or generated SDK changes needed — the API contract is unchanged.
- **Backend**: `src/Admitto.Core/Organization/Application/UseCases/Teams/GetTeams/GetTeamsHandler.cs` (add `OrderBy` on team name in both branches). No API contract change, so no SDK regeneration required.
- **Not affected**: `Team` aggregate, `UpdateTeam`/`GetTeam` use cases, Email module projections, database schema/migrations — reply-to stays in place there.
- **Tests**: Existing team-settings UI expectations (if any) referencing reply-to; add/adjust query handler tests for ordering.
