## ADDED Requirements

### Requirement: CLI exposes a send-test-email command for both scopes

The Admitto CLI SHALL expose a `email settings test` command that mirrors the new send-test-email admin endpoint at both team and event scope, calling the endpoint via the regenerated `ApiClient`. The command SHALL accept the team slug, an optional event slug, and a required recipient address. When the event slug is omitted the command SHALL invoke the team-scoped endpoint; when present it SHALL invoke the event-scoped endpoint. The command SHALL print a success message that includes the recipient on success, and SHALL print a non-zero-exit error message containing the server's error text on failure.

#### Scenario: Team-scoped test via CLI
- **WHEN** an operator runs `admitto email settings test --team acme --recipient ops@acme.org`
- **THEN** the CLI calls `POST /admin/teams/acme/email-settings/test` via `ApiClient` with `{"recipient": "ops@acme.org"}`
- **AND** prints a success message identifying the recipient on a `200` response

#### Scenario: Event-scoped test via CLI
- **WHEN** an operator runs `admitto email settings test --team acme --event devconf-2026 --recipient ops@acme.org`
- **THEN** the CLI calls `POST /admin/teams/acme/events/devconf-2026/email-settings/test` via `ApiClient` with `{"recipient": "ops@acme.org"}`

#### Scenario: Server error is surfaced to the operator
- **WHEN** the API responds with a business-rule error (e.g. "Failed to send test email: Authentication failed")
- **THEN** the CLI exits with a non-zero status
- **AND** prints an error message containing the server-supplied text
