## ADDED Requirements

### Requirement: Team owns an accent color for branding
The `Team` aggregate SHALL own an optional or defaulted accent color used for team-scoped branding. The accent color SHALL be returned in team detail and team-list responses used by admin clients. Updating team details SHALL allow a team owner to change the accent color with optimistic concurrency.

#### Scenario: Team has default accent color
- **WHEN** a new team is created without an explicit accent color
- **THEN** the team is created with the system default accent color

#### Scenario: Team owner updates accent color
- **WHEN** an owner of team "acme" updates the accent color to `#0f766e` with the correct expected version
- **THEN** the team stores `#0f766e` and increments its version

#### Scenario: Invalid accent color is rejected
- **WHEN** an owner updates the team accent color to `not-a-color`
- **THEN** the request is rejected with a validation error and the team is unchanged

#### Scenario: Team details include accent color
- **WHEN** a team member retrieves team details
- **THEN** the response includes the team's accent color
