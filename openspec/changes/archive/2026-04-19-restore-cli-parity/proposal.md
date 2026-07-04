## Why

`Admitto.Cli` is meant to be a thin, fully-featured admin client for the API, regenerated via NSwag whenever the backend surface changes (`src/Admitto.Cli/AGENTS.md`, `cli-api-client-generation` skill). Today it is broken in two ways:

1. **Regeneration is effectively blocked.** The committed `ApiClient.g.cs` is stale. Running `generate-api-client.sh` produces a client matching the current API, but several pre-existing commands (`ExportAttendeesCommand`, `CreateEventCommand`, `Commands/Team/Member/*`, `Api/ApiClientMemberExtensions.cs`) reference types the API no longer exposes (`AttendeeDto`, `AdditionalDetailSchemaDto`, `TeamMemberRole`). The CLI only compiles because the stale generated client still defines those types — so any contract-driven backend change silently breaks regeneration. This is documented as the blocker on tasks 9.2–9.4 of `add-event-management-ui`.
2. **CLI lags behind the admin UI.** The `add-event-management-ui` change adds admin endpoints for opening/closing registration, getting/updating per-event email settings, and updating event general settings. The CLI has no commands for these even though repo convention (`AGENTS.md` Feature Implementation Checklist) requires every admin endpoint to have a matching CLI command.

We want to keep the CLI and maintain UI parity, so we need a focused cleanup pass: unblock regeneration, add the missing admin commands, and quarantine the unsupported legacy commands until their backend surfaces return.

## What Changes

- **Quarantine broken legacy commands** by commenting out their registration in `Program.cs` and excluding their source files from compilation (or wrapping their bodies in `#if LEGACY_CLI` / `#error` stubs) so regeneration no longer fails:
  - `Commands/Attendee/ExportAttendeesCommand.cs` (depends on removed `AttendeeDto` / `AdditionalDetailSchemaDto`)
  - `Commands/Events/CreateEventCommand.cs` (depends on removed types — note: event creation via CLI returns when backend exposes it again)
  - `Commands/Team/Member/*` (depends on removed `TeamMemberRole`)
  - `Api/ApiClientMemberExtensions.cs`
  - Each quarantined command keeps a `// TODO(restore-cli-parity):` comment with the missing backend type so re-enablement is mechanical.
- **Regenerate `Admitto.Cli/Api/ApiClient.g.cs`** against the current API via the `cli-api-client-generation` skill so the committed client matches the live OpenAPI contract.
- **Verify the regeneration workflow** by adding a short troubleshooting note to `src/Admitto.Cli/AGENTS.md` covering the "stale generated client masking compile errors" failure mode and how to re-run the skill.
- **Add CLI commands for new admin endpoints introduced by `add-event-management-ui`**, restoring UI parity:
  - `event registration open` / `event registration close` / `event registration show` (Registrations admin endpoints)
  - `event email show` / `event email update` (Email module admin endpoints)
  - Any gaps in `event update` for general-settings fields the UI exposes (name/website/baseUrl/start/end already covered per task 9.1; verify and extend if needed).
- **No** changes to API behaviour, contracts, or modules. This change is CLI-only plus one docs update.

## Capabilities

### New Capabilities
- `cli-admin-parity`: The Admitto CLI mirrors every admin API endpoint with a corresponding command, uses the NSwag-generated `ApiClient` exclusively for HTTP calls, and stays regenerable via the documented `cli-api-client-generation` workflow. This capability also defines how the CLI handles temporarily-removed backend surfaces (quarantine, not delete).

### Modified Capabilities
_None._ This change does not alter behaviour of any existing spec capability; it brings the CLI into compliance with the existing admin endpoint surface.

## Impact

- **Code**: `src/Admitto.Cli/` — quarantine ~5 files, regenerate `Api/ApiClient.g.cs`, add ~5 new command files for registration + email settings, update `Program.cs` command tree, update `src/Admitto.Cli/AGENTS.md`.
- **Backend**: none.
- **Database**: none.
- **Docs**: `src/Admitto.Cli/AGENTS.md` regeneration troubleshooting section.
- **Other changes**: unblocks tasks 9.2–9.4 of `add-event-management-ui` (which can then be marked done or removed in favour of the work tracked here).
