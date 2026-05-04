## Context

The public API currently maps at the root (`/`) with no authentication. Any party that discovers an endpoint URL can call it freely. The event website — the organizer's server-side application — is the intended caller for all public registration flows (OTP request/verify, register, cancel, change tickets). Without authentication, there is no way to attribute requests to a specific team, enforce per-team rate limiting, or prevent abuse by unrelated third parties.

The admin API is already protected by a Keycloak-issued JWT Bearer token. Attendees do not have Keycloak accounts, and the event website is a server process, not a user-facing browser session. A second authentication scheme is required that works for server-to-server calls.

## Goals / Non-Goals

**Goals:**
- Add a team-scoped API key authentication scheme to the public API
- Provide admin CRUD endpoints so team members can create, list, and revoke their own API keys
- Enforce that an API key issued for Team A cannot call endpoints for Team B
- Move public routes from `/` to `/api/` for clarity and namespace separation

**Non-Goals:**
- Per-event API key scoping (keys are team-scoped; all events of a team share the same key set)
- API key expiry / time-limited keys (out of scope for the initial implementation)
- Client-side (browser/SPA) access to public endpoints without an API key
- Admin UI for API key management (admin API endpoints only for now)
- Replacing the existing Keycloak JWT Bearer scheme on admin endpoints

## Decisions

### Decision 1: API keys (not OAuth / Keycloak tokens) for the public scheme

The public API is called by the event website's server process, not by human users. OAuth authorization code flow and Keycloak sessions are designed for user-facing applications. Issuing Keycloak client credentials to each event website would require provisioning a Keycloak client per team, which is operationally heavy and couples the event website deployment to the Admitto identity provider.

Static API keys (pre-shared secrets) are the standard choice for server-to-server APIs where the caller is a trusted backend process. They are simple to generate, rotate, and revoke without IdP coordination.

**Alternative considered:** Keycloak client-credentials grant (machine-to-machine OAuth). Rejected: requires a Keycloak client per team, adds a token-refresh cycle, and couples the event website to Admitto's IdP configuration.

### Decision 2: `X-Api-Key` header

`X-Api-Key: <raw-key>` is widely used and recognizable for API key auth. The `Authorization: ApiKey <key>` form is also common but adds parsing complexity for no benefit in a single-scheme scenario.

### Decision 3: SHA-256 hash stored; raw key shown once

The full raw key is never stored. On creation, the server returns the raw key exactly once. After that, only a SHA-256 hash is persisted. The first 8 characters of the raw key are stored as `KeyPrefix` for identification in listings (e.g., "which key is my production key?").

This mirrors the GitHub/Stripe model and is a standard security practice: a compromised database does not leak usable keys.

**Alternative considered:** Storing the raw key encrypted (AES-256). Rejected: if the encryption key is compromised the entire key store is exposed; SHA-256 (one-way) provides a stronger security boundary.

### Decision 4: `ApiKey` as a standalone entity, not inside the `Team` aggregate

Loading API keys every time a `Team` aggregate is loaded for other operations (name change, archiving, event counter updates) would be wasteful as the key list can grow. A standalone entity with a `TeamId` FK is the standard EF pattern for large child collections that need independent queryability.

**Alternative considered:** Keys as a collection owned by `Team`. Rejected: would load all keys on every Team hydration, creating unnecessary DB reads for operations unrelated to keys.

### Decision 5: Multiple keys per team, no expiry

Teams need to rotate keys without downtime (create new key, update event website, revoke old key). Allowing multiple active keys simultaneously enables zero-downtime rotation.

Expiry is not included in V1 — it adds complexity (what happens when a key expires mid-request?) and is not required to meet the security goal. Teams can manually revoke stale keys.

### Decision 6: Custom `ApiKeyAuthenticationHandler` as second ASP.NET Core scheme

ASP.NET Core's multi-scheme auth supports registering additional `IAuthenticationHandler` implementations alongside the existing JWT Bearer scheme. The `ApiKeyAuthenticationHandler` inspects `X-Api-Key`, hashes the value, calls `IOrganizationFacade.ValidateApiKeyAsync`, and on success returns an authenticated principal carrying a `TeamId` claim.

The public endpoint group calls `.RequireAuthorization(policy => policy.AddAuthenticationSchemes("ApiKey").RequireAuthenticatedUser())` to enforce the scheme explicitly, so the existing Keycloak scheme remains the default for admin endpoints.

**Alternative considered:** Middleware (before auth pipeline). Rejected: middleware cannot integrate with ASP.NET Core's authorization policy system; a proper handler enables `.RequireAuthorization()` and policy-based access control consistently.

### Decision 7: Route-team scope enforced by an endpoint filter

The API key resolves to a `TeamId`. All public routes include `{teamSlug}` in the path. An `ApiKeyTeamScopeFilter` reads `{teamSlug}` from route values, resolves it to a `TeamId` via `IOrganizationFacade.GetTeamIdAsync`, and compares it to the `TeamId` claim on the principal. Mismatch → 403.

This prevents an API key holder of Team A from calling registration endpoints for Team B, even if they guess the slug.

### Decision 8: `/api` route prefix for public endpoints

Moving from `/` to `/api/` makes the namespace explicit and consistent with REST conventions. It avoids potential path collisions with future non-API routes (health checks, OpenAPI, etc.) and clearly separates the public surface from admin (`/admin`).

### Decision 9: Admin UI — new "API Keys" tab in team settings

The existing team settings layout (`/teams/{teamSlug}/settings/layout.tsx`) uses a sidebar nav with tabs (General, Members, Email, Danger Zone). The API Keys page fits naturally as a new tab in this layout — a new Next.js page at `/teams/{teamSlug}/settings/api-keys/page.tsx` and a proxy route at `/api/teams/{teamSlug}/api-keys`.

The one-time key display uses the same `AlertDialog` pattern already used in the Danger Zone page. A copy-to-clipboard button and a prominent "save this now" warning are shown inside the dialog before the user can dismiss it.

The Admin UI SDK **must be regenerated** after the backend endpoints are added (per project convention: `aspire start --isolated → aspire wait api → curl spec → pnpm openapi-ts`). All proxy routes use the generated SDK functions.

## Risks / Trade-offs

- [Risk: Breaking change for event websites already calling the public API] → The route prefix change is a breaking URL change. Mitigation: document the new base URL clearly; since the public API has no callers in production yet, this is low-risk.
- [Risk: API key exfiltration from event website environment] → If the event website's server is compromised, the API key is exposed. Mitigation: revoke-and-reissue flow; short-lived key rotation guidance; this is the accepted risk for any pre-shared secret scheme.
- [Risk: SHA-256 preimage attack on key hash] → SHA-256 is not a password hash (bcrypt/Argon2). For a 32-byte random key (256 bits of entropy), brute-force and preimage attacks are computationally infeasible. A slower hash would add unnecessary latency to every authenticated request.
- [Risk: DB roundtrip on every public request] → Key validation requires one indexed SELECT per request. Mitigation: the `KeyHash` column is indexed (unique); this is a single-row keyed lookup with sub-millisecond latency at scale. Caching can be added later if profiling shows it necessary.

## Migration Plan

1. Add `ApiKeys` table via EF migration — additive, no existing data affected
2. Deploy API with new scheme and `/api` prefix — public endpoints return 401 until a key is configured; admin endpoints unaffected
3. Event website operators create an API key via the admin API and update their base URL to `/api/`
4. No rollback complexity — removing the auth requirement restores the previous behavior; the table migration can be left in place
