## MODIFIED Requirements

### Requirement: Team owner can update team details
The system SHALL allow team owners to update a team's name and/or email
address as a partial update. The system SHALL NOT allow updating a team's slug —
slugs are immutable after creation. The system SHALL use optimistic concurrency
(expected version) to prevent lost updates.

#### Scenario: Update team details with partial fields
- **WHEN** an owner of team "acme" at version 1 updates the name to "Acme Corp" with expected version 1
- **THEN** the team name is changed to "Acme Corp", slug and email remain unchanged, and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an owner of team "acme" at version 2 submits an update with expected version 1
- **THEN** the request is rejected with a concurrency conflict error and the team is not modified

#### Scenario: Reject update of archived team
- **WHEN** an owner attempts to update the name of an archived team
- **THEN** the request is rejected because the team is archived

## REMOVED Requirements

### Requirement: Team owner can change a team's slug
**Reason**: Slugs are now immutable after creation. They serve as stable URL identifiers and changing them would break bookmarks, external links, and cached references. The `ChangeSlug` method is removed from the `Team` entity.
**Migration**: Remove the `Slug` field from `UpdateTeamCommand`, `UpdateTeamHttpRequest`, and the `UpdateTeamHandler`. Remove the `ChangeSlug()` method from the `Team` entity. Any API clients sending `slug` in the update request should stop doing so.
