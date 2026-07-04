# Admin UI Team Danger Zone

## Purpose
Provide an Admin UI page for destructive team actions under the Danger Zone settings tab, starting with the ability to archive a team.

## Requirements

### Requirement: Team owner can archive a team via the Danger Zone tab
The Admin UI SHALL provide a "Danger Zone" page under Team Settings that displays the Archive Team action. The archive action SHALL be styled as a destructive action and SHALL require the user to type the team's slug in a confirmation dialog before the action is executed. On successful archive, the UI SHALL navigate the user to the dashboard root and update the team list.

#### Scenario: Successfully archive a team
- **WHEN** a team owner clicks the "Archive Team" button for team "acme", types "acme" in the confirmation dialog, and confirms
- **THEN** the team is archived, the team is removed from the team switcher, and the user is navigated to the dashboard root

#### Scenario: Cancel archive action
- **WHEN** a team owner clicks the "Archive Team" button and then cancels the confirmation dialog
- **THEN** the team is not archived and the user remains on the Danger Zone page

#### Scenario: Reject archive with incorrect confirmation
- **WHEN** a team owner clicks the "Archive Team" button and types a slug that does not match the team's slug
- **THEN** the confirm button remains disabled and the archive action cannot be executed

#### Scenario: Display error when team has active events
- **WHEN** a team owner attempts to archive team "acme" which has upcoming ticketed events
- **THEN** the confirmation dialog displays an error message indicating the team cannot be archived because it has active events

#### Scenario: Display concurrency conflict error on archive
- **WHEN** a team owner attempts to archive a team but the team's version has changed since the page loaded
- **THEN** an error message is displayed indicating the team was modified by someone else and prompts the user to reload

#### Scenario: Danger Zone tab is accessible from settings navigation
- **WHEN** a team owner navigates to the settings page for team "acme"
- **THEN** the Danger Zone tab is enabled and clickable in the settings navigation, linking to `/teams/acme/settings/danger`
