## Why

Archived teams and events currently remain visible in the Admin UI and are returned by the admin API, even though the domain intent is to retire them. This creates confusion for admins who see stale, inactive items in their lists. The existing `event-management` and `team-management` specs already mandate exclusion of archived items from listings; this change closes the gap between spec and implementation, and extends it explicitly to the Admin UI so archived items vanish immediately upon archiving.

## What Changes

- **API – List Events**: `GET /admin/teams/{teamSlug}/events` SHALL exclude events with `Archived` status. No new endpoint; the existing listing is corrected.
- **API – List Teams**: `GET /admin/teams` SHALL exclude teams with `Archived` status. No new endpoint; the existing listing is corrected.
- **Admin UI – Events list**: After a successful archive action, the archived event is removed from the UI list immediately (optimistic/reactive update) without requiring a page reload.
- **Admin UI – Teams list (team switcher)**: After a successful team archive, the team already disappears from the switcher (covered by `admin-ui-team-danger-zone`); this change ensures the API no longer returns archived teams so any subsequent fetch or page load is also clean.

No new endpoints are introduced. If a future requirement needs to view archived items, a dedicated endpoint will be created at that time.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `event-management`: Add explicit "admin list excludes archived" scenario for the admin listing endpoint (`GET /admin/teams/{teamSlug}/events`), mirroring the existing team-member listing rule.
- `team-management`: The "Admin can list all active teams" requirement already states archived teams are excluded; add an explicit scenario for the admin API endpoint filter and confirm correctness.
- `admin-ui-event-management`: Add requirement that archived events are excluded from the events list page and disappear immediately after an archive action without a full page reload.

## Impact

- `Admitto.Module.Registrations` – `GetEventsHandler` (or equivalent query): add `.Where(e => e.Status != EventStatus.Archived)` filter if not already present.
- `Admitto.Module.Organization` – `GetTeamsHandler`: add `.Where(t => !t.IsArchived)` filter if not already present.
- `Admitto.UI.Admin` – events list page: update after-archive mutation to remove the item from local state (React optimistic update / cache invalidation).
- No schema migrations required; no breaking API contract changes (this is a filter on existing endpoints).
