## Context

Admitto separates Organization, Registrations, and Email by namespace/module boundary. Cross-module reads are allowed through contracts/facades, while cross-module writes and state synchronization use integration events and the outbox.

Today, Email transactional handlers call `IRegistrationsFacade.GetEventRegistrationSnapshotAsync(...)`. Registrations builds that snapshot from its own `TicketedEvent`, `TicketCatalog`, and `Registration` data, then calls `IOrganizationFacade.GetTeamBrandingAsync(...)` to add `Team.AccentColor`. This keeps the Email module thin, but it means Registrations assembles email-rendering context and depends on Organization for branding during Email work.

The relevant data is mostly slow-changing rendering and scheduling context. It does not participate in registration capacity, authorization, or lifecycle invariants, so it can be safely projected into Email with eventual consistency.

## Goals / Non-Goals

**Goals:**

- Move team/event email-rendering context ownership into the Email module.
- Remove the synchronous `GetTeamBrandingAsync` Organization facade method.
- Reduce per-transactional-email cross-module reads for slow-changing team/event metadata.
- Preserve live Registrations reads for attendee selection, reconfirm eligibility, and bulk recipient resolution.
- Keep projection maintenance idempotent under at-least-once integration-event delivery.
- Document eventual-consistency semantics for email rendering context.

**Non-Goals:**

- Do not make Email authoritative for Teams, TicketedEvents, Tickets, Registrations, or policies.
- Do not duplicate attendee lists or registration state into Email beyond existing `EmailLog`/bulk recipient snapshots.
- Do not change public/admin HTTP APIs.
- Do not change registration correctness, ticket capacity, event lifecycle, or authorization flows.
- Do not make email rendering strongly consistent with the latest team/event edit.

## Decisions

### D1. Add an Email-owned `EventEmailContext` projection

Persist one Email-schema row per `(TeamId, TicketedEventId)` with the minimum data Email needs for rendering and scheduling:

- `TeamId`
- `TicketedEventId`
- `TeamAccentColor`
- `EventName`
- `WebsiteUrl`
- `PublicSlug` or enough stored URL data to derive public links with configured base URL
- `TimeZone`
- current reconfirm trigger policy snapshot, if any
- `SelfServiceTicketTypeCount`
- event lifecycle status needed to disable scheduling/rendering when archived
- timestamps or version markers useful for diagnostics

Rationale: this keeps Email rendering context close to Email templates and send pipelines, and avoids requiring Registrations to aggregate Organization-owned branding.

Alternative considered: keep `GetEventRegistrationSnapshotAsync` and only move `GetTeamBrandingAsync` into Email. This reduces one facade method but preserves the awkward ownership problem where Registrations remains the email-context assembler.

### D2. Feed the projection through integration events

Registrations publishes enough event-context changes for Email to upsert the projection when an event is created or changed. Organization publishes enough team-branding changes for Email to update `TeamAccentColor` on existing rows for the team.

Handlers in Email are idempotent. If events arrive out of order, handlers create partial projection state when possible and fill in missing parts when the complementary event arrives. Email rendering fails or defers deterministically when required context is still missing.

Rationale: this matches existing outbox/integration-event conventions and keeps cross-module writes asynchronous.

Alternative considered: let Email query both Organization and Registrations directly through facades and cache results. That still leaves runtime coupling and cache invalidation without using the already-established messaging model.

### D3. Keep live Registrations reads for attendee data

Email continues using `IRegistrationsFacade.GetRegistrationsAsync(...)` for:

- reconfirm candidate evaluation in `RequestReconfirmationsJob`
- bulk-email attendee-source resolution
- any workflow where the spec requires live registration status, `HasReconfirmed`, ticket-type filters, or additional detail filters

Rationale: attendee state is not slow-changing rendering context. Specs explicitly require live eligibility evaluation and snapshot-on-resolve semantics.

Alternative considered: project registration summaries into Email. That would create a second attendee read model with correctness and staleness risks, and it is not needed for the current goal.

### D4. Transactional email handlers combine trigger facts with projection facts

Integration-event payloads remain the source for occurrence-specific facts such as recipient email, registration id, first/last name where present, ticket names, cancellation reason, OTP code, coupon code, and idempotency timestamps. The Email projection supplies event/team rendering facts such as event name, website URL, public links, team accent color, and change-ticket availability.

For cancellation flows whose trigger payload does not currently carry attendee first/last name, either the cancellation integration event is extended to include those immutable-at-cancel-time values or the Email handler keeps a narrow Registrations read for that specific missing registration fact. Prefer extending the event payload because the data is occurrence-specific and should travel with the event that triggers the email.

Rationale: this avoids using the projection as an event log and keeps it limited to reusable context.

### D5. Reconfirm scheduling uses the projection for schedule context, not candidate context

Email upserts/removes per-event Quartz triggers from projection state and Registrations lifecycle/policy/time-zone events. The job data continues to carry enough policy values for the tick, and the tick still queries live Registrations for candidates.

Rationale: schedule context changes slowly and belongs naturally in the EventEmailContext row; candidate eligibility changes frequently and remains Registration-owned.

### D6. Public links are derived inside Email

Store `PublicSlug` and derive links using Email/application configuration for the public base URL, matching current behavior in Registrations. Store fully materialized links only if implementation shows the base URL is not conveniently available in Email.

Rationale: public base URL is deployment configuration, not event state. Deriving links keeps the projection smaller and avoids backfilling rows when deployment URL configuration changes.

## Risks / Trade-offs

- Eventual consistency can render a just-sent email with a previous accent color or event name → Accept for email rendering; document semantics and keep updates idempotent.
- Missing projection row can prevent deterministic rendering after a queue race or migration gap → Handler records a failed email log or defers/retries according to deterministic vs transient classification; migration/backfill seeds existing events.
- Integration-event payloads may grow → Keep payloads to email-context facts and avoid including attendee lists or ticket catalogs.
- Out-of-order event delivery can create partial state → Projection upserts tolerate partial updates and required rendering reads validate required fields.
- Removing `GetTeamBrandingAsync` may expose other hidden callers → Use compile-time cleanup and ArchTests to confirm module boundaries.

## Migration Plan

1. Add Email projection entity, EF configuration, write-store access, and migration.
2. Add integration events/handlers to populate team branding and event context projection state.
3. Backfill the projection for existing teams/events during migration or startup reconciliation, using module-owned queries/facades that respect boundaries.
4. Move transactional email context reads from `IRegistrationsFacade.GetEventRegistrationSnapshotAsync(...)` to the Email projection.
5. Move reconfirm scheduler context reads to the projection while keeping live candidate reads in Registrations.
6. Enrich bulk built-in/system template rendering from the projection where needed.
7. Remove `GetTeamBrandingAsync` and related DTO/handler code once no callers remain.
8. Update arc42 docs and add/update an ADR if this projection becomes a documented architectural pattern.

Rollback: keep the migration additive until callers have moved. If rollout fails before facade removal, revert handlers to the existing facade path. After facade removal, rollback requires redeploying the previous application version and retaining the additive Email projection table harmlessly.

## Open Questions

- Should the projection be backfilled by an explicit migration script, a startup reconciliation job, or a one-time application job?
- Should cancellation integration events be extended with first/last name to eliminate the last registration-specific context read for cancellation emails?
- Should Organization publish a dedicated `TeamBrandingChanged` event only when accent color changes, or a broader `TeamUpdated` event consumed selectively by Email?
- Should this be recorded as a new ADR because it formalizes an Email-owned cross-module projection?
