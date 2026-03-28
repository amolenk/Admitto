# 10. Quality requirements

Quality goals from [chapter 1](01-introduction-and-goals.md) are made concrete here.

## 10.1 Quality scenarios

| ID | Quality | Stimulus | Response | Metric / target | Chapter 1 goal |
| :- | :------ | :------- | :------- | :-------------- | :------------- |
| Q-01 | Maintainability | Developer adds a new use case to a module | Change is contained within the module's project | Zero cross-module code changes | #1 |
| Q-02 | Reliability | Concurrent registrations for the last ticket | One succeeds, one gets a conflict error | Optimistic concurrency enforced via `Version` column | #2 |
| Q-03 | Reliability | Outbox dispatch fails after transaction commit | Message stays in outbox table for background retry | No messages lost | #2 |
| Q-04 | Security | Unauthenticated request to admin endpoint | 401 returned before handler executes | JWT validation runs in middleware | #4 |
| Q-05 | Operational simplicity | Operator deploys a new version | Single build artifact per host; no service mesh or discovery required | One container image per host, shared database and queue | #3 |

## 10.2 Test strategy

### Two-tier test structure

Each module has two test projects with distinct responsibilities:

| Project | Tests | Database | Dependencies |
| :------ | :---- | :------- | :----------- |
| `*.Domain.Tests` | Aggregate invariants, value objects, domain logic | None (pure in-memory) | Direct entity construction via builders |
| `*.Tests` | Handler integration, query results, persistence | Real PostgreSQL via Aspire | Aspire AppHost, Respawn, NSubstitute for cross-module facades |

**Guiding principle:** Domain tests verify *business rules* in isolation; integration tests verify *handler orchestration* with real infrastructure.

### Aspire integration test infrastructure

Integration tests run against a real PostgreSQL database managed by .NET Aspire:

1. **`AssemblySetup`** — `[AssemblyInitialize]` starts the Aspire `IntegrationTestAppHost` once per assembly, waits for `admitto-db` health, and creates the shared `IntegrationTestEnvironment`.
2. **`IntegrationTestEnvironment`** — wraps a `DatabaseTestContext<TDbContext>` that creates the database from EF Core migrations and initializes Respawn.
3. **`AspireIntegrationTestBase`** — abstract base class; `[TestInitialize]` resets the database via Respawn before each test to guarantee isolation.

Test classes inherit `AspireIntegrationTestBase` and receive `TestContext` via primary constructor:

```csharp
[TestClass]
public sealed class CreateCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_CreateCoupon_ValidInput_PersistsCouponAndRaisesDomainEvent()
    {
        // Arrange — fixture + handler
        // Act — handler.HandleAsync(command, testContext.CancellationToken)
        // Assert — Environment.Database.AssertAsync(...)
    }
}
```

### Fixture pattern

Fixtures encapsulate test data seeding and mock configuration behind **static factory methods** that read like scenario descriptions:

```csharp
internal sealed class RevokeCouponFixture
{
    // Private constructor — only static factories allowed.
    private RevokeCouponFixture() { }

    public static RevokeCouponFixture ActiveCoupon() => new() { _seedActiveCoupon = true };
    public static RevokeCouponFixture RedeemedCoupon() => new() { _seedRedeemedCoupon = true };
    public static RevokeCouponFixture NoCoupon() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment) { /* seed via SeedAsync */ }
}
```

**Rules:**
- One fixture class per use-case folder (same folder as the test class).
- Use `Environment.Database.SeedAsync(dbContext => { ... })` for data insertion.
- Use `Environment.Database.AssertAsync(async dbContext => { ... })` for post-act assertions (saves context first).
- Use `Environment.Database.WithContextAsync(...)` for read-only assertions without saving.

### Builder reuse

Domain builders (e.g. `CouponBuilder`) live in `*.Domain.Tests` and are reused by `*.Tests` via a project reference. This ensures test data construction is consistent across both tiers.

**Important:** When seeding coupons or other time-sensitive entities for integration tests, always set explicit future expiry dates (e.g. `DateTimeOffset.UtcNow.AddDays(30)`). The builder defaults use fixed past dates that work for domain tests (which control `now`) but cause status mismatches in integration tests that use `DateTimeOffset.UtcNow`.

### Cross-module facade mocking

Cross-module dependencies (e.g. `IOrganizationFacade`) are mocked with **NSubstitute** in the fixture, not resolved from DI:

```csharp
public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

// In SetupAsync:
OrganizationFacade
    .GetTicketTypesAsync(EventId.Value, Arg.Any<CancellationToken>())
    .Returns(ticketTypes);
```

Handlers are constructed directly with the mock + real DbContext:

```csharp
private static CreateCouponHandler NewCreateCouponHandler(CreateCouponFixture fixture) =>
    new(fixture.OrganizationFacade, Environment.Database.Context);
```

### Test naming convention

Test methods follow: `{ScenarioId}_{UseCase}_{Condition}_{ExpectedOutcome}`

Example: `SC001_CreateCoupon_ValidInput_PersistsCouponAndRaisesDomainEvent`

### Error assertion pattern

Use `ErrorResult.CaptureAsync(...)` to capture domain errors, then assert with `ShouldMatch`:

```csharp
var result = await ErrorResult.CaptureAsync(
    async () => { await sut.HandleAsync(command, ct); });

result.Error.ShouldMatch(Coupon.Errors.ExpiryMustBeInFuture);
```

## Done-when

- [x] Each quality goal from chapter 1 has at least one scenario.
- [x] Each scenario has a measurable metric or acceptance criterion.
- [x] Scenarios link back to the relevant chapter 1 quality goal.
