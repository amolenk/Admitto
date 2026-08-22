# Test Suite Agent Guide

## Scope
This file applies to `/tests`. Test intent and layer boundaries are in `docs/arc42/10-quality-requirements.md`.

## First: Architecture Tests
Before any other test suite, verify architectural rules pass:
```bash
dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj
```
Architecture tests enforce dependency direction, naming conventions, placement rules, and messaging conventions (see §8.15 in `docs/arc42/08-crosscutting-concepts.md`). Fix violations before running other suites.

One rule worth knowing up front: message contracts — integration events, commands, and domain events — must declare exactly one public constructor. If you need a shorter way to build one in a test, add a builder under `Admitto.Testing/Builders/` — do not add a convenience overload to the contract.

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

### Given/When/Then Scenario Comments
Every test method gets a three-line `// Given` / `// When` / `// Then` comment immediately above the `[TestMethod]` attribute, written in plain English from the scenario's perspective — not the code's. This borrows the phrasing style of specs so a test reads as documentation on its own, without needing a separate spec.

- `Given` — the precondition/state the test sets up. Omit this line if there's no meaningful precondition beyond defaults.
- `When` — the action under test.
- `Then` — the observable outcome being asserted.

Keep each line to one short sentence. Prefer plain-English description over code identifiers (e.g. "an archived team", not "`TeamBuilder().AsArchived()`"). This comment block is separate from, and does not replace, any existing `// Arrange` / `// Act` / `// Assert` comments inside the test body.

```csharp
// Given an archived team
// When the name is changed
// Then it throws TeamArchived
[TestMethod]
public void ChangeName_ArchivedTeam_ThrowsTeamArchived()
{
    // Arrange
    var sut = new TeamBuilder().AsArchived().Build();

    // Act
    var result = ErrorResult.Capture(() => sut.ChangeName(TeamName.From("New Name")));

    // Assert
    result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
}
```

This convention applies to new and touched tests going forward; the repo-wide retrofit of existing tests is tracked separately.

### Error Assertions
When asserting that a `BusinessRuleViolationException` (or captured `Error`) matches a specific domain error, compare against the aggregate/entity's `Errors.*` static instance via `ShouldMatch`, not a raw string error code:

```csharp
// Good
result.Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);

// Avoid — breaks silently if the error message/type changes without the code changing,
// and duplicates a literal that already exists as a typed constant.
result.Error.Code.ShouldBe("badges_event.event_not_active");
```

`ShouldMatch` (in `Amolenk.Admitto.Testing.Infrastructure.Assertions`) compares code, type, message, and details in one call. Internal `Errors` classes are visible to test projects via `InternalsVisibleTo`, so there's no need to duplicate the string literal.

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
