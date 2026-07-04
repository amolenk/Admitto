## MODIFIED Requirements

### Requirement: Team owner can update team details via the UI
The Admin UI SHALL provide a "Team Settings" page with a form pre-filled with the team's current name and accent color (no slug field, no reply-to email address field). The form SHALL send partial updates (only changed fields) with the team's current version for optimistic concurrency. The form SHALL NOT display or submit a reply-to email address; any stored reply-to value is left untouched by the form. On successful update, the UI SHALL reflect the updated name in the team switcher and sidebar and retain the saved accent color in the form.

#### Scenario: Successfully update team name
- **WHEN** a team owner navigates to the settings page for a team, changes the name, and submits
- **THEN** the team name is updated, the team switcher reflects the new name, and a success message is shown

#### Scenario: Successfully update team accent color
- **WHEN** a team owner navigates to the settings page for a team, changes the accent color to `#0f766e`, and submits
- **THEN** the team accent color is updated and a success message is shown

#### Scenario: Reply-to email address is not shown or submitted
- **WHEN** a team owner opens the settings page for a team that has a stored reply-to email address and submits a name change
- **THEN** the form shows no reply-to email field, the update request contains no reply-to fields, and the stored reply-to value is unchanged

#### Scenario: Display concurrency conflict error
- **WHEN** a team owner submits an update but the team's version in the database no longer matches the version that was loaded with the form
- **THEN** the form displays an error indicating the team was modified by someone else and prompts the user to reload the page

## ADDED Requirements

### Requirement: Team switcher lists teams alphabetically
The team switcher in the Admin UI SHALL display teams in alphabetical order by team name (case-insensitive).

#### Scenario: Teams appear in alphabetical order
- **WHEN** a user opens the team switcher and their teams are "Zebra Events", "acme", and "Beta Corp"
- **THEN** the dropdown lists them in the order "acme", "Beta Corp", "Zebra Events"
