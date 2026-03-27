# Feature Specs Agent Guide

## Scope
This file applies to `/docs/specs`.

## What Is a Feature Spec?
Each `FEAT-*.md` file is a self-contained feature specification. Before implementing any feature, read the full spec. The key sections that drive implementation are:

- **Section 2 — User Stories:** Each user story maps to one vertical slice (one subfolder under `Application/UseCases/{Feature}/`).
- **Section 4 — Acceptance Scenarios:** Each `SC-*` scenario maps to one test method.

## Reading Order
1. Read the **Overview** (section 1) for scope boundaries and non-goals.
2. Read **User Stories** (section 2) to identify the vertical slices you need to create.
3. Read **Functional Requirements** (section 3) for detailed rules and constraints.
4. Read **Acceptance Scenarios** (section 4) to understand the expected test coverage.
5. Read **Domain Model** (section 5) for entities, value objects, and invariants.

## Implementation Rules

### One User Story → One Vertical Slice
- Each user story (`US-*`) in section 2 becomes its own subfolder under `Application/UseCases/{Feature}/{UseCaseName}/`.
- The feature name in the folder path must match the spec title (e.g., `FEAT-001 Team Management` → `UseCases/TeamManagement/`).
- Do not merge multiple user stories into a single handler.

### One Acceptance Scenario → One Test Method
- Every `SC-*` scenario in section 4 must have a corresponding test method.
- Test methods are prefixed with the scenario ID (e.g., `SC001_CreateTeam_ValidInput_CreatesTeam`).
- `Must`-priority scenarios are mandatory; `Should`-priority scenarios should be implemented when feasible.

### Mapping Feature Specs to Modules
- The spec's **Epic / Parent** field (section 1) tells you which module the feature belongs to.
- Implement the code in the module project indicated (e.g., `Admitto.Module.Organization` for Organization Module features).
- Write tests in the corresponding test project (e.g., `Admitto.Module.Organization.Tests`).

## Do Not
- Skip reading the full spec before starting implementation.
- Implement scenarios that are out of scope (check section 1.3 Non-Goals).
- Invent acceptance criteria not present in the spec — if something is missing, flag it.
