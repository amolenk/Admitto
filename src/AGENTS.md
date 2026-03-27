# Source Code Agent Guide

## Scope
This file applies to `/src`.

## First Reference
- Read `/docs/README.md` before modifying source code.
- Follow `/docs/README.md` section `5.3 Architecture Pattern Catalog`.

## HTTP and Use Case Rules
- Keep minimal API endpoints feature-sliced (`UseCases/.../*HttpEndpoint.cs`).
- Use `OrganizationScope` for route-derived organization context where applicable.
- For write endpoints:
  - map request DTO to command
  - dispatch via `IMediator`
  - commit the keyed module `IUnitOfWork` in the endpoint
- Do not commit transactions inside individual command handlers.

## Validation Rule
- Admin routes run `ValidationFilter` at the route group level.
- Endpoint handlers should assume validated request DTOs on admin routes and avoid duplicate validation logic.

## Persistence Rule
- Use module write-store abstractions (`IOrganizationWriteStore`, `IRegistrationsWriteStore`) in handlers.
- Keep data ownership inside module boundaries (schema-per-module).
- Resolve `IUnitOfWork` by module key (`OrganizationModuleKey.Value`, `RegistrationsModule.Key`).

## Messaging and Events Rule
- Domain events live in `Domain/DomainEvents/` within each module project.
- Module events live in `Application/ModuleEvents/` within each module project.
- Integration events live in `*.Contracts` projects under `IntegrationEvents/`.
- Map domain events via module `*MessagePolicy` classes; do not hand-roll ad hoc event translation in handlers.

## Cross-Module Rule
- Cross-module reads should go through contracts/facades (for example `IOrganizationFacade`), not direct DbContext access across modules.

## When You Change Architecture
- Update `/docs/README.md`.
- If the change is an architecture decision, add or update an ADR in `/docs/adrs`.
