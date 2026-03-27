# Test Suite Agent Guide

## Scope
This file applies to `/tests`.

## Test Strategy Reference
- Test intent and layer boundaries are described in `/docs/README.md` section `10 Quality Requirements and Test Strategy`.

## Choosing the Right Suite
- Domain rule or application/module behavior changes:
  - `tests/Admitto.Module.Organization.Tests`
  - `tests/Admitto.Module.Registrations.Tests`
- API wiring/auth/route pipeline changes:
  - `tests/Admitto.Api.Tests`

## Commands
```bash
dotnet test tests/Admitto.Module.Organization.Tests/Admitto.Module.Organization.Tests.csproj
dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Environment Notes
- Aspire-backed integration/end-to-end suites start a distributed app host and require container runtime support.
- These suites reset databases between tests through shared base classes/fixtures; preserve that behavior when adding tests.

## Feature Scenario Coverage

When implementing a feature from `docs/specs/FEAT-*.md`, every acceptance scenario
(`SC-*`) must have a corresponding test method.

### Folder Structure
Mirror the source `Application/UseCases/{Feature}/{UseCaseName}/` structure in the
test project:

```
tests/Admitto.Module.Organization.Tests/
└── Application/UseCases/
    └── TeamManagement/
        └── CreateTeam/
            ├── CreateTeamTests.cs
            └── CreateTeamFixture.cs
```

### Test Method Naming
Prefix each test method with the scenario ID from the spec:

```csharp
[TestMethod]
public async Task SC001_CreateTeam_ValidInput_CreatesTeam() { ... }

[TestMethod]
public async Task SC002_CreateTeam_DuplicateName_ReturnsError() { ... }
```

The feature context is already established by the folder path — no feature-level
prefix is needed in the method name.

### Fixture Pattern
- One `*Fixture.cs` per use case with static factory methods for scenario variants.
- Use builder helpers (e.g., `TeamBuilder`, `UserBuilder`) for domain entities.
- Reference `CreateTeamFixture` as the canonical example.

### Coverage Rules
- `Must`-priority scenarios are mandatory — do not skip them.
- `Should`-priority scenarios should be implemented when feasible.
- If a scenario cannot be tested (e.g., external dependency), document why in a
  code comment and flag it for manual testing.

### Test Levels
- **Integration tests** (handler-level): `Admitto.Module.*.Tests` — test business
  logic through the handler with a real database.
- **End-to-end tests** (HTTP-level): `Admitto.Api.Tests` — test the full request
  pipeline including routing, validation, and persistence.

## Authoring Notes
- Prefer builder/fixture helpers over repetitive setup.
- Keep tests focused on observable behavior (domain error, HTTP result, persisted state).
- Add or update tests in the same module layer as the behavior you changed.
