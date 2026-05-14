## Why

The Team `EmailAddress` field was originally intended as a "from" address for outgoing emails, but this role is now fulfilled by the SMTP settings. The field is redundant and misleads users into thinking it affects email sending when it does not. Additionally, team and event names are momentarily replaced by their UUID in breadcrumbs, headings, and navigation while the client-side store hydrates — this creates a jarring user experience on every page load and hard refresh. Both issues can be resolved cleanly by removing the field and migrating the affected layouts to server-side rendering.

## What Changes

- **BREAKING** Remove `EmailAddress` from the `Team` domain aggregate, create/update commands, API request/response contracts, and Admin UI forms.
- Update the `team-management` spec: remove all requirements, scenarios, and response shapes that reference the team email address.
- Update the `admin-ui-team-crud` spec: remove the email field from the Create Team and Team Settings forms and their scenarios.
- Convert the team-settings and event-settings layout components from client components that rely on a client-side `useTeams()` store to Next.js **server components** that fetch the team name (and event name where needed) directly during SSR, eliminating the GUID flash on page load and hard refresh.
- Update the `admin-ui-team-crud` and `admin-ui-event-management` specs to reflect the server-rendering approach for layout breadcrumbs and headings.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `team-management`: Remove all references to team email address — the `CreateTeam` command, `UpdateTeamDetails` command, and `GetTeamDetails` response no longer include an email field.
- `admin-ui-team-crud`: Remove the email field from Create Team and Team Settings forms; convert the team-settings layout to a server component so the team name is available at SSR time (no GUID flash).
- `admin-ui-event-management`: Convert the event-settings layout to a server component so both the team name and event name are resolved server-side (no GUID flash in breadcrumbs or headings).

## Impact

- **Backend** (`Admitto.Core` — Organization module): `Team` entity, `CreateTeamCommand`, `UpdateTeamDetailsCommand`, `TeamDetailsDto`, validators, EF mapping, migration.
- **API** (`Admitto.Api`): Request/response DTOs and validators for team create and update endpoints; OpenAPI spec regeneration.
- **Admin UI** (`Admitto.UI.Admin`): Create Team form, Team Settings form, team-settings layout, event-settings layout; generated SDK types regenerated after API contract change.
- **Specs** (`openspec/specs/`): `team-management`, `admin-ui-team-crud`, `admin-ui-event-management` delta specs.
