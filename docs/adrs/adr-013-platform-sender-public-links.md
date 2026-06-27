# ADR-013: Platform SMTP sender, public event links, and team-owned email branding

## Status
Accepted.

## Context
Organizer-managed SMTP settings made every team responsible for deliverability, DNS alignment, credentials, and diagnostics. The product direction is an Admitto-controlled sender identity with attendee-facing links on an Admitto-owned tickets domain.

## Decision
Application email uses deployment-provided system SMTP settings under `Email:System`. The Email module does not persist team/event SMTP settings or SMTP credentials, and organizers cannot manage or test SMTP through Admitto.

`TicketedEvent` owns a globally unique `PublicSlug`. Attendee-facing event links are generated from the configured public tickets base URL plus `/e/{publicSlug}`. The public route resolves only stored slugs and does not accept arbitrary redirect targets.

Email accent color is team-owned branding (`Team.AccentColor`). Built-in Email templates receive this value through module-owned context rather than through email settings rows.

## Consequences
- SMTP failures caused by missing/invalid system configuration are operational failures, not team-owned event state.
- Registration policy/open status is no longer gated by organizer email settings.
- Email template rendering keeps team branding without retaining an EmailSettings aggregate.
- Public links use a stable Admitto domain while existing event `WebsiteUrl`/`BaseUrl` remain available as event-owned URLs.

## References
- arc42 chapter 5 — Email module responsibilities and team branding ownership.
- arc42 chapter 6 — registration-confirmation email flow.
- arc42 chapter 7 — deployment-provided system SMTP settings and public tickets base URL.
- Change: `openspec/changes/route-email-through-admitto/`.
