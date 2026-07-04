## Context

Today the Admin UI (Next.js + BetterAuth) signs users in against an OIDC provider — Keycloak in development (containerized via AppHost) and Entra External ID in production — and forwards the resulting JWT to the .NET API. The API validates the bearer token using `Authentication:Bearer:Authority` and resolves the caller via `HttpContextUserContextAccessor`, which understands both the Entra `oid` claim and the standard `sub` claim. Identity-provider provisioning is abstracted behind `IUserDirectory`, with two concrete implementations (`KeycloakUserDirectory`, `MicrosoftGraphUserDirectory`) wired by config in `src/Admitto.Core/Organization/DependencyInjection.cs`. The Organization module owns a domain `User` aggregate that stores an opaque `ExternalUserId` linking the domain user to its IdP record.

Three forces are pushing this change:

1. Passwords are the worst part of the current UX and we want passkeys, which both Keycloak (already configured but unused) and Auth0 support natively.
2. Entra External ID is heavyweight for a small internal team; an Auth0 free tenant with a custom domain meets every functional need with far less operational friction.
3. Admitto is intended as a sample/starter app for other teams in the company; the dev-vs-prod IdP swap is a *feature* of the architecture and we want to keep that pattern visibly intact.

Constraints that frame the design:

- The API key authentication scheme used by external websites is unrelated and must not regress.
- E2E tests run against the Aspire-managed Keycloak container today and must keep working without manual passkey ceremonies.
- BetterAuth on the UI side stays as an OIDC client (not as the IdP itself); we explicitly considered and rejected making it the IdP.
- The Organization domain `User` model and its `ExternalUserId` indirection stay; only the *value* of `ExternalUserId` and the IdP that issues it changes.

## Goals / Non-Goals

**Goals:**
- Replace Entra External ID with Auth0 as the production IdP, behind the existing `IUserDirectory` abstraction.
- Make passkeys the only end-user authentication factor in both dev (Keycloak) and prod (Auth0).
- Keep the Admin UI's BetterAuth + generic-OAuth integration; only the OIDC discovery URL changes per environment.
- Provision a bootstrap admin user from configuration so a fresh deployment can sign in.
- Preserve the current architectural pattern: API issues invites via `IUserDirectory`, IdP owns credentials, API trusts JWTs from the configured authority.
- Keep E2E tests deterministic and fast by minting tokens via Keycloak's ROPC grant against seeded test users.

**Non-Goals:**
- Building any custom WebAuthn ceremony in Admitto code. Both IdPs handle the ceremony in their hosted UIs.
- Changing the API key scheme used by external websites.
- Replacing BetterAuth in the Admin UI or moving the IdP into the application process.
- Supporting multiple authentication factors (password fallback, TOTP, SMS, email magic links as a primary factor). Passkey-only is the policy; recovery is "delete user, re-invite."
- Supporting multiple production IdPs simultaneously. Auth0 is the single production choice; the abstraction exists for dev/prod swap, not multi-IdP routing.
- Account self-registration. All users arrive via invite from an existing admin or team owner.

## Decisions

### Decision 1: Auth0 (over alternatives) as the production IdP

Auth0's free tier includes 25,000 MAU, native passkey support, one custom domain, and a Management API rich enough for our invite/deprovision needs. Compared to Logto (no passkeys on free), Clerk (paid above tiny tiers), Zitadel/Authentik (still self-hosted), and BetterAuth-as-IdP (couples API to the UI process), Auth0 is the lowest-friction managed option that delivers passkeys without operational burden.

### Decision 2: Keep `IUserDirectory` and add `Auth0UserDirectory`

We keep the existing abstraction rather than collapse it. Two reasons:

1. The config-driven dev/prod swap is exactly the pattern Admitto wants to demonstrate as a starter app.
2. Keycloak vs. Auth0 are genuinely different APIs — keeping the seam means the Application layer never sees that difference.

`Auth0UserDirectory` calls the Auth0 Management API using a confidential M2M client. It is selected when `Authentication:Auth0` configuration is present, the same way Keycloak is selected today when its section is present.

### Decision 3: Passkey enrollment happens entirely in the IdP's hosted UI

Neither the API nor the Admin UI implements WebAuthn. The invite flow asks the IdP to create a user with a "register passkey" required action (Keycloak) or sends an account-setup ticket that lands on a passkey-enrollment page (Auth0). On enrollment completion, the IdP redirects back to the Admin UI and a normal OIDC session is established.

Alternative considered: implement the WebAuthn ceremony in our app via BetterAuth's passkey plugin. Rejected because it would couple the API to BetterAuth's storage, complicate E2E tests, and contradict the "IdP owns credentials" boundary.

### Decision 4: Bind `User.ExternalUserId` lazily on first authenticated request

When an admin invites someone, the API creates an Organization `User` row by email and asks `IUserDirectory.InviteAsync` to create the IdP user. The IdP-issued `sub` is not stored at that moment. On the user's first authenticated request, the API resolves the `User` by the `email` claim in the JWT and sets `ExternalUserId` from `sub` if it is null. Subsequent requests resolve by `ExternalUserId`.

This keeps the IdP free to assign IDs however it wants (including pre-existing accounts in Auth0 social connections, in theory), avoids a webhook from Auth0 → API, and uses no `User.Status` field — a null `ExternalUserId` is the "Invited" signal.

Alternatives considered:
- An IdP webhook updating the API on enrollment. Rejected as more moving parts for a small benefit.
- A `User.Status` enum. Rejected as redundant with the ExternalUserId nullability.

### Decision 5: JWT validation stays purely standards-based

The API's `JwtBearer` configuration remains driven by `Authentication:Bearer:Authority` and `ValidAudience`. Both Keycloak and Auth0 expose standard OIDC discovery and JWKS at well-known URLs, so the only per-environment change is config. The Entra-specific `oid` claim mapping is removed; `ClaimTypes.NameIdentifier` (`sub`) handles both Keycloak and Auth0 uniformly.

### Decision 6: Bootstrap admin via `IHostedService`

A new `BootstrapAdminInitializer : IHostedService` runs on API startup. If `Organization:BootstrapAdmin:EmailAddress` is configured and no Admin user with that email exists, it creates the Organization `User` (Admin role, no team membership) and calls `IUserDirectory.InviteAsync(email)`. The operation is idempotent: if the user already exists, nothing happens. This works identically in dev (invite email lands in MailDev → Keycloak passkey enrollment) and prod (Auth0 sends the ticket).

Alternative considered: a CLI seed command. Rejected because the dev story benefits from "aspire start just works."

### Decision 7: E2E tests use Keycloak ROPC against seeded test users

The dev `AdmittoRealm.json` is updated to:
- Enable `webauthn-register-passwordless` as a required action for normal users.
- Create a confidential test client (`admitto-tests`) with **direct access grants** enabled.
- Seed a small number of test users with known passwords and **no** required actions, so they can mint tokens via ROPC.

The E2E test infrastructure gains a `KeycloakTokenClient` that posts to the token endpoint with `grant_type=password`. Production never sees ROPC because the test client and test users only exist in the dev realm.

Alternative considered: minting JWTs directly in tests with a shared signing key. Rejected because it bypasses the real IdP code path, defeating the purpose of E2E.

### Decision 8: OpenAPI Bearer transformer becomes IdP-agnostic

`BearerSecuritySchemeTransformer` currently composes Keycloak-shaped URLs from the configured authority. It is rewritten to fetch the OIDC discovery document at startup (or lazily) and use the published `authorization_endpoint` / `token_endpoint`, so the same code works for any standards-compliant authority.

## Risks / Trade-offs

- **Auth0 vendor dependency in production** → Mitigated by sticking to OIDC standards and the `IUserDirectory` abstraction; a future swap to another OIDC provider only touches one infrastructure folder.
- **Free-tier limits (25k MAU, single custom domain, branded login)** → Acceptable for an internal team; documented in the deployment ADR so consumers of Admitto-as-starter-app know to evaluate.
- **Dev/prod IdP divergence (Keycloak vs Auth0)** → Existing risk, not new. We mitigate by keeping JWT validation purely OIDC-standard and the `IUserDirectory` interface narrow.
- **Lost-passkey recovery is "delete + re-invite"** → Accepted for an internal team. Documented in the user-management section of the spec; an admin must delete the user then re-invite, which provisions a fresh IdP account.
- **Bootstrap admin race on first startup** → The initializer must be idempotent and tolerate concurrent startups (e.g., during Aspire-led rolling restarts). Implementation uses an upsert pattern guarded by the email's uniqueness.
- **ROPC in dev Keycloak realm** → Test client is dev-only and confidential; production Auth0 tenant does not enable a password grant. Risk is that someone copies the realm config to prod — addressed by clearly labelling the test client and gating it via Aspire's environment-only resource creation.
- **Microsoft.Graph removal could break unrelated code** → Verified during exploration that Graph is used only by the user directory; CI build will catch any miss.

## Migration Plan

There is no production deployment yet, so this is a one-shot replacement rather than a phased migration. Order of work in dev:

1. Remove Microsoft Graph code, Entra config sections, Entra claim mapping. Build still green via the remaining Keycloak path.
2. Update Keycloak realm: enable WebAuthn passwordless required action; add ROPC test client and test users.
3. Update `HttpContextUserContextAccessor` to remove the Entra `oid` branch.
4. Make `BearerSecuritySchemeTransformer` discovery-document driven.
5. Implement `BootstrapAdminInitializer` and wire it into the Organization module.
6. Implement `Auth0UserDirectory` + `Auth0Options` + Management API client behind feature flag (`Authentication:Auth0` config presence).
7. Update `Organization/DependencyInjection.cs` selection logic: Keycloak vs. Auth0 only.
8. Update Admin UI BetterAuth env / discovery URL for the production deployment template; dev unchanged.
9. Update E2E test infrastructure to use the new ROPC token client; rewrite any tests that depended on Entra claim names.
10. Update `docs/arc42/` (solution strategy, runtime view, deployment view, ADR for "Auth0 + Passkeys").

There is no rollback because there is no live production. Local rollback during development is `git revert`.

## Open Questions

- Auth0 Management API client: use the official `Auth0.ManagementApi` NuGet, or a hand-rolled `HttpClient` wrapper? Lean toward the official package for fewer maintenance surprises; defer the call to implementation.
- Should the bootstrap admin initializer also enforce that the configured email always has the Admin role on every startup (self-healing if demoted), or only on creation? Lean toward "only on creation" to avoid surprising overrides; revisit if we hit the foot-gun in practice.
- Custom domain configuration in Auth0 will affect the Authority URL devs see in production OpenAPI; document an example value but do not hardcode.
