# Capability: CLI Admin Parity

## Purpose

Ensure the Admitto CLI (`src/Admitto.Cli`) exposes a command for every admin HTTP endpoint in the API, using a generated NSwag `ApiClient` as the sole HTTP boundary. Establishes a quarantine policy for commands temporarily broken by client regeneration, and documents the regeneration workflow for contributors.

## Requirements

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

### Requirement: Generated API client is the single HTTP boundary

`src/Admitto.Cli/Api/ApiClient.g.cs` SHALL be produced exclusively by `src/Admitto.Cli/generate-api-client.sh` (which invokes NSwag against the live API's OpenAPI document). Hand-edits to `ApiClient.g.cs` SHALL NOT be committed.

#### Scenario: Regeneration produces a buildable CLI
- **WHEN** a contributor follows the `cli-api-client-generation` skill (start AppHost, run `generate-api-client.sh`)
- **THEN** the regenerated `ApiClient.g.cs` SHALL be committed as-is
- **AND** `dotnet build src/Admitto.Cli` SHALL succeed without further hand-edits to generated or non-generated CLI sources

#### Scenario: Regenerated client drops a previously-exposed type
- **WHEN** regeneration produces a client that no longer defines a type referenced by an existing CLI command
- **THEN** the affected command files SHALL be quarantined per the CLI quarantine policy (see next requirement)
- **AND** the build SHALL succeed after quarantine without modifying `ApiClient.g.cs`

### Requirement: Temporarily-unsupported commands are quarantined, not deleted

When the regenerated `ApiClient` no longer exposes types or operations a CLI command depends on, the affected command source files SHALL be excluded from compilation via `<Compile Remove>` entries in `src/Admitto.Cli/Admitto.Cli.csproj`, and their registrations SHALL be removed from `Program.cs`. The source files SHALL remain in the repository.

The csproj `<ItemGroup>` containing the exclusions SHALL carry a `Label` attribute and an inline comment naming the missing backend types or endpoints, so re-enablement requires only deleting the exclusions once the backend surface returns.

#### Scenario: Quarantined files are excluded from build but kept on disk
- **WHEN** a CLI source file is quarantined
- **THEN** the file SHALL still exist under `src/Admitto.Cli/`
- **AND** the file SHALL NOT be compiled into the CLI assembly
- **AND** `Program.cs` SHALL contain no `AddCommand<>` registration that targets a type defined in the quarantined file

#### Scenario: Restoring a quarantined command
- **WHEN** the backend re-exposes the missing types or endpoints and the client is regenerated
- **THEN** removing the corresponding `<Compile Remove>` line and re-adding the `Program.cs` registration SHALL be sufficient to restore the command
- **AND** no other source edits SHALL be required beyond fixing any signature drift introduced by the regeneration

### Requirement: Regeneration troubleshooting is documented

`src/Admitto.Cli/AGENTS.md` SHALL document the regeneration workflow's known failure mode in which a stale committed `ApiClient.g.cs` masks references to removed backend types, and SHALL point contributors at the quarantine `<ItemGroup>` in the csproj as the resolution mechanism.

#### Scenario: Contributor hits the stale-client failure
- **WHEN** a contributor runs `generate-api-client.sh` and the CLI fails to build with errors about missing DTO/enum types
- **THEN** `src/Admitto.Cli/AGENTS.md` SHALL describe this exact symptom
- **AND** SHALL instruct the contributor to either restore the missing endpoints in the API or add the affected files to the quarantine `<ItemGroup>`

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
