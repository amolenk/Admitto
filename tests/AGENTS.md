# Test Suite Agent Guide

## Scope
This file applies to `/tests`. Test intent and layer boundaries are in `docs/arc42/10-quality-requirements.md`.

## First: Architecture Tests
Before any other test suite, verify architectural rules pass:
```bash
dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj
```
Architecture tests enforce dependency direction, naming conventions, and placement rules (see §8.15 in `docs/arc42/08-crosscutting-concepts.md`). Fix violations before running other suites.

## Choosing the Right Suite
- Domain rule or value-object behavior → `Admitto.Core.Tests` (under `Module/*/Domain/`)
- Handler, event-driven workflow, persistence, or job behavior → `Admitto.Core.Tests` (under `Module/*/`)
- API wiring, auth, or route pipeline → `Admitto.Api.Tests`

## Commands
```bash
# Architecture tests (run first)
dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj

# All core module tests (domain + integration)
dotnet test tests/Admitto.Core.Tests/Admitto.Core.Tests.csproj

# API-level tests
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Environment Notes
- Aspire-backed integration/end-to-end suites start a distributed app host and require container runtime support.
- These suites reset databases between tests through shared base classes/fixtures; preserve that behavior when adding tests.

## Feature Scenario Coverage
Every acceptance scenario (`SC-*`) should have a corresponding test method. One scenario = one test by default; follow only documented exceptions.

### Folder Structure
Mirror the source `Module/{Module}/Application/UseCases/{Feature}/{UseCaseName}/` structure under `Admitto.Core.Tests`:

```
tests/Admitto.Core.Tests/
└── Module/Organization/
    └── Application/UseCases/
        └── TeamManagement/
            └── CreateTeam/
                ├── CreateTeamTests.cs
                └── CreateTeamFixture.cs
```

### Test Method Naming
Prefix scenario-mapped integration and API test methods with the scenario ID:

```csharp
[TestMethod]
public async Task SC001_CreateTeam_ValidInput_CreatesTeam() { ... }

[TestMethod]
public async Task SC002_CreateTeam_DuplicateName_ReturnsError() { ... }
```

Domain tests use `{Method}_{Condition}_{ExpectedOutcome}` (no `SC-*` prefix).

### Fixture Pattern
- One `*Fixture.cs` per use case with static factory methods for scenario variants.
- Use builder helpers (e.g., `TeamBuilder`, `UserBuilder`) for domain entities.
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

