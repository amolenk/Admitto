# ADR-002: Minimal APIs with Feature-Sliced Endpoint Organization

## Status
Accepted

## Context
Admitto exposes multiple APIs (public and admin) with a growing number of endpoints per module. We want to avoid a centralized routing or controller structure that becomes hard to navigate and maintain. At the same time, we want to keep the HTTP layer thin and closely aligned with application use cases.

## Decision
- Use **ASP.NET Core Minimal APIs** instead of MVC controllers.
- Organize endpoints using **feature slicing**:
  - Each feature (use case) owns its endpoints, request/response DTOs, validation, and mapping.
  - Endpoints are defined close to the corresponding application logic.
- Route registration is done via **extension methods per use case** (e.g. `MapRegisterAttendee()`), which are invoked from the ApiHost composition root.

## Rationale
- Minimal APIs reduce boilerplate and keep focus on behavior rather than framework structure.
- Feature slicing improves cohesion: all logic related to a use case lives together.
- This structure scales better than controller-based organization when modules and features grow independently.

## Consequences
### Positive
- High discoverability of feature-related code.
- Reduced coupling between unrelated endpoints.
- Faster iteration and refactoring within a feature.

### Negative
- Requires clear conventions to maintain consistent routing.
- Less familiar to developers expecting controller-based organization.