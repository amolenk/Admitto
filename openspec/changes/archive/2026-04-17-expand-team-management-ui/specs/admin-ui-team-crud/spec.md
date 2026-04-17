## MODIFIED Requirements

### Requirement: Team owner can update team details via the UI
The Admin UI SHALL provide a "Team Settings" page with a form pre-filled with the team's current name and email address. The slug SHALL be displayed as a read-only field (visible but not editable). The form SHALL send partial updates (only changed fields) with the team's current version for optimistic concurrency. On successful update, the UI SHALL reflect the updated details in the team switcher and sidebar.

#### Scenario: Successfully update team name
- **WHEN** a team owner navigates to the settings page for team "acme", changes the name to "Acme Corp", and submits
- **THEN** the team name is updated, the team switcher reflects "Acme Corp", and a success message is shown

#### Scenario: Slug is displayed as read-only
- **WHEN** a team owner navigates to the settings page for team "acme"
- **THEN** the slug field displays "acme" and is not editable

#### Scenario: Display concurrency conflict error
- **WHEN** a team owner submits an update but the team's version in the database no longer matches the version that was loaded with the form
- **THEN** the form displays an error indicating the team was modified by someone else and prompts the user to reload the page

#### Scenario: Display server-side validation errors on update
- **WHEN** a team owner submits an update with an invalid email address
- **THEN** the form displays the server-side validation error on the email field

## REMOVED Requirements

### Requirement: Successfully update team slug
**Reason**: Slugs are now immutable after team creation. The slug field is displayed as read-only in the settings form. This removes the scenario where updating a slug triggers a URL redirect.
**Migration**: The slug field in the settings form becomes a read-only display. Remove slug from the form's editable fields and from the update request payload. Remove the slug-change redirect logic.

## ADDED Requirements

### Requirement: Selecting a different team resets the main content area
The Admin UI SHALL navigate to the dashboard root (`/`) when the user selects a different team in the team switcher. This ensures the main content area does not show stale content from a previously selected team.

#### Scenario: Switch team resets content
- **WHEN** a user is viewing `/teams/acme/settings` and switches to team "beta" using the team switcher
- **THEN** the user is navigated to `/` and the main content area is empty

#### Scenario: First team selection does not navigate
- **WHEN** the app loads and auto-selects the first team (no team was previously selected)
- **THEN** the user is not navigated away from the current page
