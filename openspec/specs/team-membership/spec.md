## Purpose

Team owners manage who belongs to their teams and in what capacity. The system provisions and deprovisions identity provider accounts as memberships are added and removed.

## Requirements

### Requirement: Team owner can add a member
The system SHALL allow team owners to add a member to a team by email address and
role (Crew, Organizer, or Owner). If no user exists for the given email, the system
SHALL create a new user record. The system SHALL reject adding a member who already
has a membership in the same team.

#### Scenario: Add a new user as a team member
- **WHEN** an owner of team "acme" adds "alice@example.com" as a Crew member and no user exists with that email
- **THEN** a new user is created with email "alice@example.com" and the user has a Crew membership in team "acme"

#### Scenario: Add an existing user as a team member
- **WHEN** an owner of team "acme" adds "bob@example.com" as an Organizer and the user exists but is not a member
- **THEN** "bob@example.com" has an Organizer membership in team "acme"

#### Scenario: Reject adding a duplicate member
- **WHEN** an owner of team "acme" adds "alice@example.com" who is already a Crew member of team "acme"
- **THEN** the request is rejected because the user is already a member of the team

#### Scenario: Non-owner cannot manage members
- **WHEN** a Crew member of team "acme" attempts to add a new member
- **THEN** the request is rejected as unauthorized

#### Scenario: Admin bypasses ownership check
- **WHEN** an admin (not a member of team "acme") adds "alice@example.com" as a Crew member
- **THEN** "alice@example.com" has a Crew membership in team "acme"

---

### Requirement: Team owner can list members
The system SHALL allow team owners to list all members of their team, including each
member's email and role.

#### Scenario: List team members
- **WHEN** an owner of team "acme" lists members and "alice@example.com" is Crew and "bob@example.com" is Owner
- **THEN** the response includes both members with their respective roles

#### Scenario: List members of a team with no members
- **WHEN** an admin lists the members of team "acme" which has no members
- **THEN** an empty list is returned

---

### Requirement: Team owner can change a member's role
The system SHALL allow team owners to change a member's role within the team.

#### Scenario: Change a member's role
- **WHEN** an owner changes "alice@example.com"'s role from Crew to Organizer in team "acme"
- **THEN** "alice@example.com" has an Organizer membership in team "acme"

#### Scenario: Change role of a non-member
- **WHEN** an owner attempts to change the role of "charlie@example.com" who is not a member of team "acme"
- **THEN** the request is rejected because the user is not a member of the team

---

### Requirement: Team owner can remove a member
The system SHALL allow team owners to remove a member from a team. Removing a
member from one team SHALL not affect memberships in other teams.

#### Scenario: Remove a member from a team
- **WHEN** an owner removes "alice@example.com" from team "acme" and she is also a member of team "beta"
- **THEN** "alice@example.com" is no longer a member of team "acme" and remains a member of team "beta"

#### Scenario: Remove a non-member
- **WHEN** an owner attempts to remove "charlie@example.com" who is not a member of team "acme"
- **THEN** the request is rejected because the user is not a member of the team

---

### Requirement: New users are provisioned in the identity provider
When a new user is created, the system SHALL asynchronously provision their account
in the external identity provider.

#### Scenario: Provision identity provider account for new user
- **WHEN** "alice@example.com" is added as a member of team "acme" and no user existed before
- **THEN** a new user is created and an identity provider account is asynchronously provisioned for "alice@example.com"

---

### Requirement: Users without team memberships are deprovisioned
When a user's last team membership is removed, the system SHALL schedule identity
provider account deprovisioning after a configurable grace period. If a user regains
a team membership during the grace period, the system SHALL cancel the scheduled
deprovisioning.

#### Scenario: Schedule deprovisioning when last membership removed
- **WHEN** an owner removes "alice@example.com" from team "acme" and she has no other memberships
- **THEN** identity provider account deprovisioning is scheduled for "alice@example.com" after the grace period

#### Scenario: Cancel deprovisioning when user regains membership
- **WHEN** "alice@example.com" has been removed from all teams, deprovisioning is scheduled, and she is added to team "beta" within the grace period
- **THEN** the scheduled deprovisioning is cancelled and "alice@example.com" retains her identity provider account

#### Scenario: Deprovision after grace period expires
- **WHEN** "alice@example.com" has been removed from all teams and the grace period has elapsed and the deprovisioning job executes
- **THEN** "alice@example.com"'s identity provider account is removed
