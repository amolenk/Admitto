## Why

The public API currently has no authentication, meaning anyone who discovers the endpoint can call it freely. The event website (organizer's server-side application) needs to call Admitto securely so that only legitimate, team-authorized clients can interact with public registration flows.

## What Changes

- New `ApiKey` entity in the Organization module, scoped per team, stored as a SHA-256 hash (raw key shown once at creation)
- Admin API endpoints so team members can create, list, and revoke API keys for their team
- Custom `ApiKey` authentication scheme in `Admitto.Api` that reads the `X-Api-Key` header, hashes it, and resolves the associated team
- All public endpoints now require a valid API key; a route-scoped guard ensures the key's team matches the `{teamSlug}` in the URL
- Public API route prefix changes from `/` to `/api/`
- New **API Keys** tab in the Admin UI team settings area for managing keys

## Capabilities

### New Capabilities
- `team-api-keys`: Team-scoped API key management — create, list, revoke API keys for a team; used to authenticate the event website calling the public API
- `admin-ui-team-api-keys`: Admin UI page for managing team API keys — list active/revoked keys, create a new key (shown once), revoke an existing key

### Modified Capabilities
<!-- No existing spec-level requirements change. The public endpoint group gains an auth requirement but no existing attendee-registration or self-service behavior changes. -->

## Impact

- **Organization module**: New `ApiKey` entity and EF migration; new use cases `CreateApiKey`, `GetApiKeys`, `RevokeApiKey`; new `ValidateApiKey` query; `IOrganizationFacade` contract extended
- **Admitto.Api**: New `ApiKeyAuthenticationHandler`; second auth scheme registered alongside Keycloak JWT Bearer; `PublicEndpoints.cs` updated with `/api` prefix, `RequireAuthorization`, and `ApiKeyTeamScopeFilter`
- **Public routes**: All public routes shift from `/events/...` to `/api/events/...` — event websites must update their base URL
- **Admin routes**: Unchanged
- **Admin UI**: New page at `/teams/{teamSlug}/settings/api-keys`; new proxy routes; new nav entry in the team settings sidebar; Admin UI SDK regenerated after backend endpoints are added
- **No breaking changes** to admin endpoints or existing module domain logic
