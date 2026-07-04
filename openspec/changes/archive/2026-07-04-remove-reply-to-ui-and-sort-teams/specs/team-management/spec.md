## MODIFIED Requirements

### Requirement: Admin can list all active teams
The system SHALL allow admins to list all active teams. Archived teams SHALL be
excluded from listings by default. Teams SHALL be returned in alphabetical order
by name (case-insensitive).

#### Scenario: Admin lists all active teams
- **WHEN** an admin lists all teams and teams "acme" (active), "beta" (active), and "retired" (archived) exist
- **THEN** "acme" and "beta" are returned and "retired" is not included

#### Scenario: Admin team list is ordered alphabetically
- **WHEN** an admin lists all teams and active teams "Zebra Events", "acme", and "Beta Corp" exist
- **THEN** the teams are returned in the order "acme", "Beta Corp", "Zebra Events"

### Requirement: Authenticated users can list their teams
The system SHALL allow authenticated users to list the teams they are a member of.
Archived teams SHALL be excluded. Teams SHALL be returned in alphabetical order
by name (case-insensitive).

#### Scenario: List my teams
- **WHEN** a user who is a member of teams "acme" and "beta" lists their teams and "gamma" exists but they are not a member
- **THEN** "acme" and "beta" are returned and "gamma" is not included

#### Scenario: Archived teams excluded from my teams list
- **WHEN** a user is a member of "acme" (active) and "beta" (archived) and lists their teams
- **THEN** only "acme" is returned

#### Scenario: My teams list is ordered alphabetically
- **WHEN** a user is a member of teams "Zebra Events", "acme", and "Beta Corp" and lists their teams
- **THEN** the teams are returned in the order "acme", "Beta Corp", "Zebra Events"
