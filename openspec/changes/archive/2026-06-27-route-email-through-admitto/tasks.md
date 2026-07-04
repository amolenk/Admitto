## 1. Domain Model And Persistence

- [x] 1.1 Add team accent color to the Organization `Team` aggregate, value-object validation, DTOs, create/update handlers, and API responses.
- [x] 1.2 Add `PublicSlug` to the Registrations `TicketedEvent` aggregate, create/materialize/update commands, DTOs, validators, and read models.
- [x] 1.3 Enforce global uniqueness for `TicketedEvent.PublicSlug` in Registrations persistence and database exception mapping.
- [x] 1.4 Generate EF Core migrations for Organization, Registrations, and Email using official tooling; remove persisted Email SMTP settings storage.

## 2. System Email Sender

- [x] 2.1 Promote deployment-provided system SMTP options to the primary Email send-path resolver.
- [x] 2.2 Update `SendEmailHandler`, `DeliverEmailHandler`, and bulk fan-out to use system SMTP sender settings instead of team `EmailSettings` rows.
- [x] 2.3 Preserve `EmailLog`, outbox, retry, and bulk job behavior while changing only sender/settings resolution.
- [x] 2.4 Wire system email sender settings through AppHost/deployment/local configuration for Worker email delivery.
- [x] 2.5 Remove `EmailSettings` CRUD, diagnostic test-email slices, endpoint registrations, facade checks, and unused secret-protection code that only served organizer SMTP settings.

## 3. Public Event Links And Email Context

- [x] 3.1 Add configured public tickets base URL options for generating Admitto-owned `/e/{publicSlug}` links.
- [x] 3.2 Implement public event slug resolution and `/e/{publicSlug}` redirect/page behavior without open-redirect inputs.
- [x] 3.3 Update Registrations email-context query/facade DTO to include event public links, team accent color, and optional `ChangeTicketsLink`.
- [x] 3.4 Compute `ChangeTicketsLink` only when at least two ticket types have `SelfServiceEnabled == true`, ignoring sold-out and waitlist state.
- [x] 3.5 Update built-in ticket email text/HTML templates to render the change-tickets CTA only when the link is present.
- [x] 3.6 Ensure built-in email templates render with team accent color and default accent fallback.

## 4. API Clients And Admin UI

- [x] 4.1 Regenerate the Admin UI SDK from the Aspire-backed OpenAPI workflow after backend endpoint/contract changes.
- [x] 4.2 Remove Admin UI team/event email-settings pages, proxy routes, send-test-email actions, navigation entries, and generated SDK call sites.
- [x] 4.3 Add public slug fields to Admin UI event create/edit/detail flows and surface duplicate-slug errors.
- [x] 4.4 Add team accent color editing/display to the appropriate team settings surface.
- [x] 4.5 Optionally expose team accent color as a scoped CSS variable for selected-team UI affordances without a full design-system retheme.

## 6. Tests

- [x] 6.1 Run architecture tests first and fix any module-boundary violations.
- [x] 6.2 Add or update domain tests for team accent color and ticketed-event public slug validation.
- [x] 6.3 Add or update integration tests for public slug uniqueness, system email sender resolution, removed email-settings behavior, and conditional `ChangeTicketsLink` computation.
- [x] 6.4 Add or update API tests for removed email-settings routes, event public slug contracts, public `/e/{publicSlug}` behavior, and team accent color endpoints.
- [x] 6.5 Add or update Admin UI tests for removed email-settings UI, public slug forms, and team accent color editing if existing test coverage supports it.

## 7. Documentation And Specs

- [x] 7.1 Update arc42 building-block, runtime, deployment, and cross-cutting chapters to describe system sender configuration, public event links, and team branding ownership.
- [x] 7.2 Update or add ADR documentation if the platform-sender/public-link decision is considered architectural.
- [x] 7.3 Verify OpenSpec deltas match the implementation before archiving or syncing specs.
