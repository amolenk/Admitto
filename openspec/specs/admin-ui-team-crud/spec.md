# Admin UI Team CRUD

## Purpose
Provide Admin UI pages for creating teams and managing team settings, including validation, optimistic concurrency, and sidebar navigation.

## Requirements

### Requirement: Admin can create a team via the UI
The Admin UI SHALL provide a "Create Team" page with a form for name only (no email address field, no slug field). The form SHALL validate the name client-side and display server-side validation errors inline. On successful creation, the UI SHALL redirect to the new team's events page using the team's UUID and update the team switcher to select the new team.

#### Scenario: Successfully create a team
- **WHEN** an admin navigates to the "Create Team" page, fills in name "Acme Events", and submits the form
- **THEN** the team is created, the team switcher updates to show "Acme Events" as the selected team, and the admin is redirected to `/teams/{teamId}/events`

#### Scenario: Display validation errors on create
- **WHEN** an admin submits the create team form with an empty name
- **THEN** the form displays a validation error on the name field without submitting to the backend

#### Scenario: Navigate to create team from team switcher
- **WHEN** an admin clicks the "Add Team" button in the team switcher dropdown
- **THEN** the admin is navigated to the "Create Team" page

---

### Requirement: Team owner can update team details via the UI
The Admin UI SHALL provide a "Team Settings" page with a form pre-filled with the team's current name, accent color, and optional reply-to email address (no slug field). The form SHALL send partial updates (only changed fields) with the team's current version for optimistic concurrency. On successful update, the UI SHALL reflect the updated name in the team switcher and sidebar and retain the saved accent color and reply-to email address in the form.

#### Scenario: Successfully update team name
- **WHEN** a team owner navigates to the settings page for a team, changes the name, and submits
- **THEN** the team name is updated, the team switcher reflects the new name, and a success message is shown

#### Scenario: Successfully update team accent color
- **WHEN** a team owner navigates to the settings page for a team, changes the accent color to `#0f766e`, and submits
- **THEN** the team accent color is updated and a success message is shown

#### Scenario: Successfully update team reply-to email address
- **WHEN** a team owner navigates to the settings page for a team, changes the reply-to email address to `help@example.com`, and submits
- **THEN** the team reply-to email address is updated and a success message is shown

#### Scenario: Display concurrency conflict error
- **WHEN** a team owner submits an update but the team's version in the database no longer matches the version that was loaded with the form
- **THEN** the form displays an error indicating the team was modified by someone else and prompts the user to reload the page

---

### Requirement: Team settings layout renders the team name server-side
The team-settings layout (breadcrumbs, page heading, sidebar nav) SHALL be rendered as a Next.js Server Component. The team name SHALL be fetched server-side using the authenticated session so that the correct name is present in the initial HTML. No GUID or placeholder SHALL appear during page load or hard refresh.

#### Scenario: Team name is present on initial render
- **WHEN** a team owner navigates directly to `/teams/{teamId}/settings` or hard-refreshes the page
- **THEN** the breadcrumb and page heading show the team name immediately, without any GUID flash or loading state

#### Scenario: Unauthenticated access is redirected
- **WHEN** an unauthenticated user navigates to `/teams/{teamId}/settings`
- **THEN** the request is redirected to the sign-in page (existing auth middleware behaviour)

---

### Requirement: Team settings page is accessible from sidebar navigation
The Admin UI sidebar SHALL include a "Settings" navigation entry under each team that links to the team's settings page using the team's UUID.

#### Scenario: Navigate to team settings from sidebar
- **WHEN** a team owner clicks the "Settings" entry in the sidebar
- **THEN** the admin is navigated to `/teams/{teamId}/settings`

---

### Requirement: Selecting a different team resets the main content area
The Admin UI SHALL navigate to the dashboard root (`/`) when the user selects a different team in the team switcher. This ensures the main content area does not show stale content from a previously selected team.

#### Scenario: Switch team resets content
- **WHEN** a user is viewing a team settings page and switches to a different team using the team switcher
- **THEN** the user is navigated to `/` and the main content area is empty

#### Scenario: First team selection does not navigate
- **WHEN** the app loads and auto-selects the first team (no team was previously selected)
- **THEN** the user is not navigated away from the current page
