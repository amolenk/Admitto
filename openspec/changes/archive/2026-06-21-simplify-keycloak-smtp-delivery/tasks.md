## 1. Architecture And Specs

- [x] 1.1 Update `docs/adrs/adr-012-keycloak-production-identity-provider.md` to replace the custom identity-email handoff with direct Keycloak SMTP delivery.
- [x] 1.2 Update `docs/arc42/06-runtime-view.md` bootstrap and identity-email sections to describe Keycloak execute-actions SMTP delivery.
- [x] 1.3 Update `docs/arc42/07-deployment-view.md` to document Keycloak SMTP parameters, MailDev local behavior, and the split from Admitto application-email SMTP.
- [x] 1.4 Update `docs/arc42/08-crosscutting-concepts.md` to remove the internal Keycloak identity-email delivery pattern.

## 2. Keycloak Image And Realm Configuration

- [x] 2.1 Remove the custom provider build stage and provider jar copy from `src/Admitto.AppHost/KeycloakConfiguration/Dockerfile` while keeping the deployment realm and `themes/admitto` copied into the image.
- [x] 2.2 Delete `src/Admitto.AppHost/KeycloakConfiguration/providers/identity-email/`.
- [x] 2.3 Keep `AdmittoRealm.Local.json` password-based with Alice/Bob seeded users and MailDev SMTP settings.
- [x] 2.4 Add environment-substituted SMTP settings to `AdmittoRealm.Deployment.json` for Keycloak account-action emails.
- [x] 2.5 Extend `validate-production-auth.mjs` or add equivalent validation to confirm the deployment realm keeps passkey-only auth and has deployment-configured SMTP settings.

## 3. Aspire AppHost Wiring

- [x] 3.1 Remove Keycloak `WithReference(serviceBus)`, `WaitFor(serviceBus)`, `KC_SPI_EMAIL_TEMPLATE_PROVIDER`, and `ADMITTO_IDENTITY_EMAIL_*` wiring that existed only for the custom identity-email provider.
- [x] 3.2 Add publish parameters for Keycloak SMTP host, port, from address, display name, auth, username, password, SSL, and STARTTLS.
- [x] 3.3 Wire local Keycloak to MailDev without adding a Service Bus dependency for identity email.
- [x] 3.4 Keep existing Worker system-email SMTP wiring for Admitto application emails unchanged.

## 4. Admitto Email Module Cleanup

- [x] 4.1 Remove `KeycloakIdentityEmailRequestedIntegrationEvent` from Email contracts.
- [x] 4.2 Remove Email module `IdentityEmails` use cases, commands, handlers, and worker-only send path.
- [x] 4.3 Remove built-in internal Keycloak identity-email template entries and default template files for `passkey-enrollment` and `required-actions` if they are no longer used elsewhere.
- [x] 4.4 Remove message registry or handler registration coverage that existed only for Keycloak identity-email events.
- [x] 4.5 Ensure existing attendee, OTP, reconfirmation, cancellation, waitlist, and bulk email flows still compile and keep their current behavior.

## 5. Tests

- [x] 5.1 Remove or rewrite tests that only validate the custom Keycloak identity-email provider, Admitto identity-email event handling, HMAC identity-email authentication, or internal identity-email templates.
- [x] 5.2 Update Organization/Keycloak user directory tests to assert execute-actions email is requested from Keycloak without assuming Admitto identity-email delivery.
- [x] 5.3 Update AppHost/realm validation tests for direct Keycloak SMTP configuration and preserved local MailDev behavior.
- [x] 5.4 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first.
- [x] 5.5 Run targeted Organization, Email, API, and AppHost-related tests affected by the cleanup.
