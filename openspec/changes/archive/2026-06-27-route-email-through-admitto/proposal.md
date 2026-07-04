## Why

Organizer-managed SMTP settings add setup friction, secret storage, deliverability variability, and support burden for a product direction that can be served by one Admitto-controlled sender domain. Routing all application email through Admitto also gives email links a consistent trusted domain and enables a simpler public event-link model for attendee-facing mail.

## What Changes

- **BREAKING** Remove organizer-managed SMTP settings and diagnostic SMTP test email APIs/UI. The Worker sends attendee, OTP, cancellation, reconfirmation, waitlist, and bulk emails through system SMTP settings supplied by deployment configuration.
- Add a globally unique public slug to `TicketedEvent` and use it for Admitto-owned public event links such as `https://tickets.admitto.org/e/{publicSlug}`.
- Keep `TicketedEvent.BaseUrl`/`WebsiteUrl` as event-owned external URLs used for redirects and event context; the new Admitto link layer sits in front of those URLs rather than replacing them.
- Move email accent color out of SMTP settings by making it team branding. The accent color is managed with team data, used by built-in email templates, and can later be exposed to the Admin UI as a scoped visual accent.
- Update ticket-confirmation email context to include a `ChangeTicketsLink` only when an event has at least two public self-service ticket types. Sold-out or waitlist state does not suppress the link.
- Update built-in ticket email templates to render the change-tickets CTA only when `ChangeTicketsLink` is present.
- Remove the email-not-configured degraded mode caused by missing team SMTP rows; missing or invalid system SMTP configuration becomes an operator/deployment issue.

## Capabilities

### New Capabilities

- `public-event-links`: Admitto-owned public event slugs and `/e/{publicSlug}` link behavior.

### Modified Capabilities

- `email-settings`: Remove organizer-managed SMTP settings and replace send-path configuration with deployment-provided system SMTP settings.
- `team-email-settings`: Remove team-scoped SMTP settings as a user-managed capability.
- `team-management`: Add team accent color as team-owned branding metadata.
- `event-management`: Add globally unique public slug to ticketed events while preserving event website/base URLs.
- `email-sending`: Send all application email through the Admitto system sender, use team accent color in built-in templates, and include conditional change-tickets links in ticket mail.
- `attendee-emails`: Reflect the new system-sender model and conditional ticket-mail links in attendee-visible email history/content expectations.
- `registration-policy`: Remove email-configuration gating from registration-open/status behavior, because application email is platform configured.
- `admin-ui-team-email-settings`: Remove the team email settings UI surface.
- `admin-ui-event-management`: Remove event email-settings assumptions and account for public slug / event link management.

## Impact

- Backend Email module: remove `EmailSettings` SMTP aggregate/endpoints/resolver usage; route `SendEmailHandler`, `DeliverEmailHandler`, and bulk fan-out through system sender configuration; keep `EmailLog`, outbox, retry, and template reliability behavior.
- Registrations module: add `TicketedEvent.PublicSlug`, uniqueness enforcement, admin create/update/read contract changes, and email-context query changes for Admitto public links and conditional change-tickets link.
- Organization module: add team accent color to team aggregate/DTOs if team branding is chosen as the source for email accent color.
- Admin UI and generated SDK: regenerate after backend API changes; remove team email settings page/proxies and add public slug/accent color fields where appropriate.
- Deployment/AppHost: wire system SMTP sender settings for the Worker/API paths that render or send system application email.
- Database: EF migrations remove email SMTP settings storage and add event public slug plus team accent color.
- Documentation: update arc42 building-block/runtime/deployment/cross-cutting chapters and affected OpenSpec main specs when the change is archived.
