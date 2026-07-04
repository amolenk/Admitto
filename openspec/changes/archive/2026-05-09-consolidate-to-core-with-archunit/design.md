## Context

The codebase currently consists of 8 module-level projects: `Admitto.Module.Organization`, `Admitto.Module.Organization.Contracts`, `Admitto.Module.Registrations`, `Admitto.Module.Registrations.Contracts`, `Admitto.Module.Email`, `Admitto.Module.Email.Contracts`, `Admitto.Module.Shared`, and `Admitto.Module.Shared.Kernel`. Project references enforce some boundaries at build time, but cannot express directional rules within the dependency graph (e.g. modules may communicate only through Contracts, never through each other's Domain or Application layers). Naming and placement violations are silent at build time and only caught in code review — there is no machine-readable enforcement.

## Goals / Non-Goals

**Goals:**
- Consolidate the 8 module projects into a single `Admitto.Core` class library
- Merge the 6 per-module test projects into a single `Admitto.Core.Tests` project
- Introduce `Admitto.Core.ArchTests` with ArchUnitNet rules that make every architectural convention verifiable and agent-actionable
- Fix all event handler class names that deviate from the `{EventType}Handler` naming convention
- Preserve all existing runtime behaviour, API surface, database schema, and integration event contracts

**Non-Goals:**
- Changing the `Admitto.Migrations` project (stays a separate executable)
- Changing the `Admitto.Api`, `Admitto.Worker`, `Admitto.AppHost`, or `Admitto.ServiceDefaults` projects beyond updating project references
- Modifying any EF migration content
- Changing any API endpoint routes or response shapes
- Enforcing arch rules on `Admitto.Api` (it is the composition root; its rules are documented in AGENTS.md, not in ArchTests)

## Decisions

### 1. Single assembly over per-module assemblies

**Decision**: Merge all module projects into one `Admitto.Core` class library.

**Rationale**: Project-level boundaries cannot express directionality (A may depend on B.Contracts but not B.Domain). ArchUnit operating on namespaces within a single assembly can express all the rules that matter, plus many more (naming, placement). Build time is faster. The Contracts concept is preserved as a namespace/folder convention.

**Alternative considered**: Keep separate projects, add ArchUnit tests that load multiple assemblies. Rejected because it adds complexity without benefit — inter-assembly ArchUnit rules are harder to write and slower to run.

### 2. Namespace root: `Admitto.Core`

**Decision**: All merged namespaces use `Admitto.Core` as root.

```
Admitto.Module.Shared.Kernel.*         → Admitto.Core.Shared.Kernel.*
Admitto.Module.Shared.*                → Admitto.Core.Shared.*
Admitto.Module.Organization.*          → Admitto.Core.Module.Organization.*
Admitto.Module.Organization.Contracts  → Admitto.Core.Module.Organization.Contracts.*
Admitto.Module.Registrations.*         → Admitto.Core.Module.Registrations.*
Admitto.Module.Email.*                 → Admitto.Core.Module.Email.*
```

### 3. Contracts become a namespace/folder within the module

**Decision**: `Module.X.Contracts/` folder sits alongside `Domain/`, `Application/`, `Infrastructure/` inside each module folder. It may contain: facade interfaces, integration events, module events, and DTOs used for cross-module communication.

**ArchUnit rule**: Code outside `Module.X.*` may only reference `Module.X.Contracts.*` — never `Module.X.Domain.*`, `Module.X.Application.*`, or `Module.X.Infrastructure.*`.

### 4. Separate `Admitto.Core.ArchTests` project

**Decision**: Architecture tests live in a dedicated project, not inside `Admitto.Core.Tests`.

**Rationale**: Arch tests require no infrastructure (no DB, no Aspire), run in ~3 seconds, and serve a different purpose than functional tests. A separate project allows agents to run them first as a "pre-flight" check before running the slower functional test suite. The instruction to agents is: always run `dotnet test tests/Admitto.Core.ArchTests` before `dotnet test tests/Admitto.Core.Tests`.

### 5. `internal` access modifiers — no enforcement at CLR level

**Decision**: Do not use `internal` to enforce module boundaries within `Admitto.Core`. Let ArchUnit handle all meaningful boundaries.

**Rationale**: In a single assembly, `internal` cannot distinguish module boundaries. Attempting to do so would require complex `InternalsVisibleTo` arrangements. ArchUnit provides the same protection at test time with clearer error messages.

### 6. Event handler naming convention

**Decision**: Enforce `{EventType}Handler` for all event handler classes:
- `IDomainEventHandler<T>` implementors → `{T.Name}Handler` (e.g. `RegistrationCancelledDomainEvent` → `RegistrationCancelledDomainEventHandler`)
- `IIntegrationEventHandler<T>` implementors → `{T.Name}Handler`
- `IModuleEventHandler<T>` implementors → `{T.Name}Handler`
- `ICommandHandler<T>` implementors → `{T.Name}` with `Command` replaced by `Handler` (e.g. `CreateApiKeyCommand` → `CreateApiKeyHandler`)
- `IQueryHandler<T,R>` implementors → `{T.Name}` with `Query` replaced by `Handler`

Classes to rename as part of this change:
- `ProjectEventStatusToCatalogDomainEventHandler` → identify the exact domain event type and rename
- `TicketedEventArchivedReconfirmIntegrationEventHandler` → rename to `TicketedEventArchivedIntegrationEventHandler` (the "Reconfirm" context belongs in the use case folder, not the class name)

### 7. ArchUnit dependency rule set

```
Shared.Kernel          → no deps on any Admitto.Core namespace
Module.X.Domain        → Shared.Kernel only (no cross-module deps)
Module.X.Contracts     → Shared.Kernel + Module.X.Domain (value types only)
Module.X.Application   → Module.X.Domain + Shared.Application
                         + Module.Y.Contracts (any module, Contracts only)
Module.X.Infrastructure → Module.X.Application + Shared.Infrastructure
```

### 8. ArchUnit location rule set

```
*DomainEventHandler, *IntegrationEventHandler, *ModuleEventHandler
  → must reside in namespace ending with .EventHandlers

*HttpEndpoint
  → must reside in namespace ending with .AdminApi or .PublicApi

*Validator (AbstractValidator<T> subclasses)
  → must reside in namespace ending with .AdminApi or .PublicApi

*Command, *Query
  → must reside in *.Application.UseCases.*

*Handler (ICommandHandler/IQueryHandler impl)
  → must reside in *.Application.UseCases.*
```

## Risks / Trade-offs

**[Risk] ArchUnit is test-time, not build-time** → Mitigation: Agent workflow always runs ArchTests as step 1 after build. CI pipeline runs ArchTests before unit tests. Violations are caught before merge.

**[Risk] Large rename + move creates a noisy diff** → Mitigation: This is a one-time clean break. Use `dotnet build` after each module migration to confirm no broken references before moving to the next.

**[Risk] EF migration tool requires the DbContext assembly** → Mitigation: The three DbContexts remain in `Admitto.Core`; `Admitto.Migrations` updates its `<ProjectReference>` to `Admitto.Core`. Existing migration history is preserved unchanged — files move but content does not change.

**[Risk] Two integration event handlers for `TicketedEventArchived`** → The `Email` module has a `TicketedEventArchivedReconfirmIntegrationEventHandler`. After renaming, this conflicts with the Organization module's `TicketedEventArchivedIntegrationEventHandler`. Mitigation: The Email handler is renamed to reflect the use case (`ScheduleReconfirmationsOnTicketedEventArchivedIntegrationEventHandler`? No — better to keep it in its use case folder which provides the context). Actually: the use case folder `ScheduleReconfirmations/EventHandlers/` provides enough context; the handler can simply be `TicketedEventArchivedIntegrationEventHandler` in that namespace — namespaces disambiguate.

## Migration Plan

Execute module by module to keep the build green throughout:

1. Create `src/Admitto.Core/` project (empty class library, .NET 10, same NuGet deps as all merged projects combined)
2. Migrate `Admitto.Module.Shared.Kernel` → `Admitto.Core/Shared/Kernel/` (no deps, migrate first)
3. Migrate `Admitto.Module.Shared` → `Admitto.Core/Shared/` (update namespace refs to Kernel)
4. Migrate `Admitto.Module.Organization` + `Organization.Contracts` → `Admitto.Core/Module/Organization/`
5. Migrate `Admitto.Module.Registrations` + `Registrations.Contracts` → `Admitto.Core/Module/Registrations/`
6. Migrate `Admitto.Module.Email` + `Email.Contracts` → `Admitto.Core/Module/Email/`
7. Update `Admitto.Api`, `Admitto.Worker`, `Admitto.Migrations` project references → `Admitto.Core`
8. Remove the 8 dissolved projects from solution
9. Rename non-conforming event handler classes
10. Create `tests/Admitto.Core.Tests/` by merging the 6 module test projects
11. Create `tests/Admitto.Core.ArchTests/` with the full ArchUnit rule set
12. Remove the 6 dissolved test projects from solution
13. Update AGENTS.md to document the agent test workflow (`ArchTests` first)

**Rollback**: Each step is a commit. Revert to any prior commit restores the previous state.

## Open Questions

_(none — resolved in exploration session)_
