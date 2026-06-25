## Context

Admin endpoints use JWT bearer authentication, then `UserContextResolutionMiddleware` resolves the authenticated principal into a domain `UserContextDto` before authorization runs. Authorization handlers read that pre-resolved context through `IUserContextAccessor`, and several endpoints, command handlers, and persistence interceptors also depend on the same accessor during request execution.

The middleware currently passes nullable `teamId` and `eventId` values to `UserContextResolver`. That correctly supports global, team-scoped, and event-scoped routes, but the implicit nullable pair makes invalid combinations easy to overlook. The resolver also projects all memberships and selects a role without filtering to the route team, which can authorize a user based on membership in a different team.

Admin routes currently have three valid scope shapes:

```text
/admin/teams                                      global scope
/admin/teams/{teamId}...                         team scope
/admin/teams/{teamId}/events/{eventId}...        event scope
```

Public API-key routes are separate. They skip JWT user-context resolution and derive team scope from the authenticated API-key `team_id` claim.

## Goals / Non-Goals

**Goals:**

- Keep `UserContextResolutionMiddleware` as the single pre-authorization resolver for JWT admin requests.
- Make route scope explicit as global, team-scoped, or event-scoped.
- Reject invalid route scope before authorization or endpoint handlers run.
- Select team membership role from the route team only.
- Keep event ownership validation centralized before endpoint execution.
- Preserve plain authenticated global endpoints such as `GET /admin/teams`.

**Non-Goals:**

- Removing `UserContextResolutionMiddleware`.
- Changing public API-key authentication or public endpoint route shape.
- Changing team membership domain rules, role hierarchy, or admin bypass semantics.
- Moving transaction boundaries or validation behavior.

## Decisions

### Keep Middleware For Domain User Context Resolution

`UserContextResolutionMiddleware` remains because authorization handlers are not the only consumers of `IUserContextAccessor.Current`. Plain authenticated endpoints, command handlers, and audit persistence need the context after authorization has completed. Resolving once before authorization gives a clear guarantee: for JWT admin requests, endpoint code can read `Current` without triggering database lookups or async work.

Alternative considered: resolve inside `AdminAuthorizationHandler` and `TeamMembershipAuthorizationHandler`. This misses endpoints that use plain `.RequireAuthorization()` and still need `IUserContextAccessor.Current`, such as team listing.

Alternative considered: make `IUserContextAccessor` lazy/async. This spreads resolution into endpoints, handlers, and interceptors and weakens the fail-closed request boundary.

### Classify Route Scope Before Calling The Resolver

The middleware should parse route values into one of these valid states:

```text
Global
Team(teamId)
Event(teamId, eventId)
```

The invalid state `eventId` without `teamId`, or any unparsable route value, should result in `403 Forbidden`. This keeps resolver inputs valid and documents route-scope intent explicitly.

Alternative considered: continue passing nullable values. This is minimal but preserves the invalid-state ambiguity that caused the current discussion.

### Resolve Membership Role For The Route Team Only

When scope is team or event, the resolver should select the user's membership role for that specific `teamId`. When scope is global, the role should remain absent and only identity/admin state should be available. Admin users continue to bypass team-membership authorization through `IsAdmin`.

Alternative considered: return all memberships in `UserContextDto` and let authorization handlers choose. This would enlarge the shared contract and duplicate route-scope handling in authorization.

### Keep Event Ownership Validation In The Resolver

For event-scoped routes, the resolver should verify that the route `eventId` belongs to the route `teamId` using Organization's ticketed-event tracking state. Non-admin mismatches return no context so the middleware returns `403 Forbidden` before endpoint authorization or handler execution.

Alternative considered: each endpoint or handler validates parent event/team scope. That duplicates a cross-cutting guard and increases the chance of missed checks.

## Risks / Trade-offs

- Existing tests may rely on nullable resolver calls → Update tests to call through explicit global/team/event scope or adapt helper methods.
- Plain authenticated global endpoints still need user context → Keep global scope valid and ensure unknown users still fail before endpoint execution.
- Admin bypass of event/team mismatch may obscure bad URLs → Preserve current documented behavior, but keep it explicit in tests.
- Route parsing in middleware can drift from route conventions → Add focused API or integration tests for malformed and inconsistent route scope.
