## 1. Move builders to Admitto.Testing

Builders are moved to `Admitto.Testing` so all test projects can reference them via the existing shared library, eliminating cross-test-project builder dependencies entirely.

- [x] 1.1 Move `Module/Organization/Domain/Domain/Builders/` → `tests/Admitto.Testing/Builders/Organization/Domain/`; update namespaces to `Amolenk.Admitto.Testing.Builders.Organization.Domain`
- [x] 1.2 Move `Module/Organization/Application/Builders/` → `tests/Admitto.Testing/Builders/Organization/Application/`; update namespaces to `Amolenk.Admitto.Testing.Builders.Organization.Application`
- [x] 1.3 Move `Module/Registrations/Domain/Domain/Builders/` → `tests/Admitto.Testing/Builders/Registrations/Domain/`; update namespaces to `Amolenk.Admitto.Testing.Builders.Registrations.Domain`
- [x] 1.4 Move `Module/Registrations/Application/Builders/` → `tests/Admitto.Testing/Builders/Registrations/Application/`; update namespaces (if any files exist)
- [x] 1.5 Move `Module/Email/Domain/Domain/Builders/` → `tests/Admitto.Testing/Builders/Email/Domain/`; update namespaces to `Amolenk.Admitto.Testing.Builders.Email.Domain`

## 2. Create Admitto.Core.DomainTests project

- [x] 2.1 Create `tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj` using `MSTest.Sdk`; include `Shouldly`; add `ProjectReference` to `Admitto.Core.csproj` and `Admitto.Testing.csproj`
- [x] 2.2 Move `Module/Organization/Domain/Domain/Entities/` → `Organization/Domain/Entities/`; update namespaces
- [x] 2.3 Move `Module/Organization/Domain/Domain/ValueObjects/` (if any) → `Organization/Domain/ValueObjects/`; update namespaces
- [x] 2.4 Move `Module/Registrations/Domain/Domain/Entities/` → `Registrations/Domain/Entities/`; update namespaces
- [x] 2.5 Move `Module/Registrations/Domain/Domain/ValueObjects/` → `Registrations/Domain/ValueObjects/`; update namespaces
- [x] 2.6 Move `Module/Email/Domain/Domain/Entities/` → `Email/Domain/Entities/`; update namespaces
- [x] 2.7 Move `Module/Email/Domain/Domain/ValueObjects/` → `Email/Domain/ValueObjects/`; update namespaces

## 3. Create Admitto.Core.IntegrationTests project

- [x] 3.1 Create `tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj` using `MSTest.Sdk`; include `Aspire.Hosting.Testing`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.Extensions.TimeProvider.Testing`, `NSubstitute`, `Respawn`, `Shouldly`; add `ProjectReference` to `Admitto.Core.csproj`, `Admitto.AppHost.csproj`, `Admitto.Testing.csproj`
- [x] 3.2 Move `AssemblySetup.cs` and `MSTestSettings.cs` to the new project; update namespace and assembly-level using imports
- [x] 3.3 Move `Module/Organization/Application/` (excluding `Builders/` — already moved to Admitto.Testing) → `Organization/Application/`; update namespaces
- [x] 3.4 Move `Module/Registrations/Application/` (excluding `Builders/`) → `Registrations/Application/`; update namespaces
- [x] 3.5 Move `Module/Email/Application/` → `Email/Application/`; update namespaces

## 4. Update project references and solution

- [x] 4.1 Update `Admitto.Api.Tests.csproj`: remove `ProjectReference` to `Admitto.Core.Tests` (builders now come from `Admitto.Testing`)
- [x] 4.2 Update `Admitto.slnx`: replace `Admitto.Core.Tests` entry with `Admitto.Core.DomainTests` and `Admitto.Core.IntegrationTests`
- [x] 4.3 Delete `tests/Admitto.Core.Tests/` directory entirely
- [x] 4.4 Delete `tests/Admitto.TestHelpers/` directory entirely

## 5. Remove SC-prefix from test methods

- [x] 5.1 Rename all `SC###_…` test methods in `Admitto.Core.IntegrationTests` (under `Organization/`) to `{Method}_{Condition}_{ExpectedOutcome}`
- [x] 5.2 Rename all `SC###_…` test methods in `Admitto.Core.IntegrationTests` (under `Registrations/`) to `{Method}_{Condition}_{ExpectedOutcome}`
- [x] 5.3 Rename all `SC###_…` test methods in `Admitto.Core.IntegrationTests` (under `Email/`) to `{Method}_{Condition}_{ExpectedOutcome}`
- [x] 5.4 Rename all `SC###_…` test methods in `Admitto.Api.Tests` to `{Method}_{Condition}_{ExpectedOutcome}`

## 6. Update documentation and agent guides

- [x] 6.1 Update `docs/arc42/10-quality-requirements.md`: replace `*.Tests` references in the test strategy table with `*.DomainTests` / `*.IntegrationTests`; remove the SC-prefix example method name and replace with a plain descriptive name; remove the traceability rationale for SC prefixes
- [x] 6.2 Update `docs/arc42/08-crosscutting-concepts.md`: remove the `// SC-015 is testable` inline comment and any other SC references
- [x] 6.3 Update `tests/AGENTS.md`: update suite names and `dotnet test` commands; remove SC-prefix naming requirement; replace SC-prefix example methods with descriptive examples; update folder structure example to match new project layouts; note that builders live in `Admitto.Testing`
- [x] 6.4 Update the `test-organization` spec (in this change) to reflect the builder-in-Admitto.Testing decision

## 7. Verify

- [x] 7.1 Run `dotnet build Admitto.slnx` and confirm zero errors
- [x] 7.2 Run `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` and confirm green
- [x] 7.3 Run `dotnet test tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj` and confirm green (no Aspire required)
- [x] 7.4 Run `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj` and confirm green
- [x] 7.5 Run `dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj` and confirm green
