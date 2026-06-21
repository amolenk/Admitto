## Context

Admitto currently uses Keycloak for production, local development, and end-to-end testing. Production Admin UI authentication is passkey-only through Keycloak's hosted browser flow, while local development intentionally keeps seeded username/password users and direct-grant test clients.

The completed `bootstrap-admin-passkey-auth` change introduced a custom Keycloak email template provider that intercepts Keycloak account-action emails, publishes structured identity-email events to the shared queue, and lets Admitto's Worker render and send those messages through the Email module. That avoided a second SMTP path, but it also introduced a custom Java provider, Service Bus coupling from Keycloak, internal identity-email templates, and additional retry/logging behavior for a narrow bootstrap/account-action use case.

The production goal has shifted toward getting a stable version deployed as quickly as possible. Keycloak's built-in `execute-actions-email` flow already supports sending passkey enrollment links via SMTP. Keeping that path inside Keycloak reduces custom code and deployment risk while preserving the existing passkey-only production authentication model.

## Goals / Non-Goals

**Goals:**

- Use Keycloak's built-in SMTP email delivery for account-action emails, including passkey enrollment.
- Remove the custom Keycloak identity-email provider and the corresponding Admitto queue/email handling path.
- Preserve the custom Admitto Keycloak email theme used for account-action email branding.
- Preserve local development and end-to-end testing with seeded username/password users, including Alice and Bob.
- Keep local Keycloak SMTP pointed at MailDev so account-action emails can be inspected locally.
- Add production Keycloak SMTP deployment parameters suitable for a provider such as Fastmail.
- Keep Admitto's Email module unchanged for application-owned emails such as attendee confirmations, OTPs, reconfirmation, and bulk email.

**Non-Goals:**

- Removing or simplifying Admitto's application Email module.
- Changing the production Admin UI passkey-only browser flow.
- Changing the Keycloak email theme in this change; the existing custom email theme remains the account-action email branding surface.
- Adding a resend/recovery UI for expired or missed passkey enrollment emails.
- Implementing WebAuthn ceremonies in Admitto's API or Admin UI.

## Decisions

### Keycloak owns account-action SMTP delivery

Keycloak SHALL send account-action emails directly through its configured SMTP server. The Organization provisioning flow will continue to call Keycloak's Admin API `execute-actions-email` endpoint with `webauthn-register-passwordless`; Keycloak remains responsible for generating the action token, link, expiration handling, and email send.

Alternative considered: keep the custom Keycloak identity-email provider and route through Admitto. This gives one central mail pipeline but requires custom Java code, queue integration, internal templates, and additional operational coupling for a flow Keycloak already supports.

Alternative considered: replace the provider with an HTTP webhook rather than Service Bus. That still keeps a bespoke Keycloak-to-Admitto email integration and does not materially reduce production risk compared with direct SMTP.

### Production Keycloak SMTP is deployment-configured

Production SMTP settings SHALL be supplied through AppHost publish parameters and environment substitution in the deployment realm. The parameters should cover host, port, from address, display name, auth mode, username, password, SSL, and STARTTLS. Secrets, especially the SMTP password, must be marked secret in AppHost.

Fastmail can be used as the SMTP provider with `smtp.fastmail.com`, port `587`, STARTTLS enabled, auth enabled, and an app password. A custom sending domain should be configured and verified in Fastmail with SPF, DKIM, and DMARC before production use.

Alternative considered: hard-code production SMTP into the realm import. That would leak environment-specific configuration into source control and make rotation/deployment changes harder.

### Local Keycloak continues to use MailDev and password users

The local realm SHALL keep MailDev SMTP settings and the seeded password-capable users. Normal local sign-in with Alice and Bob does not require email, but developers can still trigger execute-actions emails and inspect them in MailDev.

Alternative considered: align local with production passkey-only behavior. That would improve parity but would make local setup and automated tests slower and more brittle.

### Admitto Email module no longer handles Keycloak identity emails

The Email module SHALL no longer define Keycloak identity-email contracts, internal passkey enrollment templates, or identity-email command handlers. Application-owned email flows remain unchanged and continue to use module settings, templates, logs, retries, and Worker delivery.

Alternative considered: leave unused identity-email code in place behind disabled configuration. That preserves a rollback path but increases maintenance cost and risks accidental reactivation.

## Risks / Trade-offs

- [Risk] Keycloak account-action emails are no longer recorded in Admitto's `EmailLog`. -> Accept for now; Keycloak account-action emails are infrastructure-owned security emails, not team/event application emails. Use Keycloak logs and SMTP provider logs for troubleshooting.
- [Risk] Production enrollment depends on correct Keycloak SMTP parameters. -> Add explicit AppHost parameters, document them in deployment docs, and validate local MailDev behavior.
- [Risk] Keycloak default account-action email templates may not match Admitto branding. -> The custom Admitto email theme supplies the account-action email copy; the action link lands on the stock `keycloak.v2` login pages until a Keycloakify-based login theme is reintroduced.
- [Risk] Operators may confuse Keycloak SMTP with Admitto application-email SMTP. -> Document the split clearly: Keycloak SMTP is for account actions; Admitto Email module SMTP is for attendee/application emails.
- [Risk] Removing the custom provider changes rollback mechanics. -> Roll back by restoring the provider files/AppHost wiring from version control, but prefer fixing Keycloak SMTP configuration first.

## Migration Plan

1. Remove the custom Keycloak provider build stage and provider jar copy from the Keycloak Dockerfile.
2. Remove AppHost Service Bus references and `KC_SPI_EMAIL_TEMPLATE_PROVIDER` / `ADMITTO_IDENTITY_EMAIL_*` environment variables from the Keycloak resource.
3. Add AppHost publish parameters for Keycloak SMTP and map them into Keycloak/realm configuration.
4. Keep local realm MailDev SMTP settings and confirm local execute-actions emails arrive in MailDev.
5. Remove the Admitto Email module identity-email event, commands, handlers, internal templates, and tests that only cover the removed handoff.
6. Update arc42 docs and ADR-012 to record direct Keycloak SMTP delivery.
7. Run architecture tests first, then targeted AppHost/realm validation, Organization provisioning, Email module, and affected API/worker tests.

Rollback: restore the custom provider and AppHost queue wiring from the previous version and remove/ignore the Keycloak SMTP publish parameters. Existing Keycloak users and passkeys can remain; only future account-action email delivery changes.

## Open Questions

- None.
