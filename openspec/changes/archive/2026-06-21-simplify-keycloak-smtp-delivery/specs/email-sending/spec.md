## REMOVED Requirements

### Requirement: Keycloak identity emails are rendered and delivered through Admitto
**Reason**: Keycloak already supports account-action emails over SMTP. Removing the custom Keycloak identity-email provider and Admitto handoff reduces production risk and avoids maintaining a bespoke Java provider, Service Bus integration, internal identity-email templates, and a second queue-driven delivery path for a narrow infrastructure-owned use case.

**Migration**: Configure Keycloak SMTP directly. Local development continues to send Keycloak account-action emails to MailDev. Production supplies Keycloak SMTP settings through deployment parameters, separate from Admitto's application-email SMTP settings. Admitto's Email module continues to handle application-owned emails such as attendee confirmations, OTPs, reconfirmation, cancellation, waitlist, and bulk emails.
