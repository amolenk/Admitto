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

## Authoring Notes
- Prefer builder/fixture helpers over repetitive setup.
- Keep tests focused on observable behavior (domain error, HTTP result, persisted state).
- Add or update tests in the same module layer as the behavior you changed.
