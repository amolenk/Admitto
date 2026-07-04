## Why

The current multi-project module structure enforces architectural boundaries at build time but cannot express directional rules (e.g. modules may only communicate through Contracts), and gives agents no deterministic feedback on naming or placement violations. Consolidating all module projects into a single `Admitto.Core` class library and introducing ArchUnitNet provides precise, machine-readable enforcement of every architectural convention — making structural violations fail fast with clear error messages before any functional tests run.

## What Changes

- **BREAKING**: `Admitto.Module.Organization`, `Admitto.Module.Organization.Contracts`, `Admitto.Module.Registrations`, `Admitto.Module.Registrations.Contracts`, `Admitto.Module.Email`, `Admitto.Module.Email.Contracts`, `Admitto.Module.Shared`, and `Admitto.Module.Shared.Kernel` are dissolved into a single `Admitto.Core` class library
- `Admitto.Api` and `Admitto.Worker` update their project reference from 8 module projects to 1 (`Admitto.Core`)
- `Admitto.Migrations` updates its project references similarly (stays a separate executable)
- Six per-module test projects are consolidated into `Admitto.Core.Tests`
- A new `Admitto.Core.ArchTests` project is introduced containing ArchUnitNet rules that encode all architectural conventions as executable tests
- Misnamed event handler classes are renamed to conform to the `{EventType}Handler` pattern
- Namespace roots change from `Admitto.Module.*` to `Admitto.Core.Module.*` and `Admitto.Core.Shared.*`

## Capabilities

### New Capabilities

- `architecture-enforcement`: ArchUnit rule set governing dependency direction, class naming, and file placement within `Admitto.Core` — the executable architecture specification

### Modified Capabilities

_(none — no user-facing or API behaviour changes)_

## Impact

- All projects that reference the dissolved module projects must be updated
- All `using` directives with `Admitto.Module.*` namespaces change to `Admitto.Core.Module.*` or `Admitto.Core.Shared.*`
- EF Core migrations are unaffected in content; the three DbContexts remain separate, their migrations folders move with them into `Admitto.Core`
- No API surface changes; no database schema changes; no integration event changes
- Agent workflow gains a new first step: `dotnet test Admitto.Core.ArchTests` (fast, no infrastructure required) before running unit or E2E tests
