## Why

Admin JWT requests rely on `UserContextResolutionMiddleware` to resolve the authenticated principal into an Admitto domain user before authorization and endpoint execution. The current resolver accepts nullable route scope and can select a membership role without filtering it to the route team, which makes the authorization context less precise than the route being accessed.

This change tightens route-scope semantics while keeping the middleware as the single pre-authorization place that resolves and caches domain user context.

## What Changes

- Classify admin JWT route scope before resolving the user context: global, team-scoped, or event-scoped.
- Reject malformed or inconsistent route scope with `403 Forbidden` before authorization or endpoint handlers run.
- Keep `UserContextResolutionMiddleware` responsible for resolving and caching domain user context for JWT requests.
- Update `UserContextResolver` so team membership role is selected only for the requested route team.
- Keep event ownership validation centralized: event-scoped routes verify that `{eventId}` belongs to `{teamId}` before endpoint execution.
- Preserve API-key public request behavior; API-key requests continue to skip JWT user-context resolution and obtain team scope from the `team_id` claim.
- Update architecture documentation for the tightened middleware/resolver/authorization split.

## Capabilities

### New Capabilities
- `admin-user-context-resolution`: Covers how authenticated admin JWT requests are resolved to domain user context, how route scope is classified, and how that context feeds authorization and endpoint execution.

### Modified Capabilities
- `team-membership`: Team membership authorization must use the membership role for the requested route team, not an arbitrary membership belonging to the user.

## Impact

- `Admitto.Api/Auth/UserContextResolutionMiddleware.cs`: route-scope parsing/classification and fail-closed behavior.
- `Admitto.Api/Auth/UserContextResolver.cs`: route-scoped role selection and event ownership verification contract.
- `Admitto.Api/Auth/TeamMembershipAuthorizationHandler.cs` and `AdminAuthorizationHandler.cs`: should remain policy decision points over the pre-resolved context.
- `Admitto.Api/Auth/HttpContextUserContextAccessor.cs`: should continue returning cached context for JWT requests and synthesized API-key context for public requests.
- `docs/arc42/08-crosscutting-concepts.md` and possibly `docs/arc42/06-runtime-view.md`: document the tightened request-pipeline behavior.
- Tests: add or update coverage for team-scoped role selection, invalid route scope, event-without-team scope, event/team mismatch, and unchanged API-key behavior.
