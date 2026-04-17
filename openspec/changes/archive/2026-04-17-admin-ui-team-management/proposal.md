## Why

The Admin UI currently lets users switch between teams and view events, but there is no way to create a new team or update an existing team's details (name, slug, email) through the UI. The backend API already fully supports these operations (`POST /admin/teams` and `PUT /admin/teams/{teamSlug}`), so this is purely a frontend gap. Without these forms, admins must use the CLI or direct API calls to manage teams.

## What Changes

- Add a "Create Team" page accessible from the team switcher's "Add Team" button, with a form for slug, name, and email address.
- Add a "Team Settings" page under each team, with a form to update the team's name, slug, and email address, using optimistic concurrency (version field).
- Add a "Team Settings" entry to the sidebar navigation.
- Wire both forms to the existing backend API endpoints via proxy routes and the generated OpenAPI client.

## Capabilities

### New Capabilities
- `admin-ui-team-crud`: Admin UI forms for creating and updating teams, including proxy routes, form validation, sidebar navigation, and error handling.

### Modified Capabilities

_(none — the backend `team-management` spec is unchanged; this is a pure frontend addition)_

## Impact

- **Code**: New pages, components, proxy routes, and hooks in `src/Admitto.UI.Admin/`.
- **APIs**: No backend changes. Uses existing `POST /admin/teams` and `PUT /admin/teams/{teamSlug}` endpoints.
- **Dependencies**: Uses shadcn/ui form components and the existing generated OpenAPI client.
