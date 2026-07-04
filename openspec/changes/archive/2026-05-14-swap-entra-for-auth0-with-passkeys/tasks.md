## 1. Remove Entra External ID and Microsoft Graph

- [x] 1.1 Delete `src/Admitto.Core/Organization/Infrastructure/UserDirectories/MicrosoftGraph/` (`MicrosoftGraphUserDirectory`, `MicrosoftGraphUserManagementService`, options class, any helpers).
- [x] 1.2 Remove the Microsoft Graph branch and Entra config binding from `src/Admitto.Core/Organization/DependencyInjection.cs`.
- [x] 1.3 Remove the Entra `oid` (`http://schemas.microsoft.com/identity/claims/objectidentifier`) handling from `src/Admitto.Api/Auth/HttpContextUserContextAccessor.cs`, leaving only standard `sub` resolution.
- [x] 1.4 Remove `Microsoft.Graph` and any Entra-only NuGet references from `Admitto.Core.csproj` and verify no transitive consumer remains.
- [x] 1.5 Strip `Authentication:Microsoft*` / Entra sections from all `appsettings*.json` files and from any Aspire configuration that injected them.
- [x] 1.6 Run `dotnet build` and the architecture tests (`dotnet test tests/Admitto.Core.ArchTests/...`) to confirm no broken references remain.

## 2. Update the Admin UI auth glue (no IdP move)

- [x] 2.1 Confirm `src/Admitto.UI.Admin/app/lib/auth.ts` and `.env.local.example` only reference `BETTER_AUTH_AUTHORITY`, `CLIENT_ID`, `CLIENT_SECRET`, `SCOPES`; remove Entra-specific examples and add an Auth0 example block (commented).
- [x] 2.2 Add a documented `.env.production.example` (or section in README) showing the Auth0 custom-domain values.

## 3. Make the API IdP-agnostic at the edge

- [x] 3.1 Rewrite `src/Admitto.Api/OpenApi/BearerSecuritySchemeTransformer.cs` to fetch the OIDC discovery document from the configured `Authentication:Bearer:Authority` and use its `authorization_endpoint` / `token_endpoint`, removing all hardcoded Keycloak path assumptions.
- [x] 3.2 Verify `src/Admitto.Api/DependencyInjection.cs` JWT bearer setup needs no provider-specific changes (Authority + ValidAudience drive everything).
- [x] 3.3 Add a unit test (or extend an existing one) that asserts the OpenAPI security definitions are populated correctly from a stub discovery document.

## 4. Add the Auth0 user directory implementation

- [x] 4.1 Create `src/Admitto.Core/Organization/Infrastructure/UserDirectories/Auth0/Auth0Options.cs` binding `Authentication:Auth0` (Domain, ClientId, ClientSecret, Audience).
- [x] 4.2 Add a typed Auth0 Management API client (prefer the official `Auth0.ManagementApi` NuGet) wired with M2M client-credentials token caching.
- [x] 4.3 Implement `Auth0UserDirectory : IUserDirectory` covering invite (create user + create password-change ticket configured for passkey enrollment), deprovision, and any other current `IUserDirectory` members.
- [x] 4.4 Implement `Auth0UserManagementService` mirroring the structure of `KeycloakUserManagementService` if a separate service is part of the existing pattern.
- [x] 4.5 Update `src/Admitto.Core/Organization/DependencyInjection.cs` so `Authentication:Auth0` selects the Auth0 implementation and `Authentication:Keycloak` selects Keycloak; throw a clear startup exception if neither is configured.
- [x] 4.6 Add focused unit tests for the Auth0 client adapter (request shape, error mapping) and the directory's invite + deprovision behaviors.

## 5. Bootstrap admin on startup

- [x] 5.1 Add `Organization:BootstrapAdmin:EmailAddress` configuration binding under the Organization module.
- [x] 5.2 Implement `BootstrapAdminInitializer : IHostedService` that, on startup, ensures an Admin `User` with the configured email exists and triggers `IUserDirectory.InviteAsync` if `ExternalUserId` is null. Idempotent across restarts; safe under concurrent startup.
- [x] 5.3 Register the initializer in `Organization/DependencyInjection.cs` only when the bootstrap email is configured.
- [x] 5.4 Add an integration test using the dev Keycloak fixture asserting bootstrap is idempotent and creates exactly one user/invite.

## 6. Lazy bind ExternalUserId on first authenticated request

- [x] 6.1 Add an Application-layer service (or extend `HttpContextUserContextAccessor`/its consumer) that resolves the calling `User`: first by `ExternalUserId`, falling back to `EmailAddress` to bind `ExternalUserId` when null.
- [x] 6.2 Reject the request with 403 when the JWT's email does not match any invited user, and when a stored `ExternalUserId` exists but does not match the JWT's `sub`.
- [x] 6.3 Cover the binding logic with domain/integration tests (first sign-in binds, second sign-in resolves by id, mismatched email is rejected, mismatched stored id is rejected).

## 7. Keycloak realm: passkeys + ROPC test client

- [x] 7.1 Update `src/Admitto.AppHost/KeycloakConfiguration/AdmittoRealm.json` to enable `webauthn-register-passwordless` as a default required action for normal users and to leave password as disabled for production-shaped accounts.
- [x] 7.2 Add a confidential test client `admitto-tests` with `directAccessGrantsEnabled: true` and add seeded test users (with known passwords and no required actions) needed by the E2E suite.
- [x] 7.3 Verify `aspire start` boots Keycloak with the updated realm and that MailDev still receives invitation emails.

## 8. End-to-end test infrastructure

- [x] 8.1 Add `KeycloakTokenClient` (or equivalent helper) to the API tests project that obtains a JWT via the ROPC grant against the dev realm using the seeded test users.
- [x] 8.2 Update `EndToEndTestBase` / `EndToEndTestEnvironment` so existing API clients use the new token client; remove any password-based or Entra-specific test setup.
- [x] 8.3 Update tests that previously relied on Entra `oid` claim assertions to use the standard `sub` claim path.
- [x] 8.4 Run `tests/Admitto.Api.Tests/bin/Debug/net10.0/Admitto.Api.Tests` and confirm the full E2E suite is green.

## 9. Documentation

- [x] 9.1 Add a new ADR under `docs/arc42/09-architecture-decisions.md` (or its companion file) titled "Auth0 + Passkeys for production identity" capturing the decision and alternatives considered.
- [x] 9.2 Update `docs/arc42/04-solution-strategy.md`, `06-runtime-view.md`, and `07-deployment-view.md` to reflect Keycloak (dev) + Auth0 (prod) and the passkey-only sign-in flow.
- [x] 9.3 Update README and any `docs/` setup pages to describe configuring an Auth0 tenant (M2M app scopes, API resource, custom domain, passkey-only enrollment policy) and the bootstrap-admin appsetting.
- [x] 9.4 Remove all references to Entra External ID / Microsoft Graph from the docs.

## 10. Verification

- [x] 10.1 `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` is green.
- [x] 10.2 `dotnet test tests/Admitto.Core.DomainTests/...` and `dotnet test tests/Admitto.Core.IntegrationTests/...` are green.
- [x] 10.3 The Admin UI builds (`cd src/Admitto.UI.Admin && pnpm build`).
- [x] 10.4 Manual smoke test in `aspire start --isolated`: bootstrap admin email arrives in MailDev, link opens Keycloak passkey enrollment, enrollment completes, Admin UI loads with an active session, API requests succeed.
- [x] 10.5 `openspec validate swap-entra-for-auth0-with-passkeys --strict` passes.
