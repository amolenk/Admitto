## Context

The current `Admitto.Core.Tests` project conflates two fundamentally different test tiers — pure in-memory domain tests and Aspire-backed integration tests — in a single MSTest assembly. This makes it impossible to run lightweight domain tests without spinning up a full container stack. Additionally, `Admitto.TestHelpers` is an orphaned project (absent from the solution file, unreferenced, using a stale `ApplicationContext`) that creates confusion, and the SC-prefix convention on test methods produces unreadable names without a spec lookup tool.

## Goals / Non-Goals

**Goals:**
- Run domain tests independently of any infrastructure without starting Aspire or containers.
- Align test project names with the `*.DomainTests` / `*.IntegrationTests` naming pattern stated in the quality requirements document.
- Align folder/namespace paths in both new projects with the source project layout.
- Remove the SC-prefix naming requirement and clean up ~82 test methods and all guiding docs.
- Delete the orphaned `Admitto.TestHelpers` project.
- Keep `Admitto.Testing` (shared infrastructure library) unchanged.

**Non-Goals:**
- Adding new tests or changing test coverage.
- Changing any source (non-test) code.
- Modifying the API test project (`Admitto.Api.Tests`) beyond updating its project reference.

## Decisions

### Decision: Two separate MSTest projects, not one project with sub-configurations

**Chosen**: `Admitto.Core.DomainTests` and `Admitto.Core.IntegrationTests` as separate `.csproj` files.

**Alternatives considered**:
- Single project with compile-time symbols to include/exclude Aspire — rejected because it complicates the project file and still links both test tiers at compile time.
- Separate solution folders — already the plan, this is complementary.

**Rationale**: Separate projects enforce the infrastructure boundary at the compiler level. Domain tests literally cannot reference Aspire packages. CI can run domain tests on any machine without a container daemon.

### Decision: Collapse the `Domain/Domain/` double-folder to a single `Domain/` level

**Chosen**: Mirror the source tree exactly: `{Module}/Domain/` (e.g. `Organization/Domain/Entities/`, `Organization/Domain/Builders/`).

**Rationale**: The extra `Domain/` nesting was never intentional — it arose as tests mirrored a now-removed intermediate folder. Removing it aligns the test tree with `src/Admitto.Core/{Module}/Domain/`.

### Decision: `Admitto.Api.Tests` references `Admitto.Core.DomainTests` (not `Admitto.Core.IntegrationTests`)

**Rationale**: `Admitto.Api.Tests` only needs the domain builders (e.g. `TeamBuilder`, `CouponBuilder`) that live in `Admitto.Core.DomainTests`. It must not depend on the Aspire-backed integration project.

### Decision: Delete `Admitto.TestHelpers`, do not migrate any code from it

**Rationale**: The project is absent from `Admitto.slnx` and has no inbound project references. Its `DatabaseTestFixture` targets a stale `ApplicationContext` that no longer exists in the current module-based architecture. There is nothing worth preserving.

### Decision: SC-prefix convention is dropped entirely; tests use `{Method}_{Condition}_{ExpectedOutcome}`

**Rationale**: The SC number provides traceability to a spec scenario, but only if the developer has the spec file open. Without tooling to look up `SC042`, the prefix adds noise and obscures intent. Descriptive method names communicate the scenario inline.

## Risks / Trade-offs

- [Risk] Renaming ~82 test methods may cause temporary git blame churn → Mitigation: perform the rename as a single atomic commit to keep history searchable.
- [Risk] Renaming projects changes the MSTest assembly name, which may break `--filter FullyQualifiedName~…` invocations in CI scripts → Mitigation: update all CI commands and `tests/AGENTS.md` as part of this change.
- [Risk] Moving builders to `Admitto.Core.DomainTests` could cause confusion (builders in a "tests" project) → Mitigation: document builder placement in `tests/AGENTS.md` and in the `test-organization` spec.

## Migration Plan

1. Create `Admitto.Core.DomainTests` project; move `Module/*/Domain/` content with corrected folder paths.
2. Create `Admitto.Core.IntegrationTests` project; move `Module/*/Application/` content plus `AssemblySetup`, `MSTestSettings`, and per-module `Infrastructure/` subtrees.
3. Update `Admitto.Api.Tests.csproj` to reference `Admitto.Core.DomainTests`.
4. Delete `tests/Admitto.TestHelpers/`.
5. Update `Admitto.slnx`.
6. Rename all SC-prefixed test methods across both suites.
7. Update docs: `docs/arc42/10-quality-requirements.md`, `docs/arc42/08-crosscutting-concepts.md`, `tests/AGENTS.md`.
8. Run `dotnet test` for all four test projects and confirm green.

Rollback: revert the branch; no database schema changes are involved.

## Open Questions

*(none)*
