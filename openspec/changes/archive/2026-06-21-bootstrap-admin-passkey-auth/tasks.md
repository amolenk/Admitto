## 1. Architecture And Specs

- [x] 1.1 Update `docs/arc42/06-runtime-view.md` bootstrap admin flow to describe Keycloak passkey enrollment and local password development behavior.
- [x] 1.2 Update `docs/arc42/08-crosscutting-concepts.md` authentication guidance to distinguish production passkey-only login from local/test password login.
- [x] 1.3 Update `docs/adrs/adr-012-keycloak-production-identity-provider.md` to record production passkey-only Keycloak as the intended architecture.
- [x] 1.4 Document Keycloak identity-email JSON handoff through Admitto, including non-overridable internal template ownership, the internal endpoint trust boundary, and HMAC authentication.
- [x] 1.5 Remove or revise remaining Auth0-era references in architecture docs and OpenSpec main spec text affected by this change.

## 2. Keycloak Realm Configuration

- [x] 2.1 Update `AdmittoRealm.Deployment.json` so the production browser flow requires WebAuthn passwordless authentication and does not offer username/password fallback.
- [x] 2.2 Keep `AdmittoRealm.Local.json` password-based for local development with seeded users and direct-grant test clients.
- [x] 2.3 Ensure production clients do not allow password grant for Admin UI authentication.
- [x] 2.4 Configure the production Keycloak image to load a custom identity-email renderer/template provider from `/opt/keycloak/providers`.
- [x] 2.5 Add a lightweight validation test or script assertion that the deployment realm import does not include password form fallback in the active browser flow.

## 3. Keycloak Theme Coverage

- [~] 3.1 Deferred: custom Keycloak login-page theming has been removed. Production login/passkey/required-action/info/error/logout pages use the stock `keycloak.v2` theme until a Keycloakify-based theme is reintroduced in a future change. The custom email theme is retained for account-action email branding.

## 4. Keycloak Identity-Email Renderer

- [x] 4.1 Add a Java Keycloak email template/renderer provider that maps supported Keycloak email invocations to structured identity-email JSON and posts it to the Admitto API.
- [x] 4.2 Include supported fields in the JSON payload: email type, recipient email, recipient name if available, locale, action link, link expiration, required action names, realm/client identifiers, and idempotency key.
- [x] 4.3 Ensure the renderer does not send final subject, text body, or HTML body content to Admitto.
- [x] 4.4 Configure the provider with Admitto API base URL, HMAC shared secret, timeout, and enabled/disabled mode via environment variables.
- [x] 4.5 Sign each request with timestamp, idempotency key, body hash, and HMAC signature headers.
- [x] 4.6 Package the provider jar into the Keycloak production image and keep local development able to use direct SMTP or default behavior unless explicitly enabled.
- [x] 4.7 Add provider unit tests for payload mapping, omission of rendered email content, idempotency key generation, and signature generation.

## 5. Internal Email API And Delivery

- [x] 5.1 Add an internal API endpoint for structured Keycloak identity-email requests outside the admin route group.
- [x] 5.2 Validate Keycloak email requests with a dedicated HMAC authentication component using constant-time signature comparison and timestamp skew checks.
- [x] 5.3 Add an Email module use case for system identity emails that are not scoped to a team or ticketed event and are rendered from Admitto templates.
- [x] 5.4 Ensure accepted Keycloak emails are idempotent by request idempotency key and do not duplicate sends on retries.
- [x] 5.5 Add built-in internal system templates for at least passkey enrollment / required-action emails and ensure they are not stored in the configurable `EmailTemplate` aggregate.
- [x] 5.6 Ensure identity-email rendering does not use team/event template override precedence and cannot be affected by templates with matching names.
- [x] 5.7 Ensure identity-email templates are not returned by admin template listing APIs and cannot be created, updated, or deleted through Admin UI template management.
- [x] 5.8 Route accepted identity emails to the Worker for template rendering and SMTP delivery through Admitto-managed system email settings.

## 6. Bootstrap And Provisioning

- [x] 6.1 Reconcile `BootstrapAdminUserInitializer` with the external provisioning flow so an admin without `ExternalUserId` is provisioned or linked idempotently.
- [x] 6.2 Ensure `KeycloakUserManagementService` creates or finds users by email and sends `webauthn-register-passwordless` execute-actions email when enrollment is needed.
- [x] 6.3 Document that resend/recovery is intentionally out of scope for this change and must be introduced through a future controlled operator/admin flow.

## 7. Local Development And AppHost Wiring

- [x] 7.1 Verify AppHost uses the local password-capable realm for run mode and the passkey-only deployment realm for publish mode.
- [x] 7.2 Verify local bootstrap defaults still allow developer login with seeded email/password users.
- [x] 7.3 Wire Aspire parameters/environment variables for Keycloak email provider API URL and HMAC secret in publish mode.
- [x] 7.4 Wire the Keycloak identity-email renderer to the Admitto API/system email path; do not introduce separate Keycloak SMTP settings.

## 8. Tests

- [x] 8.1 Add or update Organization tests for bootstrap admin creation, idempotency, and missing `ExternalUserId` reconciliation.
- [x] 8.2 Add or update Keycloak user directory tests for create/find/resend enrollment behavior.
- [x] 8.3 Add API tests for Keycloak identity-email endpoint authentication, invalid signatures, timestamp skew, payload validation, and idempotent duplicate requests.
- [x] 8.4 Add Email module tests for system identity-email template resolution and rendering from Keycloak JSON payloads.
- [x] 8.5 Add tests proving identity templates ignore team/event overrides and are not exposed through admin template APIs.
- [~] 8.6 Deferred with task 3.1: custom login-page theme coverage checks removed; the validation script retains the email-theme assertions only.
- [x] 8.7 Add or update API/auth tests for unknown identity, first sign-in binding, and account-takeover guard behavior if affected.
- [x] 8.8 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first.
- [x] 8.9 Run targeted Organization/API/Email/AppHost-related tests changed by this implementation.
