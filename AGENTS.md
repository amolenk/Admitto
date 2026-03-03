# Admitto Agent Guide

## Scope
This file applies to the entire repository. Nested `AGENTS.md` files in subdirectories provide stricter local guidance.

## Canonical Architecture Source
- Read `/docs/README.md` first.
- Treat `/docs/README.md` as the source of truth for architecture and patterns.
- Focus on these sections when implementing changes:
  - `5.3 Architecture Pattern Catalog`
  - `6 Runtime View`
  - `10 Quality Requirements and Test Strategy`

## Project Boundaries
- Use `Admitto.slnx` to determine active projects and module boundaries.
- Do not assume legacy projects under `src/Admitto.Application`, `src/Admitto.Domain`, or `src/Admitto.Infrastructure` are the default target unless a task explicitly asks for them.

## Non-Negotiable Conventions
- API endpoint handlers own the transaction boundary and commit the module unit of work.
- Command handlers must not inject or commit unit-of-work objects.
- For admin routes, FluentValidation runs in the endpoint filter before endpoint handler execution.
- Cross-module communication should happen via contracts/facades, not cross-module DbContext access.
- Domain events, module events, and integration events must follow the taxonomy in `/docs/README.md`.

## Testing Expectations
- Run targeted tests for the modules you changed.
- Run broader suites when shared components or host wiring are changed.

Suggested commands:
```bash
dotnet test tests/Admitto.Organization.Domain.Tests/Admitto.Organization.Domain.Tests.csproj
dotnet test tests/Admitto.Registrations.Domain.Tests/Admitto.Registrations.Domain.Tests.csproj
dotnet test tests/Admitto.Organization.Application.Tests/Admitto.Organization.Application.Tests.csproj
dotnet test tests/Admitto.Registrations.Application.Tests/Admitto.Registrations.Application.Tests.csproj
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```

## Documentation Hygiene
- Keep architecture details in `/docs/README.md` to avoid duplication.
- If a structural decision changes, update ADRs in `/docs/adrs` and link them from `/docs/README.md`.
