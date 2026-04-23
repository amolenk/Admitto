## Context

The Email module today owns SMTP configuration but does no sending. Other modules already publish integration events on the outbox (`Admitto.Module.Shared/.../Outbox`) and dispatch them through a queue (Azure Storage Queues in production, Azurite locally; see arc42 §5.1). The Worker host is the only host that may have outbound SMTP access — `HostCapability.Email` already exists for exactly this purpose, and `RequiresCapability(HostCapability.Email)` is honored when registering command handlers (see `AddCommandHandlersFromAssembly`).

The legacy implementation in `Admitto.bak/src/Admitto.Application/Common/Email` (`EmailDispatcher`, `EmailTemplateService`, `EmailLog`, `WellKnownEmailType`, default Scriban templates as embedded resources) is the closest reference. Its main issues are: (a) it lives in the legacy monolith with no module boundary, (b) it commits state via the producing module's UoW, (c) deduplication runs synchronously on the same path that sends, which is fragile when the trigger is an at-least-once integration event.

This change reuses the existing module conventions (UoW per module, outbox-on-save, queue-driven integration-event handlers, capability-gated handlers, Scriban templating) and introduces no new architectural patterns.

## Goals / Non-Goals

**Goals:**
- Reliable, at-least-once delivery of attendee-registration emails: a transient SMTP failure or worker crash never silently drops an email.
- Deterministic deduplication: the same triggering integration event redelivered N times yields exactly one sent email per `(eventId, recipient, idempotencyKey)`.
- Configurable templates with a clear, single-pass resolution order: event > team > built-in default.
- Email server settings configurable at both team and event level, with event-level fully overriding team-level when present.
- A persisted, queryable email log suitable for both auditing and dedup checks.
- Architecture that admits a future "bulk email" use case (e.g. bulk reconfirm) without rework.

**Non-Goals:**
- Bulk-email use cases (no `IAsyncEnumerable<EmailMessage>` send paths in this change).
- Email types other than the registration-confirmation/ticket email.
- Provider-side delivery callbacks (bounce/delivered webhooks). The log models a `Status` enum for forward compatibility but only `Sent` and `Failed` are written by this change.
- Any UI changes in `Admitto.UI.Admin`.
- Public attendee-facing endpoints (e.g. webhook for "delivered").
- New ADRs — this change uses existing capability-gating, outbox, and facade conventions.

## Decisions

### D1. Trigger via integration event, not direct domain event

`AttendeeRegisteredDomainEvent` already exists on the Registrations side. We **do not** subscribe the Email module to a domain event in another module — that would couple modules across the in-process kernel boundary. Instead:

- Registrations' `RegistrationsMessagePolicy` maps `AttendeeRegisteredDomainEvent` → `AttendeeRegisteredIntegrationEvent` (new record in `Admitto.Module.Registrations.Contracts.IntegrationEvents`).
- Email module owns an `IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>` that produces the email.

**Why:** matches the existing Organization↔Registrations pattern (`MaterializeTicketedEventIntegrationEventHandler`), keeps the contract explicit in `*.Contracts`, and gives the queue + outbox machinery the at-least-once guarantee we need.

**Rejected:** publishing a domain-event handler in Email subscribed to a Registrations domain event — violates module boundaries (cross-module DbContext sharing) and skips the durable outbox hop.

### D2. Two-stage handler: enqueue, then send

The integration-event handler runs in the **API or Worker host that processes Registrations' outbox queue**, but actual SMTP sending must only happen in the Worker (capability-gated). So the integration-event handler is a thin "enqueue" step:

1. `AttendeeRegisteredIntegrationEventHandler` (Email module, **no** capability gate, runs wherever the Registrations queue is processed) idempotency-checks the email log and, if not already sent, **enqueues a `SendEmailCommand` via the Email module's outbox**. The enqueue itself is idempotent on `(triggerEventId, recipient)`.
2. `SendEmailCommandHandler` (Email module, **`[RequiresCapability(HostCapability.Email)]`**, runs only in Worker) loads the effective settings + rendered message, sends via SMTP, writes the `EmailLog` row, and commits.

**Why:** keeps SMTP out of the API host (preserves capability gating), but still gives the Email module a single owned write path and unit-of-work boundary. The outbox+queue between stages is the durability layer; SMTP retries are handled by the queue's visibility-timeout + retry policy. Both stages are independently idempotent on the same key.

**Rejected:** doing SMTP directly from the integration-event handler. Either (a) breaks capability gating if it runs in the API host, or (b) requires forcing the Registrations integration-event subscription to the Worker only, which is fragile and asymmetric with other modules.

### D3. Idempotency key derivation

The `SendEmailCommand` carries an explicit `IdempotencyKey` (string) and `Recipient`. For the registration trigger:

```
idempotencyKey = $"attendee-registered:{registrationId}"
```

The `email_log` table has a unique index on `(ticketed_event_id, recipient, idempotency_key)`. Both stages check this index before doing work; the unique index is the **last line of defense** against races (two workers picking up the same queue message). On unique-violation, the SMTP-stage handler swallows and logs.

**Why string, not Guid:** legacy used `DeterministicGuid.Create(string)` — strings are friendlier for future bulk scenarios (e.g. `"reconfirm-bulk:{batchId}:{registrationId}"`) and trivially logged.

### D4. Template resolution: event > team > built-in default

`EmailTemplate` is a single aggregate keyed by `(scope, scopeId, type)` where `scope ∈ {team, event}`. Lookup is one EF query: `WHERE type = X AND ((scope = 'event' AND scopeId = eventId) OR (scope = 'team' AND scopeId = teamId)) ORDER BY scope DESC LIMIT 1` (event sorts after team alphabetically — explicit `CASE` for clarity). If no row, fall back to a built-in default loaded as an embedded `.html`/`.txt` resource pair.

Default templates live in `src/Admitto.Module.Email/Application/Templating/Defaults/` (copied from legacy `Admitto.bak/src/Admitto.Application/Common/Email/Templating/Defaults/ticket.{html,txt}` for this change; other types added in follow-ups).

Rendering uses **Scriban** (already used in the legacy app; a well-known templating engine with no MVC dependency). Template parameters are an `IEmailParameters`-style object whose properties are imported as Scriban `ScriptObject` globals.

**Why one aggregate, not two:** team-templates and event-templates have identical fields and lifecycle; the only thing that differs is the scope. Modeling them separately would duplicate value objects and CRUD slices.

### D5. Settings storage: single `EmailSettings` aggregate scoped to team or event

`EmailSettings` is one aggregate keyed by `(Scope, ScopeId)` where `Scope ∈ {Team, Event}` — identical shape to `EmailTemplate` (D4). Same value objects (`Hostname`, `Port`, `EmailAddress`, `EmailAuthMode`, `SmtpUsername`, `ProtectedPassword`), same encryption, same `Version` for optimistic concurrency, single EF entity configuration, single CRUD slice family. A unique index on `(scope, scope_id)` enforces "at most one settings row per scope per scopeId".

The legacy `EventEmailSettings` aggregate (keyed only by `TicketedEventId`) is removed. The EF migration drops `event_email_settings` and creates `email_settings` with `scope`, `scope_id` columns; existing rows are copied as `(scope='event', scope_id=ticketed_event_id, …)` in the same migration so no data is lost.

**Effective settings resolution** is then a one-line null-coalesce on the same table:

```
effective = lookup(scope=Event, scopeId=eventId)
         ?? lookup(scope=Team,  scopeId=teamId)
```

No per-field merging. If event-scoped settings exist, team-scoped is ignored entirely — including for credentials. This matches the user's selected scope and avoids surprising "partial override" behavior.

`IEventEmailFacade.IsEmailConfiguredAsync(eventId)` returns true iff `effective != null && effective.IsValid()`. Implementation looks up the owning team via `IOrganizationFacade` to get the team-scoped row.

**Why one aggregate, not two:** identical fields, identical lifecycle, identical secret-handling path, identical admin-CRUD shape. Modeling separately would duplicate value objects, EF configuration, CRUD slices, and tests with no behavioral benefit. This is the same rationale that makes `EmailTemplate` a single aggregate (D4); applying it consistently keeps the Email module surface small.

**Why event fully overrides:** secrets-merging across two scopes is a security and UX hazard ("which password is actually being used right now?"). Full override is what every team will reason about.

### D6. SMTP adapter: MailKit, not SmtpClient

`IEmailSender` is implemented with **MailKit** (already a de-facto standard for modern .NET SMTP; legacy used `MailKit.Net.Smtp.SmtpClient`). One SMTP connection is opened per `SendEmailCommand` execution — no pooling in this change. Bulk sending will later add a pooled variant; the `IEmailSender` interface accepts a single message so this stays open.

**Risks:** see R3.

### D7. Email log lives in the email schema

The `email_log` table is owned and read only by the Email module. The unique index is `(ticketed_event_id, recipient, idempotency_key)` (matches the dedup query). Other indexes: `(ticketed_event_id, sent_at DESC)` for future audit listing.

Log writes happen inside the SMTP stage's UoW commit, in the same transaction as marking the originating outbox message Sent — so a successful SMTP send + crashed log write rolls back atomically and the queue message becomes visible again, hitting the dedup index on retry.

### D8. Bulk-email-friendliness without implementing bulk

The shape that matters for future bulk:

- `SendEmailCommand` is per-message and idempotent → bulk is "fan out N commands with N idempotency keys", same handler.
- Template resolution and rendering live behind `IEmailTemplateService`, which already takes `(emailType, teamId, eventId)` and returns a single `EmailMessage` — bulk callers loop and reuse.
- `IEmailSender` accepts a single message; a future `IBulkEmailSender` (or pooling decorator) can be added without changing the per-message contract.
- `email_log`'s unique index covers `(eventId, recipient, idempotencyKey)`, which is the natural dedup key for any future bulk batch (`"reconfirm-bulk:{batchId}"`).

We **do not** add enqueue-batching or multi-recipient sends in this change.

### D9. Capability gating

| Component | Capability gate | Reason |
| :-------- | :-------------- | :----- |
| `IEventEmailFacade` impl | none | Metadata-only; resolvable in every host (per existing `email-settings` spec). |
| Admin endpoints (settings, templates) | none | Metadata operations. |
| `AttendeeRegisteredIntegrationEventHandler` | none | Runs wherever the Registrations queue is processed. Does not touch SMTP. |
| `SendEmailCommandHandler` | `HostCapability.Email` | Performs SMTP I/O. |
| `IEmailSender` (MailKit impl) | `HostCapability.Email` | Outbound SMTP. |

The Worker host already declares `HostCapability.Email` (verified in `src/Admitto.Worker/Program.cs`); no host wiring change needed beyond registering the new SMTP-side services behind the capability flag.

### D10. Secret protection for the unified `EmailSettings`

Reuses the existing `IProtectedSecret` + Data Protection key ring already shared via `email.data_protection_keys`. Same `ProtectedPassword` value object, same `ProtectedPasswordConverter`. The single Email-module purpose string applies to both team-scoped and event-scoped rows — they live in the same module, same trust boundary, same table.

## Risks / Trade-offs

- **R1: At-least-once delivery may double-send if the dedup index is dropped or the log is purged.** → Mitigation: the unique index is part of the EF migration and not optional; document log retention policy in arc42 §08 once written.
- **R2: SMTP is slow and blocks the worker queue.** → Mitigation: per-message handler with bounded retries via the queue's existing retry policy; future bulk path will add throttling/pooling. Initial trigger volume (one email per registration) is low.
- **R3: MailKit's `SmtpClient` is per-connection-per-send in this change → connection churn under load.** → Mitigation: acceptable for the registration trigger volume; revisit when adding bulk (D8).
- **R4: Template rendering errors at send time produce a stuck queue message that retries forever.** → Mitigation: catch Scriban parse/render errors in `SendEmailCommandHandler`, write `EmailStatus.Failed` with `LastError`, and ack the message (do not throw). A future "retry failed templates after fix" admin action can re-enqueue from the log.
- **R5: Event-level fully overrides team-level → an organizer who sets event-level settings then "forgets" team-level cannot fall back.** → Mitigation: document the rule explicitly in the spec and the admin endpoint response (`source: 'event'|'team'|null`); UI work in a follow-up will surface this.
- **R6: `AttendeeRegisteredIntegrationEvent` adds a new contract type that downstream consumers may inadvertently couple to.** → Mitigation: the record only carries the minimal fields needed for emailing (TeamId, TicketedEventId, RegistrationId, RecipientEmail, RecipientName); additional payload requires an explicit contract change.
- **R7: A future bulk-email change must NOT regress the dedup invariant.** → Mitigation: D3/D7 lock the dedup key shape; bulk callers MUST supply an idempotency key.

## Migration Plan

1. Add the new `Admitto.Module.Email` aggregates (`EmailSettings`, `EmailTemplate`, `EmailLog`) and the EF migration in the `email` schema. The migration: (a) creates `email_settings` (with `scope`, `scope_id`, unique index on the pair) by copying existing `event_email_settings` rows as `(scope='event', scope_id=ticketed_event_id, …)`, (b) drops the old `event_email_settings` table, (c) creates `email_templates`, `email_log` tables (+ indexes). The migration runner is `Admitto.Migrations`; the data-copy step ensures no SMTP configuration is lost. Single-pass, no application downtime expected (no consumers other than the Email module read these tables).
2. Add `AttendeeRegisteredIntegrationEvent` and wire it in `RegistrationsMessagePolicy`. Until the Email module's handler is registered, the message lands on the queue and is acked as "no handler" by the existing dispatcher (verify behavior in `src/Admitto.Module.Shared/Infrastructure/Messaging/MessageQueueProcessor.cs` and add a no-op test if needed).
3. Register the Email module's integration-event handler (no capability gate) in the API host's queue processor.
4. Register `SendEmailCommandHandler` + `IEmailSender` (MailKit) behind `HostCapability.Email` in the Worker.
5. Ship default templates as embedded resources.
6. **Rollback:** the migration is data-preserving but not trivially reversible (the old `event_email_settings` table is dropped). For rollback: write a down-migration that recreates `event_email_settings` and copies rows where `scope='event'` back. Day-to-day rollback of behavior (not schema) is to disable the Email module's integration-event handler registration — the integration event continues to be published but is dropped by the queue dispatcher (idempotent no-op).

## Open Questions

- **OQ1:** Should the `AttendeeRegisteredIntegrationEvent` carry the attendee's preferred locale for future i18n templates? Not required for MVP; default to deferring until a real i18n use case lands. Recommendation: omit for now.
- **OQ2:** Email-log retention: keep forever, or TTL after N days? Out of scope for this change; pick a default in a follow-up once we have UI for browsing the log.
- **OQ3:** Should template CRUD admin endpoints support draft/preview before publishing? Not in this change — current scope is plain CRUD. Track separately if organizers ask for it.
