# ADR-013: Platform SMTP sender, public event links, and team-owned email branding

## Status
Accepted.

## Context
Organizer-managed SMTP settings made every team responsible for deliverability, DNS alignment, credentials, and diagnostics. The product direction is an Admitto-controlled sender identity with attendee-facing links on an Admitto-owned tickets domain.

## Decision
Application email uses deployment-provided system SMTP settings under `Email:System`. The Email module does not persist team/event SMTP settings or SMTP credentials, and organizers cannot manage or test SMTP through Admitto.

Outgoing messages carry the platform's own sender identity: the `From` address is `Email:System:FromAddress` and the visible display name is `Email:System:FromDisplayName`, both deployment configuration. Sender identity is never derived from team data — no team name as display name and no `Reply-To` header — because sending on behalf of a team makes messages resemble spoofed third-party mail and measurably harms deliverability. Teams are identified in the message body instead. As a result teams do not own a reply-to address at all.

`TicketedEvent` owns a globally unique `PublicSlug`. Attendee-facing event links are generated from the configured public tickets base URL plus `/e/{publicSlug}`. The public route resolves only stored slugs and does not accept arbitrary redirect targets.

Email accent color is team-owned branding (`Team.AccentColor`, the shared `AccentColor` value object). Built-in Email templates receive this value through module-owned context rather than through email settings rows: the `team_email_context_view` projection is the single stored source, and it reaches the renderer through `EffectiveEmailSettings.AccentColor` for both transactional and bulk sending. Event-scoped rendering context (`EventEmailContextDto`) deliberately carries no branding, so there is exactly one accent-color path. Font family is not team-owned; it is a fixed system constant.

## Consequences
- SMTP failures caused by missing/invalid system configuration are operational failures, not team-owned event state.
- Registration policy/open status is no longer gated by organizer email settings.
- Email template rendering keeps team branding without retaining an EmailSettings aggregate.
- Public links use a stable Admitto domain while existing event `WebsiteUrl`/`BaseUrl` remain available as event-owned URLs.
- A uniform sender identity maximises deliverability and lets SPF/DKIM/DMARC be aligned once for the Admitto domain, at the cost of replies landing on the platform address rather than the team's.
- Because sender identity is pure configuration, the send pipeline needs only the accent color from the team projection, and email can be sent for a team whose projection has not caught up yet.

## References
- arc42 chapter 5 — Email module responsibilities and team branding ownership.
- arc42 chapter 6 — registration-confirmation email flow.
- arc42 chapter 7 — deployment-provided system SMTP settings and public tickets base URL.
- Change: `openspec/changes/route-email-through-admitto/`.
