## Purpose

Defines the organization and conventions for automated tests in Admitto. Establishes project boundaries, folder structure, builder placement, and test method naming rules.

## Requirements

### Requirement: Test projects are split by infrastructure dependency
There SHALL be two Core test projects: `Admitto.Core.DomainTests` (pure in-memory) and `Admitto.Core.IntegrationTests` (Aspire-backed). The `Admitto.Api.Tests` project remains unchanged in purpose.

#### Scenario: Domain tests run without starting Aspire
- **WHEN** a developer runs `dotnet test tests/Admitto.Core.DomainTests/`
- **THEN** no Aspire host, no database container, and no network service SHALL be started; all tests complete in isolation

#### Scenario: Integration tests use a real database
- **WHEN** a developer runs `dotnet test tests/Admitto.Core.IntegrationTests/`
- **THEN** the Aspire AppHost starts, a real PostgreSQL database is provisioned, and tests execute against it

---

### Requirement: Test folder structure mirrors the source project
Test file paths in both `Admitto.Core.DomainTests` and `Admitto.Core.IntegrationTests` SHALL mirror the corresponding source path in `src/Admitto.Core/`.

#### Scenario: Domain entity test path
- **WHEN** a domain entity test exists for `src/Admitto.Core/Organization/Domain/Entities/Team.cs`
- **THEN** its test file SHALL reside at `tests/Admitto.Core.DomainTests/Organization/Domain/Entities/TeamTests.cs`

#### Scenario: Integration use-case test path
- **WHEN** an integration test exists for `src/Admitto.Core/Organization/Application/UseCases/TeamManagement/CreateTeam/CreateTeamHandler.cs`
- **THEN** its test file SHALL reside at `tests/Admitto.Core.IntegrationTests/Organization/Application/UseCases/TeamManagement/CreateTeam/CreateTeamTests.cs`

---

### Requirement: Builders live in Admitto.Testing
Builder classes (e.g. `TeamBuilder`, `CouponBuilder`) SHALL reside in `Admitto.Testing` under `Builders/{Module}/{Domain|Application}/`. All test projects (`Admitto.Core.DomainTests`, `Admitto.Core.IntegrationTests`, `Admitto.Api.Tests`) SHALL reference `Admitto.Testing` to access builders. No test project SHALL reference another test project for builders.

#### Scenario: Builder reuse from integration tests
- **WHEN** an integration test in `Admitto.Core.IntegrationTests` needs a `TeamBuilder`
- **THEN** it SHALL import it from `Admitto.Testing` (no test-to-test project reference)

#### Scenario: Builder reuse from API tests
- **WHEN** an API test in `Admitto.Api.Tests` needs a `CouponBuilder`
- **THEN** it SHALL import it from `Admitto.Testing` (no test-to-test project reference)

---

### Requirement: Test method names are descriptive without external ID prefixes
All test methods in `Admitto.Core.DomainTests`, `Admitto.Core.IntegrationTests`, and `Admitto.Api.Tests` SHALL use the `{Method}_{Condition}_{ExpectedOutcome}` naming pattern. No external ID prefixes (such as `SC-*`) SHALL appear in method names.

#### Scenario: Integration test method naming
- **WHEN** a developer writes a new integration test for the CreateTeam use case
- **THEN** the method name SHALL follow `CreateTeam_ValidInput_CreatesTeam` (not `SC001_CreateTeam_ValidInput_CreatesTeam`)

#### Scenario: Domain test method naming
- **WHEN** a developer writes a new domain test for a `Team` aggregate behaviour
- **THEN** the method name SHALL follow `AddMember_WhenAtCapacity_ThrowsDomainException`

---

### Requirement: Admitto.TestHelpers is removed
The `tests/Admitto.TestHelpers/` directory and project SHALL NOT exist. No code from it SHALL be migrated, as it references a stale `ApplicationContext` that no longer exists.

#### Scenario: No reference to Admitto.TestHelpers
- **WHEN** the solution is built
- **THEN** no project in `Admitto.slnx` or any `.csproj` SHALL reference `Admitto.TestHelpers`
