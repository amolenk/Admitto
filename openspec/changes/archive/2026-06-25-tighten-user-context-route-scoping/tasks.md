## 1. Route Scope Model

- [x] 1.1 Add an explicit admin route-scope representation for global, team, and event scopes.
- [x] 1.2 Update `UserContextResolutionMiddleware` to classify route values into the route-scope representation.
- [x] 1.3 Make middleware return `403 Forbidden` for invalid route scope, including unparsable route values and `eventId` without `teamId`.

## 2. User Context Resolution

- [x] 2.1 Update `UserContextResolver` to accept explicit route scope instead of an ambiguous nullable pair.
- [x] 2.2 Filter the resolved membership role to the requested route team for team and event scopes.
- [x] 2.3 Keep global-scope resolution valid with no membership role selected.
- [x] 2.4 Preserve event ownership validation for event scope and admin bypass behavior.

## 3. Authorization And Accessor Behavior

- [x] 3.1 Confirm `AdminAuthorizationHandler` and `TeamMembershipAuthorizationHandler` continue using pre-resolved context without performing route parsing.
- [x] 3.2 Confirm `HttpContextUserContextAccessor` continues to return cached JWT context and synthesized API-key context.
- [x] 3.3 Preserve public API-key request behavior under `/api/...`.

## 4. Tests

- [x] 4.1 Update resolver tests for global, team, and event route scopes.
- [x] 4.2 Add coverage proving membership in another team does not authorize the requested route team.
- [x] 4.3 Add middleware or API-level coverage for invalid route scope returning `403 Forbidden`.
- [x] 4.4 Add or preserve coverage for event/team mismatch and admin event-scope bypass.
- [x] 4.5 Add or preserve coverage that API-key public requests skip JWT user-context resolution.

## 5. Documentation And Verification

- [x] 5.1 Update `docs/arc42/08-crosscutting-concepts.md` with the tightened middleware/resolver/authorization split.
- [x] 5.2 Update `docs/arc42/06-runtime-view.md` if the admin request flow diagram or text needs the pre-authorization context-resolution step.
- [x] 5.3 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 5.4 Run targeted auth/API/core tests covering the changed behavior.
