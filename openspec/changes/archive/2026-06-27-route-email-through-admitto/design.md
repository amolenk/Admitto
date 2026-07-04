## Context

The Email module currently owns team-scoped `EmailSettings` containing SMTP host, port, sender address, authentication mode, encrypted password, and email branding values. Worker email delivery resolves those settings at send time, and Admin UI exposes a team email-settings page for organizers to manage and test SMTP configuration.

The current model improves organizer flexibility but pushes deliverability, DNS, SMTP credentials, and operational troubleshooting onto each team. The product direction is to use Admitto as the stable sender identity while keeping event-specific display names and Admitto-owned public links in attendee-facing emails.

Registrations owns `TicketedEvent`, including event name, website URL, base URL, lifecycle, policies, and ticket catalog. Organization owns `Team`. The existing architecture requires cross-module access through contracts/facades and keeps endpoint-owned unit-of-work boundaries.

## Goals / Non-Goals

**Goals:**

- Send all application email through a deployment-configured Admitto system sender.
- Remove organizer-managed SMTP settings, test-email actions, and SMTP secret storage.
- Add a globally unique public event slug to `TicketedEvent` for Admitto-owned links.
- Keep existing event `BaseUrl`/`WebsiteUrl` so Admitto public routes can redirect to the event website or external public frontend.
- Move accent color to team-owned branding so email templates and, later, Admin UI affordances can use the same value.
- Include a change-tickets link in ticket-confirmation email only when the event has at least two public self-service ticket types.
- Preserve email reliability behavior: outbox, `EmailLog` claims, retry behavior, bulk recipient snapshots, cancellation, and single SMTP connection bulk fan-out.

**Non-Goals:**

- No customer-domain `From` address support.
- No customer SMTP configuration or SMTP connectivity testing.
- No full Admin UI theme overhaul; accent color exposure to UI should be scoped and incremental.
- No domain-verification or white-label email/link feature.
- No change to Keycloak account-action email delivery, which remains separate Keycloak SMTP infrastructure.
- No change to ticket availability rules for self-service ticket changing beyond whether the ticket email displays the link.

## Decisions

1. Use system SMTP configuration for all application email.

   The Worker should resolve `EffectiveEmailSettings` from configuration rather than from a team-owned database row. Configuration should include SMTP host, port, auth mode, credentials, a stable Admitto `FromAddress`, and optionally a default display name. Existing `SystemEmailOptions`/`SystemEmailSettingsResolver` can be promoted from unused infrastructure to the primary send-path resolver.

   Alternative considered: keep team `EmailSettings` but ignore SMTP fields. Rejected because it preserves misleading admin API/UI, secret-storage code, and the “email not configured by team” state.

2. Keep event-specific display name separate from authenticated sender address.

   Emails may use display names such as `Azure Fest 2026` while the `FromAddress` domain remains Admitto-controlled, for example `Azure Fest 2026 <tickets@admitto.org>`. This keeps DKIM/DMARC alignment with Admitto while allowing attendee-visible event context.

   Alternative considered: customer-domain `FromAddress`. Rejected because it requires domain verification and DNS setup that this change intentionally avoids.

3. Add `TicketedEvent.PublicSlug` in Registrations.

   A public event slug is event identity/navigation, not email configuration. It belongs on `TicketedEvent`, is globally unique, and feeds Admitto public links such as `/e/{publicSlug}`. The existing `BaseUrl` and `WebsiteUrl` remain authoritative event-owned external URLs and are used as redirect targets or event context.

   Alternative considered: store the slug in Email settings. Rejected because the slug will be used for public routing beyond email composition and should follow event lifecycle and uniqueness rules.

4. Make accent color team-owned branding.

   Because the accent color can theme both email templates and selected-team UI affordances, it should live on `Team` rather than Email. Email delivery can obtain it through a cross-module read contract or an email-context DTO supplied by existing facades. The initial UI use should be limited to a CSS variable or small affordances, not a complete design-system retheme.

   Alternative considered: keep branding in Email. Rejected because removing SMTP settings would leave an Email aggregate whose only purpose is generic team branding.

5. Generate Admitto-owned public links from configured public base URL plus event slug.

   Email link generation should use an operator-configured public tickets base URL, for example `https://tickets.admitto.org`, combined with `/e/{publicSlug}`. Registration-specific paths can be derived under the slug, for example `/e/{publicSlug}/qr-code/{registrationId}`, `/e/{publicSlug}/cancel/{registrationId}`, and `/e/{publicSlug}/registrations/{registrationId}/tickets` or a route chosen during implementation.

   The public `/e/{publicSlug}` route must not be an open redirect. It should resolve the slug to a stored event and redirect only to that event's configured URL or serve a controlled public page.

   Alternative considered: continue using event `BaseUrl` directly in all emails. Rejected because mail clients then see arbitrary organizer domains as primary CTA links instead of the authenticated Admitto domain.

6. Compute conditional change-ticket link in Registrations email context.

   Registrations owns ticket catalog and self-service ticket metadata, so it should decide whether the link exists. The rule is: include `ChangeTicketsLink` when at least two ticket types have `SelfServiceEnabled == true`. Sold-out and waitlist state do not suppress the link. Email templates should render the CTA only when the parameter is present.

   Alternative considered: let Email inspect ticket catalog state. Rejected because that would violate module ownership boundaries.

7. Remove email-settings API surfaces and regenerate clients.

   Backend removal changes the OpenAPI contract. The Admin UI SDK must be regenerated before removing proxy/UI call sites so no handwritten client shortcuts are introduced.

## Risks / Trade-offs

- [Risk] A single Admitto sender reputation is affected by all customer bulk traffic. -> Mitigation: keep transactional and bulk operational settings separable where practical, monitor bounces/complaints, and consider separate future sending streams/domains if needed.
- [Risk] Teams lose custom sender identity. -> Mitigation: retain event/team display names and optional `Reply-To` as a future additive feature, while keeping authenticated `FromAddress` Admitto-owned.
- [Risk] Globally unique slugs can collide or require naming negotiation. -> Mitigation: validate uniqueness at create/update time, enforce a database unique index, and provide clear conflict errors.
- [Risk] Removing `EmailSettings` deletes existing local/test configuration. -> Mitigation: acceptable because production data preservation has not been stated as required; provide a forward migration and update local Aspire configuration.
- [Risk] UI theming scope creeps into a full design-system retheme. -> Mitigation: store team accent color now, use it in email immediately, and apply only small Admin UI affordances in this change if implementation remains small.
- [Risk] Public `/e/{slug}` redirect could become an abuse vector. -> Mitigation: no arbitrary redirect query parameter; only redirect to validated event-owned URLs stored in Admitto.

## Migration Plan

1. Add team accent color and event public slug model changes with EF migrations.
2. Introduce or promote system email sender options and wire them through AppHost/deployment configuration.
3. Replace Email send-path settings resolution with system configuration plus team branding/event context.
4. Remove `EmailSettings` CRUD/test endpoints, aggregate storage, validators, UI routes, and API client call sites.
5. Add public slug fields to event create/update/read contracts and Admin UI forms.
6. Add Admitto public-link generation and the `/e/{publicSlug}` redirect/link route behavior.
7. Extend Registrations email context with `ChangeTicketsLink` and update ticket templates.
8. Regenerate Admin UI SDK after backend contract changes.
9. Update arc42 and archive/sync OpenSpec specs after implementation is verified.

Rollback is destructive for removed team SMTP settings unless a data-preserving migration is added before production use. The expected rollback path during local/test development is to revert code and database state.

## Open Questions

- What exact public tickets base URL should be configured for local development and production (`tickets.admitto.org`, environment-specific host, or API-host route)?
- Should the change-ticket public route reuse the existing API-key-protected public endpoint directly, or should `/e/{publicSlug}/...` serve as a frontend route that calls the API with the event site's API key?
- Should `Reply-To` be introduced in this change or left as a follow-up once team contact information is modeled?
