## Context

Public endpoints are currently registered under `/api/teams/{teamId:guid}/events/{eventId:guid}`. API-key authentication already looks up the active key's owning team and emits it as a `team_id` claim. The public API group then applies `ApiKeyTeamScopeFilter` to compare the route `teamId` with the claim.

That means the same team scope is represented twice: once in the API key and once in the URL. The route value exists only so the server can compare it with the authenticated key owner, and every public endpoint receives `Guid teamId` only to pass the value into commands and queries.

The public endpoints live in `Admitto.Core`, while `ApiKeyAuthenticationHandler` and `ApiKeyTeamScopeFilter` live in `Admitto.Api`. Any shared team-claim access must preserve this dependency direction.

## Goals / Non-Goals

**Goals:**

- Remove the team segment from all public API URLs while keeping the `/api` prefix.
- Require a valid active `X-Api-Key` for every public endpoint under `/api/...`.
- Resolve public endpoint `TeamId` from the authenticated API-key principal.
- Remove `ApiKeyTeamScopeFilter` because the route no longer carries team scope.
- Keep event IDs explicit in public URLs as `/api/events/{eventId}/...`.
- Regenerate OpenAPI clients after the backend route contract changes.

**Non-Goals:**

- Changing admin routes; admin endpoints continue to use explicit team/event route scope and team-membership authorization.
- Introducing slug-based public routing.
- Changing API key creation, storage, hashing, revocation, or prefix display behavior.
- Adding backward-compatible public aliases for the old `/api/teams/{teamId}/events/{eventId}/...` routes.
- Changing OTP verification-token semantics, registration signatures, waitlist coupon behavior, or business authorization rules beyond API-key protection.

## Decisions

### Public routes use `/api/events/{eventId}/...`

The public route group will move from `/api/teams/{teamId:guid}/events/{eventId:guid}` to `/api/events/{eventId:guid}`. This preserves the explicit event resource in the URL while avoiding duplicate team scope.

Alternative considered: `/api/teams/current/events/{eventId}/...`. This still encodes team scope in the URL and adds a non-resource segment without improving security or clarity.

Alternative considered: `/api/events/{teamId}/{eventId}/...`. This keeps the problem unchanged.

### API-key team scope is read from shared auth code

Endpoint code in `Admitto.Core` needs a dependency-safe way to obtain the API-key team. The implementation should expose API-key claim constants and parsing through shared code, for example under `Admitto.Core.Shared.Application.Auth`, so both `Admitto.Api` and `Admitto.Core` can depend on it.

Alternative considered: public endpoints directly reference `ApiKeyAuthenticationHandler.TeamIdClaimType`. This would make `Admitto.Core` depend on `Admitto.Api`, violating the current host/core dependency direction.

Alternative considered: use `IUserContextAccessor.Current.UserId` as the team ID for API-key requests. This is currently how API-key user context is synthesized, but the field name is semantically wrong for team scope and would make endpoint correctness rely on an implicit convention.

### Delete `ApiKeyTeamScopeFilter`

After the route no longer contains `teamId`, the filter has no comparison to perform. Authentication itself is sufficient to guarantee that a valid active key belongs to exactly one team, and endpoints will use that authenticated team ID.

Alternative considered: keep the filter as a claim-presence guard. This duplicates authentication failure handling and is less direct than making the team-claim extraction fail closed in endpoint/auth helper code.

### All `/api/...` public endpoints require API key authentication

The public API route group remains protected by the `ApiKey` authentication scheme. Endpoints that currently express anonymous intent, such as public coupon details and QR-code retrieval, will be brought under the same API-key requirement.

Alternative considered: split anonymous routes into another group. This contradicts the selected product/security requirement that all public endpoints must be protected by API key.

## Risks / Trade-offs

- **Breaking public client URLs** -> This is an intentional breaking API change. Update OpenAPI output, generated SDKs, fixtures, and external integration documentation/specs together.
- **Accidental dependency inversion** -> Keep claim constants/parsing in shared Core code, not in `Admitto.Api` types consumed by Core endpoint classes.
- **Missing `team_id` claim causes ambiguous failures** -> Make team-claim extraction fail closed with 401/403-style problem behavior before commands/queries run.
- **Wrong event with valid team key returns business-level not found** -> Handlers should continue passing both `TeamId` and `TicketedEventId` into commands/queries so event ownership checks remain enforced by existing team filters.
- **Generated clients become stale** -> Regenerate the Admin UI SDK from the Aspire-published OpenAPI spec after backend route changes.
