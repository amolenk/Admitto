## Why

The Email module today only owns per-event SMTP server settings; it cannot actually send mail. Attendees who self-register receive nothing, organizers cannot offer any kind of confirmation, and there is no audit log of what was sent. We also need email delivery to be **reliable** (handle transient SMTP failures and at-least-once delivery of triggering integration events) and **deduplicating** (the same triggering event redelivered must not produce a second email).

This change makes the Email module actually send mail in response to integration events from other modules, starting with the registration-confirmation/ticket email triggered by `AttendeeRegisteredIntegrationEvent`. It also lifts SMTP settings up to the team level and introduces configurable email templates that fall back from event → team → built-in default.

## What Changes

- **New: send registration-confirmation email**
  - Registrations module publishes a new `AttendeeRegisteredIntegrationEvent` from the existing `AttendeeRegisteredDomainEvent` via its `MessagePolicy`.
  - Email module subscribes to that integration event, composes a "ticket" email from a template, and sends it via SMTP using the resolved per-event/per-team email settings.
  - End-to-end delivery is durable (outbox + queue + retries) and idempotent (one email per `(eventId, recipient, idempotencyKey)`).
- **New: email log**
  - Email module persists a row per sent email (recipient, subject, type, provider, status, sent-at, idempotency key) in the `email` schema with a unique index that deduplicates redelivered triggering events.
- **New: configurable email templates**
  - New `EmailTemplate` aggregate in the Email module, scoped to either a team or a specific event. Carries `Type` (e.g. `ticket`), `Subject`, `TextBody`, `HtmlBody`, all rendered with Scriban.
  - Lookup precedence: event-level template → team-level template → built-in default (embedded resources).
  - Admin endpoints for CRUD of team and event templates (read/upsert/delete; backend only — no UI in this change).
- **New: team-level email settings via a single, scope-keyed aggregate**
  - **Replace** the existing per-event-only `EventEmailSettings` aggregate with a single `EmailSettings` aggregate keyed by `(Scope, ScopeId)` where `Scope ∈ {Team, Event}` — the same shape used for `EmailTemplate`. Same fields (host/port/from/auth/credentials), same encryption-at-rest, same per-scope uniqueness.
  - **BREAKING (internal, no external API breakage):** `event_email_settings` table is replaced by `email_settings` (with `scope`, `scope_id` columns and a unique index on `(scope, scope_id)`). Existing rows are migrated by the EF migration: each existing row becomes `(scope='event', scope_id=ticketed_event_id, …)`. `IEventEmailFacade.IsEmailConfiguredAsync(eventId)` is unchanged at the contract level but now returns true if event-scoped OR team-scoped settings are valid; a new internal resolver returns the effective settings.
  - Admin endpoints exposed at both team and event scope, sharing one slice family (mirrors the `EmailTemplate` admin pattern).
  - Event-scoped settings fully override team-scoped settings when present (no per-field merging).
- **Deferred (explicitly):** bulk email (e.g. bulk reconfirm), additional triggers (cancellation, reconfirm, visa-letter-denied, verify-email), and any UI work. The architecture below is chosen so bulk sending can be added without rework — see `design.md`.

## Capabilities

### New Capabilities
- `email-sending`: Reliable, idempotent, integration-event-driven outbound email (registration-confirmation as the first trigger) using the email module's settings + template resolution.
- `email-templates`: Persisted, per-team and per-event email templates with built-in defaults and Scriban-based rendering.
- `email-log`: Persisted record of every email the system attempts/sends, used both for auditing and for deduplication.
- `team-email-settings`: Team-scoped SMTP server settings, modeled as part of the unified `EmailSettings` aggregate (same scope/scopeId shape as `EmailTemplate`) and resolved together with event-scoped settings.

### Modified Capabilities
- `email-settings`: Storage shape changes from per-event aggregate to a single `EmailSettings` aggregate scoped to team or event; the facade now reports "configured" when either scope is valid; a new internal contract exposes the effective resolved settings to email-sending.
- `attendee-registration`: Registrations module now publishes `AttendeeRegisteredIntegrationEvent` whenever an attendee is registered (admin-initiated or self-service), via its `MessagePolicy` from the existing `AttendeeRegisteredDomainEvent`.

## Impact

- **Code**
  - `Admitto.Module.Email` (new aggregates, handlers, infrastructure adapters, EF migration, default templates as embedded resources).
  - `Admitto.Module.Email.Contracts` (extended facade contract; new `EffectiveEmailSettingsDto` shape if needed for cross-module composition — internal to email).
  - `Admitto.Module.Registrations` (new integration event published via `RegistrationsMessagePolicy`).
  - `Admitto.Module.Registrations.Contracts` (new `AttendeeRegisteredIntegrationEvent` record).
  - `Admitto.Worker` (capability-gated email sender registered when `HostCapability.Email` is set; SMTP adapter using MailKit).
- **Infrastructure**
  - New EF migration in `email` schema: replaces `event_email_settings` with `email_settings (scope, scope_id, …)` (data-preserving migration: existing rows become `scope='event'`); adds `email_templates`, `email_log` tables (+ unique indexes for scope uniqueness and dedup).
  - Existing Data Protection key ring already lives in the `email` schema; reused for team-level secret encryption.
  - Worker host gains an outbound SMTP dependency (already represented in arc42 §5.1 as the SMTP service).
- **APIs**
  - New admin endpoints under the email module for team-email-settings and email-template CRUD (no UI changes; CLI parity per `src/Admitto.Cli/AGENTS.md`).
  - No changes to public attendee-facing endpoints.
- **Docs**
  - Update `docs/arc42/05-building-block-view.md` (Email module description) and `docs/arc42/06-runtime-view.md` (registration-email runtime flow).
  - No new ADR required (uses existing capability-gating, outbox, and facade conventions).
- **Out of scope (explicitly deferred):** UI changes, bulk-email use cases, additional trigger types beyond registration confirmation.
