## 1. Domain — ApiKey Entity

- [x] 1.1 Add `ApiKeyId` typed value object in `Admitto.Module.Organization/Domain/ValueObjects/ApiKeyId.cs` (wraps Guid, factory `New()`)
- [x] 1.2 Add `ApiKey` entity in `Admitto.Module.Organization/Domain/Entities/ApiKey.cs` with fields: `ApiKeyId Id`, `TeamId TeamId`, `string Name`, `string KeyPrefix`, `string KeyHash`, `DateTimeOffset CreatedAt`, `string CreatedBy`, `DateTimeOffset? RevokedAt`; computed `bool IsActive => RevokedAt is null`; factory `ApiKey.Create(teamId, name, keyPrefix, keyHash, createdAt, createdBy)`; method `void Revoke(DateTimeOffset revokedAt)` (idempotent)

## 2. Infrastructure — Persistence

- [x] 2.1 Add `ApiKeyEntityConfiguration` (EF) in Organization module: table `ApiKeys` in schema `organization`, unique index on `KeyHash`, FK to `Teams`
- [x] 2.2 Add `DbSet<ApiKey> ApiKeys` to `OrganizationDbContext`
- [x] 2.3 Add lookup method `Task<(Guid TeamId, Guid KeyId)?> FindActiveApiKeyByHashAsync(string keyHash, CancellationToken ct)` to `OrganizationDbContext` / `IOrganizationWriteStore`
- [x] 2.4 Add EF Core migration for the `ApiKeys` table

## 3. Application — API Key Management Use Cases

- [x] 3.1 Add `CreateApiKeyCommand(TeamId, Name, KeyPrefix, KeyHash, CreatedBy)` and `CreateApiKeyHandler` (persists `ApiKey`, returns `Guid keyId`) under `Application/UseCases/ApiKeyManagement/CreateApiKey/`
- [x] 3.2 Add `CreateApiKeyHttpRequest { Name }`, `CreateApiKeyValidator` (Name required, max 100 chars), `CreateApiKeyHttpResponse { Id, Name, KeyPrefix, Key }` and `CreateApiKeyHttpEndpoint` at `POST /admin/teams/{teamSlug}/api-keys` → 201; endpoint generates the raw key (32-byte Base64url), computes prefix and SHA-256 hash, dispatches command
- [x] 3.3 Add `GetApiKeysQuery(TeamId)`, `GetApiKeysHandler` (query all keys for team), `ApiKeyListItemDto { Id, Name, KeyPrefix, CreatedAt, CreatedBy, RevokedAt }` and `GetApiKeysHttpEndpoint` at `GET /admin/teams/{teamSlug}/api-keys` → 200 under `Application/UseCases/ApiKeyManagement/GetApiKeys/`
- [x] 3.4 Add `RevokeApiKeyCommand(TeamId, ApiKeyId)`, `RevokeApiKeyHandler` (load `ApiKey`, verify team ownership, call `Revoke()`, save; 404 if not found/wrong team, 409 if already revoked) and `RevokeApiKeyHttpEndpoint` at `DELETE /admin/teams/{teamSlug}/api-keys/{keyId}` → 204 under `Application/UseCases/ApiKeyManagement/RevokeApiKey/`
- [x] 3.5 Add `ValidateApiKeyQuery(KeyHash)` → `Guid? teamId` and `ValidateApiKeyHandler` (calls `FindActiveApiKeyByHashAsync`) under `Application/UseCases/ApiKeyManagement/ValidateApiKey/`

## 4. Contracts — Facade Extension

- [x] 4.1 Add `ValidateApiKeyAsync(string keyHash, CancellationToken ct) → ValueTask<Guid?>` to `IOrganizationFacade` in the Contracts project
- [x] 4.2 Implement in `OrganizationFacade` by dispatching `ValidateApiKeyQuery` via mediator

## 5. API — Authentication Infrastructure

- [x] 5.1 Add `ApiKeyAuthenticationHandler` in `Admitto.Api/Auth/`: reads `X-Api-Key` header, SHA-256 hashes the raw value, calls `IOrganizationFacade.ValidateApiKeyAsync`; on success returns authenticated `ClaimsPrincipal` with `TeamId` claim; on failure returns `AuthenticateResult.NoResult()`
- [x] 5.2 Add `ApiKeyAuthenticationOptions` (empty, required by scheme registration) in `Admitto.Api/Auth/`
- [x] 5.3 Register the `"ApiKey"` scheme in `DependencyInjection.AddApiAuthentication()` via `.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { })` alongside the existing JWT Bearer scheme

## 6. API — Public Endpoint Security and Route Prefix

- [x] 6.1 In `PublicEndpoints.cs`, change `MapGroup("")` to `MapGroup("/api")`
- [x] 6.2 Add `.RequireAuthorization(policy => policy.AddAuthenticationSchemes("ApiKey").RequireAuthenticatedUser())` to the public endpoint group in `PublicEndpoints.cs`
- [x] 6.3 Add `ApiKeyTeamScopeFilter` in `Admitto.Api/` (or `Admitto.ApiService/`): reads `{teamSlug}` from route values, resolves to `TeamId` via `IOrganizationFacade.GetTeamIdAsync`, compares against `TeamId` claim on `HttpContext.User`; returns 403 `ProblemDetails` on mismatch
- [x] 6.4 Apply `ApiKeyTeamScopeFilter` to the public endpoint group in `PublicEndpoints.cs`

## 7. API — Admin Endpoint Wiring

- [x] 7.1 Wire the three API key admin endpoints into `OrganizationApiEndpoints.MapOrganizationAdminEndpoints()` under the `/{teamSlug}` group:
  ```csharp
  team.MapGroup("/api-keys")
      .MapCreateApiKey()
      .MapGetApiKeys()
      .MapRevokeApiKey();
  ```

## 8. Admin UI — SDK Regeneration

- [x] 8.1 Start Aspire (`aspire start --isolated`), wait for API (`aspire wait api`), fetch the OpenAPI spec and run `pnpm openapi-ts` to regenerate `app/lib/admitto-api/generated/`

## 9. Admin UI — Proxy Routes

- [x] 9.1 Add `GET /api/teams/[teamSlug]/api-keys/route.ts` proxy route using the generated `getApiKeys` SDK function
- [x] 9.2 Add `POST /api/teams/[teamSlug]/api-keys/route.ts` proxy route using the generated `createApiKey` SDK function
- [x] 9.3 Add `DELETE /api/teams/[teamSlug]/api-keys/[keyId]/route.ts` proxy route using the generated `revokeApiKey` SDK function

## 10. Admin UI — API Keys Page

- [x] 10.1 Add "API Keys" entry (with `Key` icon and description "Manage API keys") to `navItems` in `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/settings/layout.tsx`
- [x] 10.2 Create `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/settings/api-keys/page.tsx` that fetches and lists all API keys via the proxy route, showing name, prefix, created date, creator, and status (Active badge or revoked date); include empty state
- [x] 10.3 Add "Create API Key" button that opens a dialog with a name input; on submit, calls the POST proxy, then shows the one-time key display dialog (raw key + copy-to-clipboard + warning); on dismiss, refreshes the list
- [x] 10.4 Add "Revoke" action (visible only for active keys) that opens a confirmation `AlertDialog` warning that the key stops working immediately; on confirm, calls the DELETE proxy and refreshes the list

## 11. Tests

- [x] 11.1 E2E test: public endpoint without `X-Api-Key` header returns 401 (SC009)
- [x] 11.2 E2E test: public endpoint with unrecognized API key returns 401 (SC010)
- [x] 11.3 E2E test: public endpoint with revoked API key returns 401 (SC011)
- [x] 11.4 E2E test: API key for Team A calling Team B's endpoint returns 403 (SC012)
- [x] 11.5 E2E test: valid API key for correct team allows request to proceed (SC013)
- [x] 11.6 E2E test: create → list → revoke lifecycle via admin endpoints (SC001–SC008)
