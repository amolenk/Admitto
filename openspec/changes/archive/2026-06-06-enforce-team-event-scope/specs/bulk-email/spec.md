## ADDED Requirements

### Requirement: Bulk email operations are scoped to the owning team
All commands and queries that list, retrieve, or cancel a `BulkEmailJob` SHALL verify that the job belongs to the team identified by `teamId` in the route. If the job does not exist or belongs to a different team, the system SHALL return 404.

This applies to: list bulk emails, get bulk email details, cancel bulk email.

#### Scenario: Bulk email list scoped to team
- **WHEN** an organizer of team "team-a" lists bulk emails for an event that belongs to "team-a"
- **THEN** only bulk email jobs for that event and team are returned

#### Scenario: Bulk email operation on job belonging to a different team
- **WHEN** an organizer of team "team-a" attempts to cancel a bulk email job that belongs to "team-b"
- **THEN** the request returns 404
