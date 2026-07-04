## Context

`Admitto.Cli` is a Spectre.Console.Cli app that talks to the Admitto API exclusively through an NSwag-generated `ApiClient.g.cs`. The generation workflow is documented in `src/Admitto.Cli/AGENTS.md` and the `cli-api-client-generation` skill: start the Aspire AppHost, run `src/Admitto.Cli/generate-api-client.sh`, build the project.

That workflow is currently broken end-to-end:

- The committed `Api/ApiClient.g.cs` is stale relative to the live API. It still defines `AttendeeDto`, `AdditionalDetailSchemaDto`, and `TeamMemberRole` — types the API no longer exposes after recent module refactors.
- Several CLI files reference those types directly:
  - `Commands/Attendee/ExportAttendeesCommand.cs`
  - `Commands/Events/CreateEventCommand.cs`
  - `Commands/Team/Member/AddTeamMemberCommand.cs`
  - `Commands/Team/Member/UpdateTeamMemberCommand.cs`
  - `Commands/Team/Member/TeamMemberRoleDescriptionAttribute.cs`
  - `Api/ApiClientMemberExtensions.cs`
- Re-running NSwag against the current API produces a client that no longer defines those types, so the CLI fails to compile and the developer is forced to revert the regeneration. As a result, the committed client drifts further from the API every time the backend changes.

In parallel, the `add-event-management-ui` change has added admin endpoints the CLI does not yet wrap:

- `POST /admin/.../registration/open`
- `POST /admin/.../registration/close`
- `GET  /admin/.../registration/open-status`
- `GET  /admin/.../email-settings` (Email module)
- `PUT  /admin/.../email-settings` (Email module)

Repo convention (`AGENTS.md` Feature Implementation Checklist) requires every admin endpoint to have a matching CLI command.

## Goals / Non-Goals

**Goals:**
- Make `generate-api-client.sh` reliably produce a CLI that builds against the current API.
- Add CLI commands for the new event-registration and event-email admin endpoints introduced by `add-event-management-ui`.
- Establish a documented "quarantine" pattern for CLI commands whose backend surface has temporarily disappeared, so future contract changes don't reintroduce the same blocker.
- Keep the CLI as the canonical headless admin interface alongside the admin UI.

**Non-Goals:**
- Restoring the legacy attendee export, event creation, or team-member management endpoints. Those are backend-side decisions tracked separately; this change only quarantines the CLI side.
- Refactoring the CLI service abstraction (`IAdmittoService`), command pattern, or auth/token handling.
- Changing any API behaviour, contracts, DTOs, or database schema.
- Updating the HeyAPI TypeScript SDK (already covered by `add-event-management-ui` task 10.3).

## Decisions

### 1. Quarantine via `<Compile Remove>` in the csproj, not `#if` or commented code

We exclude broken files from compilation by adding a single `<ItemGroup>` to `src/Admitto.Cli/Admitto.Cli.csproj`:

```xml
<ItemGroup Label="Quarantined: backend admin endpoints removed; restore once the API exposes attendee / contributor / email-template / team-member surfaces again. See AGENTS.md > Quarantining commands when the API surface shrinks.">
  <Compile Remove="Commands/Attendee/**/*.cs" />
  <Compile Remove="Commands/Contributor/**/*.cs" />
  <Compile Remove="Commands/Email/Bulk/**/*.cs" />
  <Compile Remove="Commands/Email/RecipientLists/**/*.cs" />
  <Compile Remove="Commands/Email/Template/**/*.cs" />
  <Compile Remove="Commands/Email/Verification/**/*.cs" />
  <Compile Remove="Commands/Email/SendReconfirmEmailCommand.cs" />
  <Compile Remove="Commands/Email/TestEmailCommand.cs" />
  <Compile Remove="Commands/Events/CreateEventCommand.cs" />
  <Compile Remove="Commands/Events/Policy/Reconfirm/**/*.cs" />
  <Compile Remove="Commands/Team/Member/**/*.cs" />
  <Compile Remove="Api/ApiClientMemberExtensions.cs" />
</ItemGroup>
```

We also remove the corresponding `AddCommand<>` / `AddBranch` registrations from `Program.cs` (and their `using` directives) so the binary has no dangling references.

**Scope expanded during implementation.** The proposal originally anticipated quarantining only `ExportAttendeesCommand`, `CreateEventCommand`, `Commands/Team/Member/**`, and `ApiClientMemberExtensions.cs`. Once `ApiClient.g.cs` was regenerated against the current backend, the dead surface turned out to be much broader: virtually all of `attendee`, `contributor`, `email/bulk`, `email/recipient-lists`, `email/template`, `email/verification`, the standalone `SendReconfirmEmailCommand` / `TestEmailCommand`, and `Commands/Events/Policy/Reconfirm/**` (no `/registration-policy`-equivalent endpoint exists for reconfirm policy) all lost their backing endpoints. We extended the `<ItemGroup>` rather than partially re-implementing those commands, on the rationale already established here ("Files stay on disk; restoration is a one-line revert once the backend exposes the endpoints again"). `IO/InputHelper.cs` lost its `ParseAdditionalDetails` and `ParseTickets` helpers, and `IAdmittoService.FindAttendeeAsync` was commented out; both were used only by quarantined commands.

A subtle issue surfaced during the quarantine: when `InputHelper.cs` had unresolved types, the C# compiler suppressed downstream errors in every file that transitively referenced `InputHelper.Parse*`. The initial post-regeneration build reported only a handful of errors; after fixing `InputHelper`, the true cascade (~50 errors across ~20 files) appeared. The AGENTS.md addendum documents this trap.

**Alternatives considered:**
- *`#if LEGACY_CLI` guards inside each file* — pollutes every file, and IDEs flag the dead code as unreachable. Rejected.
- *Commenting out file contents* — easy to merge-rot; loses syntax highlighting and refactor safety. Rejected.
- *Deleting the files* — loses the documented intent. Restoration becomes archaeology rather than `git revert`. Rejected.

The csproj exclusion keeps the files on disk (so `git log --follow` works and re-enabling is a one-line revert), keeps them out of the build, and gives one obvious place for future contributors to see what's quarantined and why.

### 2. New commands follow the existing `event <noun> <verb>` pattern

The new admin endpoints are scoped to a team+event. We add them under the existing `event` branch in `Program.cs`:

```
event registration open       -> POST   /admin/.../registration/open
event registration close      -> POST   /admin/.../registration/close
event registration show       -> GET    /admin/.../registration/open-status
event email show              -> GET    /admin/.../email-settings
event email update            -> PUT    /admin/.../email-settings
```

Files live at:
- `src/Admitto.Cli/Commands/Events/Registration/OpenRegistrationCommand.cs`
- `src/Admitto.Cli/Commands/Events/Registration/CloseRegistrationCommand.cs`
- `src/Admitto.Cli/Commands/Events/Registration/ShowRegistrationStatusCommand.cs`
- `src/Admitto.Cli/Commands/Events/Email/ShowEventEmailCommand.cs`
- `src/Admitto.Cli/Commands/Events/Email/UpdateEventEmailCommand.cs`

All five inherit from `AsyncCommand<TeamEventSettings>` (or a small subclass for `update`'s extra options), use `IAdmittoService.SendAsync`/`QueryAsync`, and resolve slugs via `InputHelper`.

**Alternatives considered:**
- *Top-level `registration` / `email` branches* — breaks the "everything event-scoped lives under `event`" mental model already established by `event policy`, `event ticket-type`, etc. Rejected.

### 3. Regeneration verification is part of "done" for this change

The change is only complete when:
1. `src/Admitto.Cli/generate-api-client.sh` runs to completion against a fresh Aspire AppHost.
2. `dotnet build src/Admitto.Cli` succeeds with the regenerated client committed.
3. The generated client diff is reviewed and committed in the same PR as the quarantine and new commands.

The `cli-api-client-generation` skill is the canonical procedure; `src/Admitto.Cli/AGENTS.md` gets a short troubleshooting addendum describing the "stale client masking missing types" failure mode and pointing future contributors at the quarantine `<ItemGroup>`.

### 4. No tests added

The CLI has no test project today. New commands are thin wrappers around the generated client (the same pattern as every other command) and are validated by manual smoke tests against the AppHost and by the build itself. Adding a test harness for Spectre.Console commands is out of scope.

## Risks / Trade-offs

- **Risk:** Quarantining commands silently removes user-visible functionality (attendee export, event creation via CLI, team-member management).
  - **Mitigation:** `Program.cs` change is explicit; quarantine `<ItemGroup>` has a `Label` and inline comment naming the missing backend types. `src/Admitto.Cli/AGENTS.md` documents the policy. Restoration is a single-commit revert once the backend surfaces return.

- **Risk:** Re-running NSwag locally still requires Aspire + Docker, which contributors may not have set up.
  - **Mitigation:** Out of scope — this constraint already exists. The AGENTS.md addendum reiterates it.

- **Risk:** New commands drift from the admin UI as future endpoints land without an accompanying CLI command.
  - **Mitigation:** The `cli-admin-parity` capability spec (this change) makes parity an explicit, testable requirement; the existing repo `AGENTS.md` Feature Implementation Checklist already calls for it. Future PRs that add admin endpoints without CLI commands can be flagged against the spec.

- **Trade-off:** Quarantined files stay in the repo as dead weight until either the backend surfaces return or the CLI explicitly drops the feature. Accepted in exchange for low-friction restoration.

## Migration Plan

1. Quarantine broken files via csproj `<Compile Remove>`; remove their registrations from `Program.cs`.
2. Confirm `dotnet build src/Admitto.Cli` succeeds against the *current* (stale) `ApiClient.g.cs` after quarantine.
3. Start Aspire AppHost; run `generate-api-client.sh`; commit the regenerated `ApiClient.g.cs`.
4. Confirm `dotnet build src/Admitto.Cli` still succeeds against the regenerated client.
5. Add the five new command files; register them under the `event` branch in `Program.cs`.
6. Update `src/Admitto.Cli/AGENTS.md` with the quarantine note and regeneration troubleshooting.
7. Mark `add-event-management-ui` tasks 9.2/9.3/9.4 done (the work has moved here).

Rollback: `git revert` the change. The quarantined files remain intact, so reverting fully restores the prior (broken-but-compiling) state.

## Open Questions

- Should `event email update` accept individual `--smtp-host` / `--smtp-port` / ... flags, or a single `--from-file` JSON payload? Default decision unless told otherwise: individual flags for the common SMTP fields, matching the UI form fields exactly. Resolved during implementation by inspecting the regenerated `UpsertEventEmailSettingsRequest` shape.
