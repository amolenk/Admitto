## Context

Admitto uses Keycloak as the identity provider in production and local development. The Admin UI delegates sign-in to Keycloak through Better Auth/OIDC, while the API validates JWT bearer tokens from the configured authority. Organization owns Admitto users and links them to IdP identities through `User.ExternalUserId`.

The current implementation and documentation are inconsistent. The passkey spec still reflects an Auth0-era design, the accepted Keycloak ADR says passkey-only is not currently assumed, the production realm import still uses the built-in browser password form, and bootstrap admin provisioning relies on async external-user registration after creating the domain user. This makes the first production login unclear: the operator has no password but the normal login page can still ask for one.

Local development has a different need. Developers and tests should keep using predictable email/password credentials and direct-grant test clients, because this keeps local setup offline and automation-friendly.

Keycloak also needs to initiate emails for passkey enrollment and account actions. Configuring SMTP and final email templates separately in Keycloak would duplicate Admitto's email delivery concerns, create a second template system, and require a second operational mail path. Instead, Keycloak should send structured identity-email requests to Admitto, and Admitto should render and deliver the actual email.

## Goals / Non-Goals

**Goals:**

- Make production Admin UI authentication passkey-only for Admitto users.
- Preserve local development and end-to-end testing with email/password credentials.
- Make bootstrap admin provisioning produce a usable passkey enrollment path without manual Keycloak console steps.
- Route Keycloak identity-email events through Admitto so production does not require independent Keycloak SMTP settings or separately managed Keycloak email templates.
- Keep identity-email templates internal, built-in, and non-overridable.
- Authenticate Keycloak-to-Admitto email delivery without relying on an end-user session.
- Keep WebAuthn ceremonies inside Keycloak; Admitto must not implement passkey registration or assertion itself.
- Update specs and architecture docs so they no longer describe Auth0 as an active option.

**Non-Goals:**

- Implement WebAuthn directly in the API or Admin UI.
- Add public self-service account registration.
- Remove local seeded password users or direct-grant test support.
- Add multi-factor authentication beyond passkey-only production login.
- Add an operator or admin resend flow for expired/missed enrollment emails.
- Support arbitrary Keycloak email types beyond the account-action events needed for passkey bootstrap.
- Expose identity-email templates in the Admin UI or team/event template APIs.

## Decisions

### Production and local Keycloak realms diverge intentionally

Production will use a Keycloak browser flow for Admin UI users that identifies the user and then requires a WebAuthn passwordless authenticator. The flow must not include `auth-username-password-form` as a fallback for normal Admin UI sign-in.

Local development will keep the current password-based flow and seeded users. This is an explicit environment exception, not a weakening of production authentication.

Alternative considered: make local development passkey-only too. That would match production more closely but makes automated tests and repeated local setup significantly harder. The existing test strategy depends on predictable credentials and Keycloak direct-grant clients.

### Custom Keycloak login-page theming is deferred

Custom Keycloak login-page theming has been removed. Production login, passkey assertion/registration, required-action, info/error, and logout pages use the stock `keycloak.v2` theme until a Keycloakify-based theme is reintroduced in a future change. The custom email theme is retained as the branding surface for account-action emails.

### Bootstrap admin is provisioned by email and enrolled by Keycloak action email

`Organization:BootstrapAdmin:EmailAddress` remains the bootstrap input. Startup ensures the Admitto user exists with admin privileges. External provisioning creates or reuses the Keycloak user, requires `webauthn-register-passwordless`, and sends an execute-actions email so the operator can create the first passkey.

Alternative considered: require an initial temporary password. This contradicts the desired passwordless production model and creates a secret that must be distributed, rotated, or disabled.

Alternative considered: first unknown signer becomes admin. This avoids invitation plumbing but creates a race condition on fresh deployments and is not acceptable for production bootstrap.

### Keycloak sends identity-email JSON through an Admitto API webhook

The production Keycloak image will include a custom Keycloak email template/renderer SPI implementation. Keycloak remains responsible for generating action links, action-token lifetimes, user identity context, and locale context. The custom renderer will not produce final subject, text body, or HTML body. Instead, it will map Keycloak email invocations to structured JSON and post that JSON to an internal Admitto API endpoint such as `POST /internal/keycloak/identity-emails`.

The request payload should contain only the data Admitto needs to select and render an email template, for example: email type (`passkey-enrollment`, `required-actions`, or another supported identity event), recipient email, recipient name if available, locale, action link, link expiration, required action names, realm/client identifiers, and a deterministic idempotency key. Admitto owns the final template names, subject/body rendering, and delivery.

The API endpoint will validate the request, persist or enqueue a system identity email with an idempotency key, and return success once Admitto has accepted responsibility for rendering and delivery. Actual rendering and SMTP delivery should remain in the Email module/Worker path where the Email capability is available, preserving the existing host-capability split.

Identity emails will use a separate internal template catalog, not the team/event-scoped `EmailTemplate` aggregate exposed to administrators. These templates are built into the application, versioned with code, and selected only by internal identity-email type. They must not participate in the normal override precedence of event-scoped template → team-scoped template → built-in default.

Alternative considered: configure SMTP directly in Keycloak. This is operationally simpler for Keycloak but creates two independent mail stacks, duplicates secret management, and bypasses Admitto's logging/retry path.

Alternative considered: use a Keycloak `EmailSenderProvider` and pass rendered email content to Admitto. That removes SMTP from Keycloak but still leaves email template content in Keycloak, which conflicts with the goal of using Admitto templates.

Alternative considered: store identity email templates in the existing configurable `EmailTemplate` aggregate. That would make customization easy but would allow admins to alter login/enrollment/security-critical emails, creating phishing and account-recovery risks. Built-in internal templates are safer and simpler.

Alternative considered: make the API call Keycloak back to fetch email content. That couples Admitto to Keycloak internals, still leaves rendering ownership ambiguous, and does not avoid needing a secure callback from Keycloak when email is requested.

### Keycloak email webhook uses HMAC authentication

The Keycloak renderer plugin and API will share a deployment secret configured through Aspire parameters/environment variables. Each request will include headers for timestamp, idempotency key, and HMAC signature. The signature will cover the HTTP method, path, timestamp, idempotency key, and SHA-256 hash of the JSON request body. The API will validate the timestamp window and signature using constant-time comparison before accepting the identity-email event.

This endpoint will not use user JWT bearer authentication because Keycloak is calling as infrastructure during an account action, often before a user has an Admitto session. It also should not use a broad Admitto API key because the caller is not acting as a team-owned external integration.

Alternative considered: OAuth client-credentials from Keycloak to Admitto. That is viable but introduces token issuance/validation bootstrapping between the same IdP and the API for a single narrow internal operation. HMAC keeps the trust boundary small and avoids circular dependency during first-admin enrollment.

### API token validation remains provider-agnostic

The API continues to validate JWTs from `Authentication:Bearer:Authority` and use the standard `sub` claim for `ExternalUserId`. Keycloak-specific behavior belongs in the realm configuration and `IExternalUserDirectory` adapter, not in endpoint authorization handlers.

Alternative considered: add Keycloak-specific claims or role mapping in the API. Admitto already owns application roles in Organization, so importing IdP roles would duplicate authorization state.

## Risks / Trade-offs

- [Risk] Misconfigured production realm still exposes password login. → Add tests or validation around the deployment realm import and document the required browser flow in arc42/ADR updates.
- [Risk] Keycloak email webhook is unavailable when enrollment is requested. → Return a clear failure to Keycloak and log the failed handoff; resend/retry is deliberately deferred to a later change.
- [Risk] Admitto template variables drift from the Keycloak JSON contract. → Version the internal payload contract and cover supported email types with template-rendering tests.
- [Risk] Identity templates are less customizable. → Accept the trade-off because these messages are security-sensitive and should remain stable, audited, and controlled by code review.
- [Risk] Shared webhook secret leaks. → Scope the secret to the single internal email endpoint, rotate via deployment configuration, and never accept it for user or team API calls.
- [Risk] Async worker provisioning can delay first enrollment email. → Ensure bootstrap status is observable in logs and make resend/reconcile idempotent.
- [Risk] Local and production auth flows differ. → Keep the difference explicit in specs, realm files, and AppHost wiring; cover both paths with targeted tests.
- [Risk] Existing Auth0-era spec text causes future implementation drift. → Update `passkey-authentication` requirements to Keycloak-only production behavior.

## Migration Plan

1. Update OpenSpec and architecture documentation to define Keycloak production passkey-only behavior and local password-based development behavior.
2. Update production Keycloak realm import to use a passwordless browser flow for the Admin UI realm.
3. Keep local realm import password-based with seeded users and direct-grant test clients.
4. Add the Keycloak identity-email renderer provider to the production Keycloak image and configure it with the Admitto API URL plus HMAC secret.
5. Add the internal Admitto API endpoint and Email module support for rendering and delivering system identity emails accepted from Keycloak.
6. Update bootstrap/external-user provisioning to reconcile Keycloak users and send passkey enrollment action emails idempotently.
7. Verify architecture tests first, then targeted Organization, API/auth, Email, and AppHost/realm validation tests.

Rollback: restore the previous production realm import/browser flow and disable the Keycloak identity-email renderer. Existing enrolled passkeys in Keycloak can remain; rolling back the browser flow only changes how future sign-ins are prompted.

## Open Questions

- None.
