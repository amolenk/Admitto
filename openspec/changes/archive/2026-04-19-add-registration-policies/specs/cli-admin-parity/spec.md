## ADDED Requirements

### Requirement: CLI exposes cancellation policy management

The Admitto CLI SHALL expose commands to view, set (create or update), and remove the cancellation policy for an event. Each command SHALL invoke the corresponding admin endpoint via the regenerated `ApiClient` and SHALL contain no business logic beyond input mapping, slug resolution, and output formatting.

#### Scenario: Show cancellation policy
- **WHEN** an operator runs `admitto event cancellation-policy show -t <team> -e <event>`
- **THEN** the CLI SHALL call `GET /admin/teams/{team}/events/{event}/cancellation-policy` via `ApiClient`
- **AND** SHALL print the configured late-cancellation cutoff or indicate that no policy is configured

#### Scenario: Set cancellation policy
- **WHEN** an operator runs `admitto event cancellation-policy set -t <team> -e <event> --late-cutoff <iso-datetime>`
- **THEN** the CLI SHALL call `PUT /admin/teams/{team}/events/{event}/cancellation-policy` via `ApiClient`
- **AND** SHALL exit `0` on success or `1` on a non-success response

#### Scenario: Remove cancellation policy
- **WHEN** an operator runs `admitto event cancellation-policy remove -t <team> -e <event>`
- **THEN** the CLI SHALL call `DELETE /admin/teams/{team}/events/{event}/cancellation-policy` via `ApiClient`

---

### Requirement: CLI exposes reconfirm policy management

The Admitto CLI SHALL expose commands to view, set, and remove the reconfirm policy for an event.

#### Scenario: Show reconfirm policy
- **WHEN** an operator runs `admitto event reconfirm-policy show -t <team> -e <event>`
- **THEN** the CLI SHALL call `GET /admin/teams/{team}/events/{event}/reconfirm-policy` via `ApiClient`
- **AND** SHALL print the configured window and cadence or indicate that no policy is configured

#### Scenario: Set reconfirm policy
- **WHEN** an operator runs `admitto event reconfirm-policy set -t <team> -e <event> --opens-at <iso> --closes-at <iso> --cadence-days <n>`
- **THEN** the CLI SHALL call `PUT /admin/teams/{team}/events/{event}/reconfirm-policy` via `ApiClient`

#### Scenario: Remove reconfirm policy
- **WHEN** an operator runs `admitto event reconfirm-policy remove -t <team> -e <event>`
- **THEN** the CLI SHALL call `DELETE /admin/teams/{team}/events/{event}/reconfirm-policy` via `ApiClient`

## REMOVED Requirements

### Requirement: Event registration open/close commands
**Reason**: The admin endpoint `POST /admin/teams/{team}/events/{event}/registration/open` (and any corresponding close action) is removed by this change: registration openness is now derived from the configured window and the lifecycle guard, with no stored registration-status to toggle.
**Migration**:
- Delete the Spectre.Console command backing `admitto event registration open`.
- The `admitto event registration show` command is retained (now calling a read-only open-status query derived from the window and the lifecycle guard).
- New cancellation-policy and reconfirm-policy commands are added per the ADDED requirements above.

## MODIFIED Requirements

### Requirement: CLI mirrors every admin API endpoint

The Admitto CLI (`src/Admitto.Cli`) SHALL expose a Spectre.Console.Cli command for every admin HTTP endpoint exposed by `Admitto.Api`. The command SHALL invoke that endpoint via the NSwag-generated `ApiClient` and SHALL NOT contain business logic beyond input mapping, slug resolution, and output formatting.

#### Scenario: New admin endpoint added to the API
- **WHEN** a new admin endpoint is added under `/admin/...` in any module
- **THEN** the same change SHALL add or update a CLI command under the matching feature branch in `Program.cs`
- **AND** the command SHALL call the endpoint through a method on the regenerated `ApiClient`

#### Scenario: Event registration status
- **WHEN** an operator runs `admitto event registration show -t <team> -e <event>`
- **THEN** the CLI SHALL call `GET /admin/teams/{team}/events/{event}/registration/open-status` via `ApiClient`
- **AND** SHALL print whether registration is currently open (derived from the configured window and the event's lifecycle status)

#### Scenario: Event email settings
- **WHEN** an operator runs `admitto event email show -t <team> -e <event>` or `admitto event email update -t <team> -e <event> [...]`
- **THEN** the CLI SHALL call the corresponding `GET`/`PUT` `/admin/teams/{team}/events/{event}/email-settings` endpoints via `ApiClient`
