## Why

The current Keycloak integration routes account-action emails through a custom Keycloak provider, Service Bus, the Admitto Email module, and Worker delivery. That is more custom infrastructure than we need for a stable production release; Keycloak already supports execute-actions emails over SMTP, which is simpler to operate and easier to reason about.

## What Changes

- Remove the custom Keycloak identity-email provider and its Service Bus / Admitto Email-module handoff.
- Keep Keycloak as the production identity provider with the existing passkey-only Admin UI browser flow.
- Keep the custom Admitto Keycloak email theme in both local and production Keycloak images for account-action email branding.
- Configure Keycloak to send its own account-action emails through SMTP.
- Keep local development on the password-capable Keycloak realm with seeded username/password users, including Alice and Bob.
- Point local Keycloak SMTP at MailDev so execute-actions emails remain testable during development.
- Add production deployment parameters for Keycloak SMTP settings, suitable for providers such as Fastmail.
- Keep Admitto's Email module responsible for attendee, bulk, OTP, reconfirmation, and other application-owned emails; only Keycloak identity/account-action emails move out of that pipeline.
- Update architecture docs and ADR-012 to record Keycloak SMTP as the chosen account-action email path.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `passkey-authentication`: Bootstrap/admin passkey enrollment invitations are still initiated through Keycloak execute-actions email, but production delivery requires Keycloak SMTP configuration rather than the Admitto identity-email endpoint.
- `email-sending`: Remove the requirement that Keycloak identity emails are rendered and delivered through Admitto; clarify that Admitto email delivery covers application-owned emails, not Keycloak account-action emails.

## Impact

- `src/Admitto.AppHost/KeycloakConfiguration/Dockerfile` no longer builds or copies a custom Keycloak provider jar.
- `src/Admitto.AppHost/KeycloakConfiguration/providers/identity-email/` can be deleted.
- `src/Admitto.AppHost/AppHost.cs` no longer wires Keycloak to Service Bus for identity email; it wires Keycloak SMTP parameters instead.
- `src/Admitto.AppHost/KeycloakConfiguration/AdmittoRealm.Local.json` keeps MailDev SMTP and seeded password users.
- `src/Admitto.AppHost/KeycloakConfiguration/AdmittoRealm.Deployment.json` gains environment-substituted SMTP settings and keeps the Admitto email theme and passkey flow.
- `src/Admitto.Core/Email` identity-email commands, handlers, integration event contract, internal templates, and related tests can be removed.
- Architecture docs under `docs/arc42/` and `docs/adrs/adr-012-keycloak-production-identity-provider.md` must be updated.
- Production operators must provide Keycloak SMTP parameters separately from Admitto application-email SMTP parameters.
