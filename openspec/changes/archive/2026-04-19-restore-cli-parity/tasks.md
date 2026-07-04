## 1. Quarantine broken legacy commands

- [x] 1.1 Add a labeled `<ItemGroup>` to `src/Admitto.Cli/Admitto.Cli.csproj` with `<Compile Remove>` entries for `Commands/Attendee/ExportAttendeesCommand.cs`, `Commands/Events/CreateEventCommand.cs`, `Commands/Team/Member/**/*.cs`, and `Api/ApiClientMemberExtensions.cs`. Inline-comment the missing backend types (`AttendeeDto`, `AdditionalDetailSchemaDto`, `TeamMemberRole`).
- [x] 1.2 Remove the `attendee export`, `event create`, and entire `team member` registrations from `src/Admitto.Cli/Program.cs`, plus their `using` directives. Leave the `attendee`, `event`, and `team` branches intact for the surviving subcommands.
- [x] 1.3 Run `dotnet build src/Admitto.Cli` and confirm the build succeeds against the current (still-stale) `Api/ApiClient.g.cs`.

## 2. Regenerate the API client

- [x] 2.1 Start the Aspire AppHost via the `aspire` skill and wait for the API resource to become healthy.
- [x] 2.2 Run `src/Admitto.Cli/generate-api-client.sh` per the `cli-api-client-generation` skill.
- [x] 2.3 Review the diff of `src/Admitto.Cli/Api/ApiClient.g.cs`; confirm it adds methods for `OpenRegistration`, `CloseRegistration`, `GetRegistrationOpenStatus`, `GetEventEmailSettings`, and `UpsertEventEmailSettings`, and that no hand-edits remain.
- [x] 2.4 Run `dotnet build src/Admitto.Cli` against the regenerated client and confirm it succeeds. If unexpected breakages appear, extend the quarantine `<ItemGroup>` from task 1.1 (and document the additions) rather than hand-editing `ApiClient.g.cs`.

## 3. Add CLI commands for new admin endpoints (UI parity)

- [x] 3.1 Create `src/Admitto.Cli/Commands/Events/Registration/OpenRegistrationCommand.cs` (Spectre `AsyncCommand<TeamEventSettings>`, calls the regenerated `OpenRegistrationAsync` via `IAdmittoService.SendAsync`, returns 0/1 with a success message).
- [x] 3.2 Create `src/Admitto.Cli/Commands/Events/Registration/CloseRegistrationCommand.cs` (mirror of 3.1 for `CloseRegistrationAsync`).
- [x] 3.3 Create `src/Admitto.Cli/Commands/Events/Registration/ShowRegistrationStatusCommand.cs` (uses `QueryAsync`; renders status via `AnsiConsoleExt`).
- [x] 3.4 Create `src/Admitto.Cli/Commands/Events/Email/ShowEventEmailCommand.cs` (uses `QueryAsync`; renders settings as a Spectre `Table`; handles the "no settings yet" 404 path with a clear message).
- [x] 3.5 Create `src/Admitto.Cli/Commands/Events/Email/UpdateEventEmailCommand.cs` with a settings class extending `TeamEventSettings` that exposes individual flags for each field on the regenerated `UpsertEventEmailSettingsRequest`. Validate required fields in `Validate()`.
- [x] 3.6 Wire the five new commands into `Program.cs` under the `event` branch as `event registration {open|close|show}` and `event email {show|update}` sub-branches, matching the description style of existing commands.
- [x] 3.7 Run `dotnet build src/Admitto.Cli` and confirm a clean build.
- [x] 3.8 Smoke-test each new command against the running AppHost (open, show, close, then email show/update) and confirm exit codes and output.

## 4. Documentation

- [x] 4.1 Add a "Quarantining commands when the API surface shrinks" section to `src/Admitto.Cli/AGENTS.md` describing the failure mode (stale `ApiClient.g.cs` masking missing types), pointing at the labeled `<ItemGroup>` in the csproj, and giving the restoration steps.
- [x] 4.2 Cross-reference the regeneration troubleshooting from the existing "NSwag API client" section in `src/Admitto.Cli/AGENTS.md`.

## 5. Close out the upstream blocker

- [x] 5.1 Update `openspec/changes/add-event-management-ui/tasks.md` tasks 9.2, 9.3, and 9.4 to mark them done with a note pointing at this change as the resolution.
- [x] 5.2 Run `openspec validate restore-cli-parity` and confirm it reports no errors.
