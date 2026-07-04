## Why

The current test organisation creates friction: SC-prefix method names are opaque without a spec lookup tool, both domain-level and infrastructure-dependent tests live in the same MSTest project (making it impossible to run pure domain tests in isolation), an orphaned `Admitto.TestHelpers` project clutters the `tests/` folder, and the `Domain/Domain/` double-folder path in `Admitto.Core.Tests` is confusing.

## What Changes

- **Remove SC-prefix naming convention** from all test methods in `Admitto.Core.Tests` and `Admitto.Api.Tests`. Test methods revert to `{Method}_{Condition}_{ExpectedOutcome}` everywhere. Remove the requirement from all docs and agent guides.
- **Split `Admitto.Core.Tests`** into two distinct projects:
  - `Admitto.Core.DomainTests` — pure in-memory aggregate/value-object tests; no Aspire, no database.
  - `Admitto.Core.IntegrationTests` — handler, event-driven workflow, persistence, and job tests backed by a real PostgreSQL database via Aspire.
- **Align folder and namespace paths** with the source project: the current `Module/{Module}/Domain/Domain/` double-level is collapsed to `{Module}/Domain/` (mirroring `src/Admitto.Core/{Module}/Domain/`).
- **Delete `Admitto.TestHelpers`** — the project is not in the solution file, references no other project (and is referenced by none), and uses the stale `ApplicationContext`. There is nothing to migrate.
- **Keep `Admitto.Testing`** as-is — it is a well-scoped shared infrastructure library used by both `Admitto.Core.IntegrationTests` and `Admitto.Api.Tests`.
- Update `Admitto.Api.Tests` to reference `Admitto.Core.DomainTests` (for builders) instead of the old `Admitto.Core.Tests`.
- Update `Admitto.slnx` with the two new projects; remove the old `Admitto.Core.Tests` entry.

## Capabilities

### New Capabilities

- `test-organization`: Conventions, project boundaries, and folder structure for the three test tiers (domain, integration, API).

### Modified Capabilities

*(none — no existing capability specs change their functional requirements)*

## Impact

- `tests/Admitto.Core.Tests/` — deleted / replaced by two new projects.
- `tests/Admitto.TestHelpers/` — deleted.
- `tests/Admitto.Core.DomainTests/` — new project; pure in-memory MSTest project, no Aspire dependency.
- `tests/Admitto.Core.IntegrationTests/` — new project; replaces integration half of old `Admitto.Core.Tests`; keeps Aspire + Respawn dependency.
- `tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj` — update `ProjectReference` from `Admitto.Core.Tests` to `Admitto.Core.DomainTests`.
- `Admitto.slnx` — updated project list.
- `docs/arc42/10-quality-requirements.md`, `docs/arc42/08-crosscutting-concepts.md`, `tests/AGENTS.md` — SC-prefix guidance removed; test strategy table updated to match new project names.
- ~82 test method names across both test suites (rename `SC001_…` → descriptive method names without prefix).
