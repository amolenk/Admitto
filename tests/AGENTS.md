# Test Suite Agent Guide

## Scope
This file applies to `/tests`. Test intent and layer boundaries are in `docs/arc42/10-quality-requirements.md`.

## First: Architecture Tests
Before any other test suite, verify architectural rules pass:
```bash
dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj
```
Architecture tests enforce dependency direction, naming conventions, and placement rules (see §8.15 in `docs/arc42/08-crosscutting-concepts.md`). Fix violations before running other suites.

## Choosing the Right Suite
- Domain rule or value-object behavior → `Admitto.Core.DomainTests`
- Handler, event-driven workflow, persistence, or job behavior → `Admitto.Core.IntegrationTests`
- API wiring, auth, or route pipeline → `Admitto.Api.Tests`

## Commands
```bash
# Architecture tests (run first)
dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj

# Domain unit tests
dotnet test --project tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj

# Core integration tests (requires container runtime)
dotnet test --project tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj

# API-level tests (requires container runtime)
dotnet test --project tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Environment Notes
- Aspire-backed integration/end-to-end suites start a distributed app host and require container runtime support.
- These suites reset databases between tests through shared base classes/fixtures; preserve that behavior when adding tests.

### Folder Structure
Mirror the source structure under the appropriate test project:

```
tests/Admitto.Core.DomainTests/
└── Organization/Domain/Entities/
    └── TeamTests.cs

tests/Admitto.Core.IntegrationTests/
└── Organization/Application/UseCases/
    └── TeamManagement/
        └── CreateTeam/
            ├── CreateTeamTests.cs
            └── CreateTeamFixture.cs
```

### Test Method Naming
All tests use `{Method}_{Condition}_{ExpectedOutcome}`:

```csharp
[TestMethod]
public async Task CreateTeam_ValidInput_CreatesTeam() { ... }

[TestMethod]
public async Task CreateTeam_DuplicateName_ReturnsError() { ... }
```

**Do not prefix test method names with scenario IDs** (e.g., `SC001_`, `SC-BIND_`). Do not reference scenario IDs in comments either. Describe the scenario in plain English in the test body or method name.

### Builders
Builders (e.g., `TeamBuilder`, `CouponBuilder`) live in `Admitto.Testing/Builders/` and are shared across all test projects. Add new builders there.

### Fixture Pattern
- One `*Fixture.cs` per use case with static factory methods for scenario variants.
- Use builder helpers for domain entities.
- `CreateTeamFixture` is the canonical example.

### Coverage Rules
- `Must`-priority scenarios are mandatory.
- `Should`-priority scenarios should be implemented when feasible.
- If a scenario can't be tested (e.g., external dependency), document why in a comment and flag for manual testing.

### Test Levels
- **Domain tests**: aggregate invariants and value objects in isolation.
- **Integration tests** (handler-level): business logic through handlers, event-driven workflows, and jobs with a real database.
- **End-to-end tests** (HTTP-level): full request pipeline including routing, validation, and persistence.

## Authoring Notes
- Prefer builder/fixture helpers over repetitive setup.
- Keep tests focused on observable behavior (domain error, HTTP result, persisted state).
- Add or update tests in the same module layer as the behavior you changed.
