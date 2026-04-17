# Admin UI Team Membership

## Purpose
Provide Admin UI pages for managing team members within the Members settings tab, including listing members, adding new members, changing roles, and removing members.

## Requirements

### Requirement: Team owner can view team members in the Members tab
The Admin UI SHALL provide a "Members" page under Team Settings that displays all team members in a table with their email address and role. The page SHALL be accessible via the Members tab in the settings navigation.

#### Scenario: View members list
- **WHEN** a team owner navigates to the Members tab for team "acme" which has members "alice@example.com" (Crew) and "bob@example.com" (Owner)
- **THEN** the members table displays both members with their email addresses and roles

#### Scenario: View empty members list
- **WHEN** a team owner navigates to the Members tab for team "acme" which has no members
- **THEN** the page displays an empty state message indicating no members have been added

#### Scenario: Members tab is accessible from settings navigation
- **WHEN** a team owner navigates to the settings page for team "acme"
- **THEN** the Members tab is enabled and clickable in the settings navigation, linking to `/teams/acme/settings/members`

---

### Requirement: Team owner can add a member via the UI
The Admin UI SHALL provide an inline form on the Members page to add a new member by entering their email address and selecting a role (Crew, Organizer, or Owner). On successful addition, the members table SHALL update to include the new member. The form SHALL display server-side validation errors inline.

#### Scenario: Successfully add a team member
- **WHEN** a team owner enters "charlie@example.com" with role "Crew" in the add member form and submits
- **THEN** the member is added, the members table refreshes to show "charlie@example.com" with role Crew, and the form is cleared

#### Scenario: Display error when adding duplicate member
- **WHEN** a team owner enters "alice@example.com" who is already a member of the team and submits
- **THEN** the form displays an error message indicating the user is already a member

#### Scenario: Display validation error for invalid email
- **WHEN** a team owner enters "not-an-email" in the email field and submits
- **THEN** the form displays a validation error on the email field without submitting to the backend

---

### Requirement: Team owner can change a member's role via the UI
The Admin UI SHALL allow team owners to change a member's role by selecting a new role from a dropdown in the members table row. The change SHALL take effect immediately on selection.

#### Scenario: Successfully change a member's role
- **WHEN** a team owner changes "alice@example.com"'s role from Crew to Organizer using the role dropdown
- **THEN** the member's role is updated to Organizer and the members table reflects the change

#### Scenario: Display error on role change failure
- **WHEN** a team owner attempts to change a member's role and the backend returns an error
- **THEN** an error notification is displayed and the role reverts to its previous value

---

### Requirement: Team owner can remove a member via the UI
The Admin UI SHALL allow team owners to remove a member from the team using a remove action in the members table row. The UI SHALL confirm the removal before executing it.

#### Scenario: Successfully remove a team member
- **WHEN** a team owner clicks the remove action for "alice@example.com" and confirms the removal
- **THEN** the member is removed and the members table refreshes without "alice@example.com"

#### Scenario: Cancel member removal
- **WHEN** a team owner clicks the remove action for "alice@example.com" and cancels the confirmation
- **THEN** the member is not removed and the members table remains unchanged
