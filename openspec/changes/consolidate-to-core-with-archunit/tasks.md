## 1. Create Admitto.Core project

- [x] 1.1 Create `src/Admitto.Core/` class library project targeting .NET 10 with all NuGet dependencies from the 8 dissolved projects combined
- [x] 1.2 Add `Admitto.Core` to `Admitto.slnx`

## 2. Migrate Shared.Kernel

- [x] 2.1 Move all files from `Admitto.Module.Shared.Kernel` into `src/Admitto.Core/Shared/Kernel/`, updating namespace from `Admitto.Module.Shared.Kernel` to `Admitto.Core.Shared.Kernel`
- [x] 2.2 Verify `dotnet build Admitto.Core` succeeds

## 3. Migrate Shared

- [x] 3.1 Move all files from `Admitto.Module.Shared` into `src/Admitto.Core/Shared/`, updating namespace from `Admitto.Module.Shared` to `Admitto.Core.Shared`, updating all `using Admitto.Module.Shared.Kernel` to `Admitto.Core.Shared.Kernel`
- [x] 3.2 Verify `dotnet build Admitto.Core` succeeds

## 4. Migrate Organization module

- [x] 4.1 Move all files from `Admitto.Module.Organization` into `src/Admitto.Core/Module/Organization/`, updating namespace root from `Admitto.Module.Organization` to `Admitto.Core.Module.Organization`, updating all `using` directives to the new `Admitto.Core.*` namespaces
- [x] 4.2 Move all files from `Admitto.Module.Organization.Contracts` into `src/Admitto.Core/Module/Organization/Contracts/`, updating namespace to `Admitto.Core.Module.Organization.Contracts`
- [x] 4.3 Verify `dotnet build Admitto.Core` succeeds

## 5. Migrate Registrations module

- [x] 5.1 Move all files from `Admitto.Module.Registrations` into `src/Admitto.Core/Module/Registrations/`, updating namespace root and all `using` directives to `Admitto.Core.*`
- [x] 5.2 Move all files from `Admitto.Module.Registrations.Contracts` into `src/Admitto.Core/Module/Registrations/Contracts/`, updating namespace to `Admitto.Core.Module.Registrations.Contracts`
- [x] 5.3 Verify `dotnet build Admitto.Core` succeeds

## 6. Migrate Email module

- [x] 6.1 Move all files from `Admitto.Module.Email` into `src/Admitto.Core/Module/Email/`, updating namespace root and all `using` directives to `Admitto.Core.*`
- [x] 6.2 Move all files from `Admitto.Module.Email.Contracts` into `src/Admitto.Core/Module/Email/Contracts/`, updating namespace to `Admitto.Core.Module.Email.Contracts`
- [x] 6.3 Verify `dotnet build Admitto.Core` succeeds

## 7. Update entry point projects

- [x] 7.1 Replace all module `<ProjectReference>` entries in `Admitto.Api.csproj` with a single reference to `Admitto.Core`; update all `using` directives in `Admitto.Api` to `Admitto.Core.*` namespaces
- [x] 7.2 Replace all module `<ProjectReference>` entries in `Admitto.Worker.csproj` with a single reference to `Admitto.Core`; update all `using` directives
- [x] 7.3 Replace all module `<ProjectReference>` entries in `Admitto.Migrations.csproj` with a single reference to `Admitto.Core`; update all `using` directives
- [x] 7.4 Verify `dotnet build` on the full solution succeeds

## 8. Remove dissolved projects

- [x] 8.1 Remove `Admitto.Module.Organization`, `Admitto.Module.Organization.Contracts`, `Admitto.Module.Registrations`, `Admitto.Module.Registrations.Contracts`, `Admitto.Module.Email`, `Admitto.Module.Email.Contracts`, `Admitto.Module.Shared`, and `Admitto.Module.Shared.Kernel` from `Admitto.slnx`
- [x] 8.2 Delete the corresponding `src/` directories for all 8 dissolved projects
- [x] 8.3 Verify `dotnet build` on the full solution succeeds with no orphaned references

## 9. Fix non-conforming event handler names

- [x] 9.1 Identify the domain event type handled by `ProjectEventStatusToCatalogDomainEventHandler` and rename the class to `{EventType}Handler` (e.g. if it handles `TicketCatalogProjectionRequestedDomainEvent` → `TicketCatalogProjectionRequestedDomainEventHandler`)
- [x] 9.2 Rename `TicketedEventArchivedReconfirmIntegrationEventHandler` (Email module) to `TicketedEventArchivedIntegrationEventHandler` — the use case folder `ScheduleReconfirmations/EventHandlers/` provides the context; the "Reconfirm" qualifier is redundant in the class name
- [x] 9.3 Audit all other `*EventHandler` classes and rename any that deviate from the `{EventType}Handler` pattern
- [x] 9.4 Verify `dotnet build Admitto.Core` succeeds after all renames

## 10. Create Admitto.Core.Tests

- [x] 10.1 Create `tests/Admitto.Core.Tests/` project targeting .NET 10 with the same test framework and NuGet dependencies as the 6 dissolved test projects; add it to `Admitto.slnx`
- [x] 10.2 Move all test files from `Admitto.Module.Organization.Domain.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.3 Move all test files from `Admitto.Module.Organization.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.4 Move all test files from `Admitto.Module.Registrations.Domain.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.5 Move all test files from `Admitto.Module.Registrations.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.6 Move all test files from `Admitto.Module.Email.Domain.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.7 Move all test files from `Admitto.Module.Email.Tests` into `tests/Admitto.Core.Tests/`, updating namespaces
- [x] 10.8 Remove the 6 dissolved test projects from `Admitto.slnx` and delete their directories
- [x] 10.9 Verify all tests in `Admitto.Core.Tests` pass

## 11. Create Admitto.Core.ArchTests

- [x] 11.1 Create `tests/Admitto.Core.ArchTests/` project targeting .NET 10 with ArchUnitNet NuGet package; add it to `Admitto.slnx`
- [x] 11.2 Implement dependency direction rules: Shared.Kernel has no Admitto.Core deps; Domain has no Application/Infrastructure deps; Application may only cross-reference other modules via Contracts; Infrastructure may not reference other modules except via Contracts
- [x] 11.3 Implement naming rules: `IDomainEventHandler<T>` → `{T.Name}Handler`; `IIntegrationEventHandler<T>` → `{T.Name}Handler`; `IModuleEventHandler<T>` → `{T.Name}Handler`; `ICommandHandler<T>` → command name with suffix replaced by Handler; `IQueryHandler<T,R>` → query name with suffix replaced by Handler
- [x] 11.4 Implement placement rules: `*DomainEventHandler`, `*IntegrationEventHandler`, `*ModuleEventHandler` must be in namespace ending with `EventHandlers`; `*HttpEndpoint` must be in `AdminApi` or `PublicApi`; `AbstractValidator<T>` subclasses must be in `AdminApi` or `PublicApi`; `*Command`/`*Query` must be in `*.Application.UseCases.*`
- [x] 11.5 Verify all ArchUnit tests pass with zero violations

## 12. Update documentation

- [ ] 12.1 Update `docs/arc42/05-building-block-view.md` to reflect the new `Admitto.Core` assembly and its internal namespace structure
- [ ] 12.2 Update `docs/arc42/08-crosscutting-concepts.md` to document the ArchUnit enforcement mechanism and the Contracts namespace convention
- [ ] 12.3 Update `AGENTS.md` agent workflow to document running `dotnet test tests/Admitto.Core.ArchTests` as the first test step after `dotnet build`
