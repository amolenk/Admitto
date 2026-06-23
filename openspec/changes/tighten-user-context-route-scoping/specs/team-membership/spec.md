## ADDED Requirements

### Requirement: Team membership authorization uses the requested team role
The system SHALL evaluate team membership authorization using the authenticated user's membership role for the requested route team only. Membership in another team SHALL NOT satisfy authorization for the requested team. Admin users SHALL continue to bypass team membership checks.

#### Scenario: Membership in requested team authorizes access
- **WHEN** a user has the required role in the route team and accesses a team-scoped admin endpoint for that team
- **THEN** team membership authorization succeeds

#### Scenario: Membership in another team does not authorize access
- **WHEN** a user has the required role in another team but no sufficient role in the route team
- **THEN** team membership authorization fails for the route team

#### Scenario: Admin user bypasses requested team role check
- **WHEN** an admin user has no membership in the route team and accesses a team-scoped admin endpoint
- **THEN** team membership authorization succeeds
