# Admin UI Team CRUD

## Purpose
Provide Admin UI pages for creating teams and managing team settings, including validation, optimistic concurrency, and sidebar navigation.

## Requirements

### Requirement: Admin can create a team via the UI
The Admin UI SHALL provide a "Create Team" page with a form for slug, name, and email address. The form SHALL validate inputs client-side and display server-side validation errors inline. On successful creation, the UI SHALL redirect to the new team's events page and update the team switcher to select the new team.

#### Scenario: Successfully create a team
- **WHEN** an admin navigates to the "Create Team" page, fills in slug "acme", name "Acme Events", email "info@acme.org", and submits the form
- **THEN** the team is created, the team switcher updates to show "Acme Events" as the selected team, and the admin is redirected to `/teams/acme/events`

#### Scenario: Display validation errors on create
- **WHEN** an admin submits the create team form with an empty name
- **THEN** the form displays a validation error on the name field without submitting to the backend

#### Scenario: Display server-side errors on create
- **WHEN** an admin submits the create team form with a slug that already exists
- **THEN** the form displays the server-side error message returned by the backend

#### Scenario: Navigate to create team from team switcher
- **WHEN** an admin clicks the "Add Team" button in the team switcher dropdown
- **THEN** the admin is navigated to the "Create Team" page

---

### Requirement: Team owner can update team details via the UI
The Admin UI SHALL provide a "Team Settings" page with a form pre-filled with the team's current slug, name, and email address. The form SHALL send partial updates (only changed fields) with the team's current version for optimistic concurrency. On successful update, the UI SHALL reflect the updated details in the team switcher and sidebar.

#### Scenario: Successfully update team name
- **WHEN** a team owner navigates to the settings page for team "acme", changes the name to "Acme Corp", and submits
- **THEN** the team name is updated, the team switcher reflects "Acme Corp", and a success message is shown

#### Scenario: Successfully update team slug
- **WHEN** a team owner changes the slug from "acme" to "acme-corp" and submits
- **THEN** the team slug is updated and the admin is redirected to `/teams/acme-corp/settings`

#### Scenario: Display concurrency conflict error
- **WHEN** a team owner submits an update but the team's version in the database no longer matches the version that was loaded with the form
- **THEN** the form displays an error indicating the team was modified by someone else and prompts the user to reload the page

#### Scenario: Display server-side validation errors on update
- **WHEN** a team owner submits an update with an invalid email address
- **THEN** the form displays the server-side validation error on the email field

---

### Requirement: Team settings page is accessible from sidebar navigation
The Admin UI sidebar SHALL include a "Settings" navigation entry under each team that links to the team's settings page.

#### Scenario: Navigate to team settings from sidebar
- **WHEN** a team owner clicks the "Settings" entry in the sidebar for team "acme"
- **THEN** the admin is navigated to `/teams/acme/settings`
