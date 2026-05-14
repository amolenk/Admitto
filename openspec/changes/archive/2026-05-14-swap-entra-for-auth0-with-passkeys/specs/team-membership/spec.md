## MODIFIED Requirements

### Requirement: New users are provisioned in the identity provider
When a new user is created, the system SHALL asynchronously provision their account
in the configured external identity provider and SHALL request that the provider
require passkey enrollment before first sign-in. The system SHALL NOT set or
transmit a password on the user's behalf.

#### Scenario: Provision identity provider account for new user
- **WHEN** "alice@example.com" is added as a member of team "acme" and no user existed before
- **THEN** a new user is created and an identity provider account is asynchronously provisioned for "alice@example.com" with a passkey-enrollment invitation

#### Scenario: Invitation email links to passkey enrollment
- **WHEN** the identity provider sends the invitation email for "alice@example.com"
- **THEN** the link in the email lands on the identity provider's hosted passkey-enrollment page and, on completion, redirects to the Admin UI

---

### Requirement: Users without team memberships are deprovisioned
When a user's last team membership is removed, the system SHALL schedule identity
provider account deprovisioning after a configurable grace period. If a user regains
a team membership during the grace period, the system SHALL cancel the scheduled
deprovisioning. Deprovisioning SHALL remove the user's account from the configured
identity provider regardless of which provider implementation is active.

#### Scenario: Schedule deprovisioning when last membership removed
- **WHEN** an owner removes "alice@example.com" from team "acme" and she has no other memberships
- **THEN** identity provider account deprovisioning is scheduled for "alice@example.com" after the grace period

#### Scenario: Cancel deprovisioning when user regains membership
- **WHEN** "alice@example.com" has been removed from all teams, deprovisioning is scheduled, and she is added to team "beta" within the grace period
- **THEN** the scheduled deprovisioning is cancelled and "alice@example.com" retains her identity provider account

#### Scenario: Deprovision after grace period expires
- **WHEN** "alice@example.com" has been removed from all teams and the grace period has elapsed and the deprovisioning job executes
- **THEN** "alice@example.com"'s identity provider account is removed from whichever identity provider is currently configured
