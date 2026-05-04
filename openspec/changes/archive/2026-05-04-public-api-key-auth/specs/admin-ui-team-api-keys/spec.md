## ADDED Requirements

### Requirement: Team settings navigation includes an API Keys entry
The Admin UI team settings sidebar SHALL include an "API Keys" navigation entry that links to `/teams/{teamSlug}/settings/api-keys`. It SHALL appear alongside the existing General, Members, Email, and Danger Zone entries.

#### Scenario: SC101 - API Keys nav entry is visible in team settings
- **WHEN** a team member navigates to any page under `/teams/{teamSlug}/settings`
- **THEN** the sidebar shows an "API Keys" entry

#### Scenario: SC102 - Clicking API Keys nav entry navigates to the page
- **WHEN** a team member clicks the "API Keys" entry in the team settings sidebar
- **THEN** the browser navigates to `/teams/{teamSlug}/settings/api-keys`

---

### Requirement: Team member can view API keys list
The Admin UI SHALL provide a page at `/teams/{teamSlug}/settings/api-keys` that lists all API keys for the team (active and revoked). Each row SHALL display the key name, the key prefix, the creation date, the creator, and the revocation date (or an "Active" badge if not revoked).

#### Scenario: SC103 - List shows all keys for the team
- **WHEN** a team member navigates to `/teams/{teamSlug}/settings/api-keys`
- **THEN** the page displays a table of all API keys with name, prefix, created date, creator, and status

#### Scenario: SC104 - Active key shows Active badge; revoked key shows revoked date
- **WHEN** a team member views a list that includes one active and one revoked key
- **THEN** the active key row shows an "Active" badge and the revoked row shows the revocation date

#### Scenario: SC105 - Empty state when no keys exist
- **WHEN** a team member navigates to the API keys page and the team has no API keys
- **THEN** the page shows an empty state message encouraging the user to create the first key

---

### Requirement: Team member can create an API key via the UI
The Admin UI SHALL provide a "Create API Key" action on the API keys page. A form (in a dialog or inline) SHALL collect a key name. On success, the UI SHALL display the full raw key value exactly once in a prominent dialog with a copy-to-clipboard button and a warning that the key will not be shown again. After dismissing the dialog, the new key SHALL appear in the list in active state (raw value not shown, prefix visible).

#### Scenario: SC106 - Successful key creation shows raw key once
- **WHEN** a team member submits the create API key form with name "Production"
- **THEN** a dialog appears showing the full raw key with a copy button and a warning that it cannot be retrieved again

#### Scenario: SC107 - Dismissing the dialog adds the key to the list
- **WHEN** a team member dismisses the one-time key display dialog
- **THEN** the new key appears in the list as active with only the prefix visible

#### Scenario: SC108 - Validation error when name is empty
- **WHEN** a team member submits the create API key form without a name
- **THEN** the form displays a validation error and does not submit

---

### Requirement: Team member can revoke an API key via the UI
The Admin UI SHALL provide a "Revoke" action for each active API key. A confirmation dialog SHALL warn the user that revoking the key will immediately prevent any event website using it from calling the API. On confirmation, the key SHALL be revoked and the list SHALL update to show the revoked status.

#### Scenario: SC109 - Revoke action is available only for active keys
- **WHEN** a team member views the API keys list
- **THEN** active keys have a "Revoke" action and revoked keys do not

#### Scenario: SC110 - Confirmation dialog appears before revoking
- **WHEN** a team member clicks "Revoke" on an active key
- **THEN** a confirmation dialog appears warning that the key will stop working immediately

#### Scenario: SC111 - Confirmed revocation updates the list
- **WHEN** a team member confirms the revocation in the dialog
- **THEN** the key's status in the list changes to revoked and the "Revoke" action disappears
