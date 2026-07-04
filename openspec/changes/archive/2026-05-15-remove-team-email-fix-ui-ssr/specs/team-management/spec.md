## MODIFIED Requirements

### Requirement: Admin can create a team
The system SHALL allow admins to create a team with a name.
The name SHALL conform to its domain value object constraints.
A `TeamId` (UUID) is assigned by the system on creation.

#### Scenario: Successfully create a team
- **WHEN** an authenticated admin creates a team with name "Acme Events"
- **THEN** the team is created with the provided name, is in an active state, and a `TeamId` UUID is returned

#### Scenario: Reject invalid input on create
- **WHEN** an admin creates a team with an empty name
- **THEN** the request is rejected with a validation error indicating the name is required

---

### Requirement: Team member can view team details
The system SHALL allow team members with Crew role or above to retrieve a team's
details by team ID.

#### Scenario: View team details by ID
- **WHEN** a user with Crew role requests the details of team with ID "11111111-0000-0000-0000-000000000001"
- **THEN** the team's ID, name, and version are returned

#### Scenario: Reject unauthorized team view
- **WHEN** a user who is not a member of the requested team requests its details
- **THEN** the request is rejected as unauthorized

---

### Requirement: Team owner can update team details
The system SHALL allow team owners to update a team's name as a partial update.
The system SHALL use optimistic concurrency (expected version) to prevent lost updates.

#### Scenario: Update team details with partial fields
- **WHEN** an owner of team "Acme Events" at version 1 updates the name to "Acme Corp" with expected version 1
- **THEN** the team name is changed to "Acme Corp" and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an owner of team "acme" at version 2 submits an update with expected version 1
- **THEN** the request is rejected with a concurrency conflict error and the team is not modified

#### Scenario: Reject update of archived team
- **WHEN** an owner attempts to update the name of an archived team
- **THEN** the request is rejected because the team is archived

## REMOVED Requirements

### Requirement: (email validation on create)
**Reason**: The team email address field has been removed from the domain model. Email sending uses the SMTP settings from the email-settings capability, not a per-team address.
**Migration**: Remove the `email` field from `POST /teams` requests. The field will be ignored or rejected.
