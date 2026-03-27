# Feature Specification: Team Membership Management

## 1. Overview

| Field           | Value                                                  |
| --------------- | ------------------------------------------------------ |
| Feature ID      | FEAT-002                                               |
| Status          | Draft                                                  |
| Author          | Copilot + User                                         |
| Created         | 2026-03-26                                             |
| Last updated    | 2026-03-26                                             |
| Epic / Parent   | Organization Module                                    |
| Arc42 reference | 5. Building Block View — Organization module           |

### 1.1 Problem Statement

Team organizers need to manage who belongs to their team and in what capacity.
Currently, members can be added to a team with a role, but there is no way to view
the team roster, adjust a member's role, or remove someone who should no longer have
access. Additionally, when a user loses all team memberships their identity provider
account lingers indefinitely, and when a new user is added their identity provider
account must be provisioned before they can authenticate.

### 1.2 Goal

Team owners can add members, view the full membership roster, change a member's role,
and remove members. When a new user is added, the system provisions their account in
the external identity provider. When a user's last team membership is removed, the
system deprovisions their identity provider account after a configurable grace period,
giving time to reverse accidental removals.

### 1.3 Non-Goals

- Invite flow with email notifications
- Self-service join or leave
- Member activity tracking or audit log
- Bulk membership operations

## 2. User Stories

### US-001: Add a member to a team

**As a** team owner,
**I want** to add someone to my team by their email address and assign them a role,
**so that** they can access team resources according to their responsibilities.

### US-002: List team members

**As a** team owner,
**I want** to see all members of my team and their roles,
**so that** I can manage team composition.

### US-003: Change a member's role

**As a** team owner,
**I want** to change a team member's role,
**so that** their permissions reflect their current responsibilities.

### US-004: Remove a member from a team

**As a** team owner,
**I want** to remove someone from my team,
**so that** they no longer have access to team resources.

### US-005: Provision user in identity provider

**As the** system, **when** a new user is added to a team,
**I want** to provision their account in the external identity provider,
**so that** they can authenticate and access the platform.

### US-006: Deprovision user from identity provider

**As the** system, **when** a user's last team membership is removed,
**I want** to deprovision their identity provider account after a grace period,
**so that** orphaned accounts are cleaned up while allowing time to reverse
accidental removals.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                   | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall allow team owners to add a member by email address and role (Crew, Organizer, or Owner).                                     | Must     | US-001     |
| FR-002 | If no user exists for the given email, the system shall create a new user record.                                                              | Must     | US-001     |
| FR-003 | The system shall reject adding a member who already has a membership in the same team.                                                         | Must     | US-001     |
| FR-004 | The system shall allow team owners to list all members of their team, including each member's email and role.                                  | Must     | US-002     |
| FR-005 | The system shall allow team owners to change a member's role within the team.                                                                  | Must     | US-003     |
| FR-006 | The system shall allow team owners to remove a member from a team.                                                                             | Must     | US-004     |
| FR-007 | When a new user is created, the system shall asynchronously provision their account in the external identity provider.                         | Must     | US-005     |
| FR-008 | When a user's last team membership is removed, the system shall schedule identity provider account deprovisioning after a configurable grace period. | Must     | US-006     |
| FR-009 | If a user regains a team membership during the grace period, the system shall cancel the scheduled deprovisioning.                             | Should   | US-006     |

## 4. Acceptance Scenarios

### SC-001: Add a new user as a team member (FR-001, FR-002)

```gherkin
Given a team "acme" exists
  And no user exists with email "alice@example.com"
  And the requester is an owner of team "acme"
When they add "alice@example.com" as a Crew member
Then a new user is created with email "alice@example.com"
  And the user has a Crew membership in team "acme"
```

### SC-002: Add an existing user as a team member (FR-001)

```gherkin
Given a team "acme" exists
  And a user exists with email "bob@example.com" who is not a member of team "acme"
  And the requester is an owner of team "acme"
When they add "bob@example.com" as an Organizer
Then "bob@example.com" has an Organizer membership in team "acme"
```

### SC-003: Reject adding a duplicate member (FR-003)

```gherkin
Given a team "acme" exists
  And "alice@example.com" is already a Crew member of team "acme"
  And the requester is an owner of team "acme"
When they add "alice@example.com" as a Crew member
Then the request is rejected because the user is already a member of the team
```

### SC-004: List team members (FR-004)

```gherkin
Given a team "acme" exists
  And "alice@example.com" is a Crew member
  And "bob@example.com" is an Owner
  And the requester is an owner of team "acme"
When they list the members of team "acme"
Then the response includes "alice@example.com" with role Crew
  And the response includes "bob@example.com" with role Owner
```

### SC-005: List members of a team with no members (FR-004)

```gherkin
Given a team "acme" exists with no members
  And the requester is an admin
When they list the members of team "acme"
Then an empty list is returned
```

### SC-006: Change a member's role (FR-005)

```gherkin
Given "alice@example.com" is a Crew member of team "acme"
  And the requester is an owner of team "acme"
When they change "alice@example.com"'s role to Organizer
Then "alice@example.com" has an Organizer membership in team "acme"
```

### SC-007: Change role of a non-member (FR-005)

```gherkin
Given "charlie@example.com" is not a member of team "acme"
  And the requester is an owner of team "acme"
When they attempt to change "charlie@example.com"'s role to Crew
Then the request is rejected because the user is not a member of the team
```

### SC-008: Remove a member from a team (FR-006)

```gherkin
Given "alice@example.com" is a Crew member of team "acme"
  And "alice@example.com" is also a member of team "beta"
  And the requester is an owner of team "acme"
When they remove "alice@example.com" from team "acme"
Then "alice@example.com" is no longer a member of team "acme"
  And "alice@example.com" remains a member of team "beta"
```

### SC-009: Remove a non-member (FR-006)

```gherkin
Given "charlie@example.com" is not a member of team "acme"
  And the requester is an owner of team "acme"
When they attempt to remove "charlie@example.com" from team "acme"
Then the request is rejected because the user is not a member of the team
```

### SC-010: Provision identity provider account for new user (FR-007)

```gherkin
Given no user exists with email "alice@example.com"
When "alice@example.com" is added as a member of team "acme"
Then a new user is created
  And an identity provider account is asynchronously provisioned for "alice@example.com"
```

### SC-011: Schedule deprovisioning when last membership removed (FR-008)

```gherkin
Given "alice@example.com" is a member of team "acme" only (no other memberships)
  And the requester is an owner of team "acme"
When they remove "alice@example.com" from team "acme"
Then identity provider account deprovisioning is scheduled for "alice@example.com"
  after the grace period
```

### SC-012: Cancel deprovisioning when user regains membership (FR-009)

```gherkin
Given "alice@example.com" has been removed from all teams
  And deprovisioning is scheduled within the grace period
When "alice@example.com" is added as a member of team "beta"
Then the scheduled deprovisioning is cancelled
  And "alice@example.com" retains their identity provider account
```

### SC-013: Deprovision after grace period expires (FR-008)

```gherkin
Given "alice@example.com" has been removed from all teams
  And the grace period has elapsed
When the deprovisioning job executes
Then "alice@example.com"'s identity provider account is removed
```

### SC-014: Non-owner cannot manage members (FR-001)

```gherkin
Given the requester is a Crew member of team "acme"
When they attempt to add a new member to team "acme"
Then the request is rejected as unauthorized
```

### SC-015: Admin bypasses ownership check (FR-001)

```gherkin
Given the requester is an admin but not a member of team "acme"
When they add "alice@example.com" as a Crew member of team "acme"
Then "alice@example.com" has a Crew membership in team "acme"
```

## 5. Domain Model

### 5.1 Entities

#### User (Aggregate Root) — _exists, needs extension_

_Represents a person who can be a member of one or more teams. Owns the membership
collection and the identity provider lifecycle._

| Attribute        | Type                        | Constraints                   | Description                                                         | Status    |
| ---------------- | --------------------------- | ----------------------------- | ------------------------------------------------------------------- | --------- |
| id               | UserId (UUID)               | PK, generated at creation     | Unique identity                                                     | ✅ Exists |
| emailAddress     | EmailAddress                | required, unique              | User's email, used as the identity key                              | ✅ Exists |
| externalUserId   | ExternalUserId              | nullable                      | Link to external identity provider account                          | ✅ Exists |
| memberships      | list of TeamMembership      | owned collection, JSON        | All team memberships for this user                                  | ✅ Exists |
| deprovisionAfter | datetime                    | nullable                      | When set, the IdP account is scheduled for removal after this time  | 🆕 New    |
| version          | uint                        | auto-incremented              | Optimistic concurrency token                                        | ✅ Exists |

#### TeamMembership (Owned Entity) — _exists_

_A single team membership within a User aggregate._

| Attribute | Type               | Constraints        | Description                              | Status    |
| --------- | ------------------ | ------------------ | ---------------------------------------- | --------- |
| id        | TeamId (UUID)      | FK to Team         | The team this membership belongs to      | ✅ Exists |
| role      | TeamMembershipRole | required, enum     | Crew / Organizer / Owner                 | ✅ Exists |

### 5.2 Relationships

- A **User** has zero or more **TeamMemberships** (one-to-many, owned)
- A **TeamMembership** references exactly one **Team** (by TeamId)
- A **User** has at most one external identity provider account (via externalUserId)

### 5.3 Value Objects

_All defined in the shared kernel (`Admitto.Module.Shared.Kernel`)._

| Value Object       | Attributes | Constraints                          |
| ------------------ | ---------- | ------------------------------------ |
| UserId             | GUID       | Not empty                            |
| EmailAddress       | string     | Valid email format, max length       |
| ExternalUserId     | string     | Identity provider–specific identifier|
| TeamMembershipRole | enum       | Crew, Organizer, Owner               |

### 5.4 Domain Rules and Invariants

- **One membership per team**: A user can have at most one membership per team.
  Adding a duplicate is rejected.
- **Role is mutable**: A membership's role can be changed independently of other
  operations.
- **Removal triggers deprovisioning check**: When a membership is removed, if the
  user has no remaining memberships, `deprovisionAfter` is set to `now + grace period`.
- **Re-membership cancels deprovisioning**: When a membership is added to a user
  with a pending `deprovisionAfter`, the deprovisioning is cancelled (field set
  to null).
- **Deprovisioning is final**: Once the grace period expires and the deprovisioning
  job executes, the user's identity provider account is removed. If the user has
  regained memberships by that time, the job is a no-op.

### 5.5 Domain Events

| Event                             | Trigger                        | Purpose                           | Status    |
| --------------------------------- | ------------------------------ | --------------------------------- | --------- |
| UserCreatedDomainEvent            | User.Create()                  | Triggers IdP provisioning         | ✅ Exists |
| TeamMembershipRemovedDomainEvent  | User.RemoveTeamMembership()    | Triggers deprovisioning check     | 🆕 New    |

_IdP provisioning and deprovisioning are asynchronous workflows triggered via the
module event / outbox pattern per arc42 Section 8._

## 6. Non-Functional Requirements

_Project-wide NFRs (JWT authentication, observability, error response format) apply
per arc42 Section 10 and are not repeated here._

| ID      | Category        | Requirement                                                                                                  |
| ------- | --------------- | ------------------------------------------------------------------------------------------------------------ |
| NFR-001 | Security        | Only team owners (or admins) may add, remove, or change roles of team members.                               |
| NFR-002 | Security        | The membership list is only visible to team owners (or admins).                                              |
| NFR-003 | Reliability     | Identity provider provisioning must be retried on transient failures; a failed provision must not block the membership operation. |
| NFR-004 | Reliability     | Identity provider deprovisioning must be idempotent — safe to retry without side effects.                    |
| NFR-005 | Configurability | The deprovisioning grace period must be configurable (default: 7 days).                                      |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                     | Expected Behavior                                                            |
| ----- | ------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| EC-1  | Add a member who already belongs to the team                 | Return domain error; membership unchanged                                    |
| EC-2  | Change role of a non-member                                  | Return 404 or domain error                                                   |
| EC-3  | Remove a non-member                                          | Return 404 or domain error                                                   |
| EC-4  | Remove last membership; user has no other teams              | Schedule deprovisioning after grace period                                   |
| EC-5  | Add membership during deprovisioning grace period            | Cancel scheduled deprovisioning                                              |
| EC-6  | Deprovisioning job fires but user has regained membership    | Job is a no-op; user and IdP account remain                                  |
| EC-7  | Identity provider is unavailable during provisioning         | Retry asynchronously; membership is still persisted                          |
| EC-8  | Identity provider is unavailable during deprovisioning       | Retry asynchronously; deprovisioning is rescheduled                          |
| EC-9  | Owner removes themselves from the team                       | Allowed; team may have no owners (admin can still manage)                    |
| EC-10 | Add member to an archived team                               | Return domain error; team is archived (per FEAT-001)                         |
| EC-11 | Crew or Organizer attempts to manage members                 | Return 403 Forbidden                                                         |

## 8. Success Criteria

| ID     | Criterion                                                                                     |
| ------ | --------------------------------------------------------------------------------------------- |
| SC-001 | All 15 acceptance scenarios pass in CI.                                                       |
| SC-002 | Membership CRUD operations enforce owner-only authorization with admin bypass.                 |
| SC-003 | New user creation triggers asynchronous IdP provisioning.                                     |
| SC-004 | Removing a user's last membership schedules deprovisioning after the configured grace period.  |
| SC-005 | Re-adding a membership during the grace period cancels scheduled deprovisioning.              |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **FEAT-001 Team Management**: Archived team guard (EC-10) requires team archive
  status to be checked when adding members.
- **External identity provider**: Keycloak (local dev) / Microsoft Entra External ID
  (production) for provisioning and deprovisioning.
- **Outbox + queue infrastructure**: For reliable async dispatch of provisioning and
  deprovisioning events.
- **Quartz or scheduled job mechanism**: For executing deprovisioning after the grace
  period expires.

### 9.2 Constraints

- Membership operations are scoped to the User aggregate in the Organization module.
- Cross-module code must not access the Organization DbContext directly; use the
  `IOrganizationFacade` contract.
- Identity provider operations are asynchronous and must not block API responses.

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                        |
| -------------------------------- | -------------------------------------------------------------------------------- |
| 3. Context & Scope               | Admin API boundary; identity provider as external system                         |
| 5. Building Block View           | Organization module owns users and memberships                                   |
| 6. Runtime View                  | Endpoint → handler → write store flow; async event processing for IdP operations |
| 8. Crosscutting Concepts         | Auth (owner-only + admin bypass), messaging (domain → module events + outbox), validation (FluentValidation), error handling (BusinessRuleViolationException) |
| 9. Architecture Decisions (ADRs) | ADR-001 (modular monolith), ADR-002 (feature-sliced endpoints)                   |
| 10. Quality Requirements         | Reliability (IdP retries), security (role-based access)                          |

## 10. Open Questions

_No open questions. All decisions resolved during specification._

| #   | Question                                               | Owner | Status   | Resolution                                                |
| --- | ------------------------------------------------------ | ----- | -------- | --------------------------------------------------------- |
| 1   | Who can manage members — Organizers or Owners only?    | User  | Resolved | Owners only (admins bypass)                               |
| 2   | Must there always be at least one owner?               | User  | Resolved | No — if no owners remain, the admin can manage the team   |
| 3   | Is identity provider account creation in scope?        | User  | Resolved | Yes — provisioning is part of this feature                |
| 4   | What is a reasonable default grace period?             | User  | Resolved | 7 days                                                    |
