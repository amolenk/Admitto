## Context

The Email module (introduced by `add-email-module`) only sends single transactional emails (one per `AttendeeRegisteredIntegrationEvent`). Two new use cases require fan-out:

1. **Reconfirm**: `TicketedEvent` already owns `TicketedEventReconfirmPolicy` (window + cadence). Nothing today turns that policy into actual emails on cadence to all currently-registered attendees who haven't been asked recently enough.
2. **Custom bulk**: organizers want to mail a chosen subset of registrants (e.g. all "workshop A" ticket holders) and also occasional external lists (e.g. a marketing invite list pasted from a spreadsheet) — without first uploading and persisting a "recipient list" entity as the legacy code required.

Constraints inherited from the codebase:

- arc42 §8.4 says cross-module reads use a **facade**; cross-module **writes** use the outbox.
- arc42 §8.6 splits domain / module / integration events; reconfirm is internal to the Email module so its policy-changed signal can be a module event.
- The Email module already has `EmailLog` with a unique index on `(ticketed_event_id, recipient, idempotency_key)`. Bulk fan-out reuses that table and idempotency mechanism without going through the single-send command pipeline (see D4).
- `HostCapability.Jobs` already gates Quartz-driven work to the Worker host; `HostCapability.Email` gates SMTP. Both apply here.
- The Registrations module owns `Registration`, `TicketedEvent`, `TicketType`. The Email module must not query `RegistrationsDbContext` directly.

Legacy reference: `Admitto.Application/Jobs/SendCustomBulkEmailJob.cs`, `SendReconfirmBulkEmailJob.cs`, `Domain/Entities/BulkEmailWorkItem.cs`, and `Domain/Entities/EmailRecipientList.cs` show the prior shape. We carry forward the *idea* of a tracked work item with status, drop the pre-uploaded recipient lists, and move authorship into the Email module.

Stakeholders: organizers (initiate sends, monitor results), attendees (recipients), platform operator (audit trail).

## Goals / Non-Goals

**Goals:**

- One bulk-send pipeline that supports reconfirm, organizer-initiated custom sends, and external invitation lists.
- Per-bulk-send observability: status, totals, per-recipient log, audit of who triggered it.
- Strong idempotency under at-least-once delivery, including a redelivered job trigger.
- No persistent "recipient list" entity. Recipient set for a bulk send is materialised once and frozen on the job.
- **Single SMTP connection per fan-out**: bulk dispatch SHALL open one SMTP connection (per worker pickup of a job) and stream all messages through it; do NOT publish one `SendEmailCommand` per recipient. `EmailLog` writes still happen per recipient.
- Reuse the existing template-resolution precedence (event > team > built-in default) for bulk emails.
- Allow ad-hoc subject + body in the request to override the resolved template's subject/body for one send.
- Allow the `reconfirm` flow to be re-driven by simply re-running the scheduled job — no new outbox events per recipient.

**Non-Goals:**

- Per-recipient personalization beyond the existing template-parameter mechanism (no per-recipient template overrides).
- Saved/named recipient lists. (Removed; the legacy `EmailRecipientList` model does not return.)
- Send-time test mode (UI sends to a single test address through a normal single-send path instead).
- Throttling/rate-limit policies in this change. (May be a follow-up if SMTP providers force it.)
- Tracking opens/clicks/bounces. (Out of scope; only sent/failed at the SMTP layer.)
- Recurring "marketing campaigns" with multiple sequenced sends.

## Decisions

### D1 — Bulk-send is owned by the Email module

**Decision**: The `BulkEmailJob` aggregate, the recipient resolver, and the worker that fans out `SendEmailCommand`s all live inside `Admitto.Module.Email`.

**Alternatives considered**:

- *Registrations owns it*: would couple Registrations to template/SMTP concerns and force it to stage many `SendEmailCommand`s — that's email-shaped work.
- *New `Bulk` module*: adds a third module that itself depends on both Email and Registrations. We don't have enough orthogonal logic to justify a module boundary; the bulk fan-out is fundamentally an Email concern that sometimes asks Registrations a question.

**Why Email**: every operation here is "email-shaped" (template, SMTP, send-history). Registrations exposes only the data the resolver needs through a facade.

### D2 — Cross-module read via extended `IRegistrationsFacade`, not a direct DbContext query

**Decision**: Add `IRegistrationsFacade.QueryRegistrationsAsync(eventId, QueryRegistrationsDto)` returning a flat projection (`email`, `displayName`, `registrationId`, `ticketTypeSlugs[]`, `additionalDetails`, `hasReconfirmed`). The query DTO carries filters (ticket-type slugs, registration status, reconfirmed-or-not, etc.). No raw `Registration` entities cross the boundary. The name is intentionally generic — this query is reusable beyond bulk email (e.g. registration listing pages).

The existing `GetTicketedEventEmailContextAsync` is unchanged; reconfirm bulk fan-out already has the `RegistrationId` from the resolved snapshot, so no per-recipient facade lookup is added.

**Alternatives considered**:

- *Email reads `RegistrationsDbContext` directly*: violates arc42 §8.4 (no cross-module DbContext access).
- *Push model — Registrations publishes integration events for each candidate*: enormous amplification, fragile against state drift, terrible for ad-hoc selections.
- *Event-carried state on the bulk-job itself, supplied by the API*: forces the API endpoint to do the resolution, duplicating Registrations query logic into the API host and losing the "criteria are revalidated at send time" property.

**Why facade query**: it matches the established pattern (`IRegistrationsFacade.GetTicketedEventEmailContextAsync`), keeps all SQL inside Registrations, and lets the caller stay in-process within the Worker.

### D3 — Recipient resolution snapshots once and freezes for the lifetime of the job

**Decision**: When a `BulkEmailJob` transitions from `Pending` → `Resolving`, the resolver calls the facade and persists a snapshot of resolved recipient rows on the job (`BulkEmailRecipient` value objects: email, displayName, registrationId?, parametersJson). Subsequent retries of the fan-out re-read the snapshot; they do not re-query Registrations. Only the *initial* resolution is dynamic.

**Alternatives considered**:

- *Re-resolve on every retry*: introduces non-determinism (registrations cancelled mid-send disappear from the list, new ones appear). Breaks the audit promise: "this is who we sent to."
- *Resolve in the API request and embed in the job*: same problem as D2's API-side variant — duplicates query logic; can't easily run reconfirm scheduled jobs without an API request.

**Why snapshot-on-start**: organizers expect "I clicked send at 14:02, here's the list as it was at 14:02." Reconfirm jobs run on a fixed cadence — each tick resolves "who should be reconfirmed *now*", which is naturally the at-tick state.

### D4 — Direct SMTP fan-out over one connection, with per-recipient `EmailLog` idempotency

**Decision**: The bulk fan-out worker, having loaded a `BulkEmailJob` and its frozen `Recipients` snapshot, opens a **single** SMTP connection (via the existing SMTP sender abstraction, but called in a "session" mode) and iterates the snapshot, sending each message on the same connection. For every recipient it writes an `EmailLog` row with `IdempotencyKey = "bulk:{bulkJobId}:{normalizedEmail}"`. The existing unique index on `(ticketed_event_id, recipient, idempotency_key)` makes the per-recipient write idempotent under worker re-runs.

The single-send pipeline (`SendEmailCommand` published from integration-event handlers) is **not** reused for bulk fan-out — that pipeline opens an SMTP connection per command and would be wasteful at scale.

The aggregate's `Recipients` snapshot tracks per-recipient send state (`Pending` / `Sent` / `Failed`). The worker iterates only `Pending` rows so a resumed job picks up where it left off without re-sending.

**Alternatives considered**:

- *Per-recipient `SendEmailCommand` published to the outbox* — simpler to write, but every command opens its own SMTP connection; for a 5000-recipient send that's 5000 TLS handshakes.
- *Single connection but no per-recipient persistence* — loses resumability after a worker crash.
- *Per-recipient key derived from criteria* — fragile if criteria are edited; reconfirm sends would collide between cadence ticks.

**Why this design**: matches how SMTP is meant to be used (one connection, many `MAIL FROM`/`RCPT TO`/`DATA` exchanges), keeps `EmailLog` as the single write-side audit, and uses the recipient snapshot's per-recipient state for resume-after-crash.

### D5 — Reconfirm eligibility is determined fresh on every tick from `HasReconfirmed`; no `EmailLog` cadence filtering

**Decision**: Each tick of the per-event Quartz trigger creates a fresh `BulkEmailJob` and resolves eligible recipients live from `IRegistrationsFacade.QueryRegistrationsAsync` with filters `Status=Registered AND HasReconfirmed=false`. The cadence is encoded entirely in the cron schedule of the per-event Quartz trigger; no additional "have we asked them recently?" check against `email_log` is performed.

This means:

- Attendees who registered between ticks are picked up on the next tick.
- Attendees who reconfirm between ticks fall out of the candidate set on the next tick (they no longer match `HasReconfirmed=false`).
- Attendees who have not reconfirmed receive one prompt per cron firing — exactly the user-visible "every cadence period" behaviour.

**Alternatives considered**:

- *Filter candidates against `email_log` for "no successful reconfirm row younger than Cadence"*: redundant with the cron schedule. The cron already enforces "fire every Cadence period". Layering an additional log-based filter just duplicates the same constraint and complicates the resolver.
- *Project a "last reconfirmed at" onto Registrations*: cross-module write loop just to remember a date; `HasReconfirmed` already conveys the only fact we care about.

**Why this**: simplest model that matches the user's intent. The cron is the cadence; `HasReconfirmed` is the eligibility gate; there is exactly one source of truth for each.

### D6 — Reconfirm trigger: a per-event Quartz trigger driven off `TicketedEventReconfirmPolicy`

**Decision**: A single Quartz job `EvaluateReconfirmJob` is registered statically with the Email module. For each event with an active reconfirm policy, a per-event Quartz **trigger** is registered with the cron derived from the policy window + cadence. The trigger is created/updated/removed in response to:

- `TicketedEventCreated` integration event from Registrations (initial registration).
- `TicketedEventReconfirmPolicyChanged` **module event** (new — published by Registrations when the policy is set or cleared on an active event).
- `TicketedEventCancelled` / `TicketedEventArchived` integration events (cleanup).

When the trigger fires, it creates a `BulkEmailJob` with a `Reconfirm` source for that event and queues it for the same fan-out path as custom sends.

**Alternatives considered**:

- *One global tick + scan all events*: simpler scheduling, more wasted work, harder to reason about per-event windows.
- *Module event from the Email module subscribing to* `TicketedEventLifecycleChanged`: needs the same cleanup logic anyway; the proposed approach makes the dependency explicit.

**Why per-event trigger**: cron expressions cleanly model "during the window, every N hours starting at start-of-window."

### D7 — Recipient source is exactly one of two shapes — attendee or external list

**Decision**: A `BulkEmailJob.Source` is one of:

```text
AttendeeSource(filters)        // resolved via IRegistrationsFacade.QueryRegistrationsAsync
ExternalListSource(items)      // literal [(email, displayName?)] supplied at request time
```

A single job carries exactly one source; there is no "combined" shape. If an organizer needs to email both a registration-derived set and an external list, they create two bulk jobs. This keeps the audit record unambiguous (each job has one provenance) and removes the deduplication edge-case complexity.

**Alternatives considered**:

- *Combined source* (initial proposal): added complexity (union semantics, dedup, mixed provenance per recipient) for a use case that's cleanly expressed as two separate jobs.
- *Free-form recipient list always provided by the API*: pushes the resolver into the API and loses the snapshot-at-resolve semantics for attendee sources.

### D8 — Ad-hoc subject/body lives on the job, not in `EmailTemplates`

**Decision**: The `BulkEmailJob` carries `(EmailType, Subject?, TextBody?, HtmlBody?)`. If `Subject`/`TextBody`/`HtmlBody` are present, they override the template-resolved values for this single send. They are **not** persisted as a new `EmailTemplate` row.

**Alternatives considered**:

- *Always require a template*: forces organizers to save a template they never want to reuse.
- *Promote ad-hoc content to a hidden template*: clutters the template store with single-use rows.

### D9 — Cooperative cancellation of a running job + per-message delay

**Decision**: A `BulkEmailJob` may be cancelled at any time before it reaches a terminal state (`Completed`, `Failed`, `Cancelled`). Cancellation is **cooperative**: an admin call (`POST /admin/.../bulk-emails/{id}/cancel`) sets `CancellationRequestedAt` on the aggregate and persists. The fan-out worker checks this flag between recipients (which is cheap because the bulk pipeline owns its own loop — see D4). On observing the request:

1. The worker stops sending further messages.
2. Any `Recipients` rows still in `Pending` are transitioned to `Cancelled`.
3. The job transitions to `Cancelled` with `CancelledAt` set; `RecipientCount`, `SentCount`, `FailedCount`, `CancelledCount` are finalised.
4. The single SMTP connection is closed cleanly (`QUIT`).

To make cancellation responsive and to be polite to SMTP relays, the worker SHALL wait a configurable delay (default `BulkEmailOptions.PerMessageDelay = 500ms`, configurable via standard options binding) between each recipient send. The cancellation check happens immediately before each send and during the delay (delay is implemented as `Task.Delay(PerMessageDelay, ct)` on a CancellationToken that fires when the job-level cancellation is observed), so the worker reacts within at most one delay cycle.

Recipients already sent before cancellation remain `Sent` (the email was delivered to the SMTP relay; we cannot un-send). The audit therefore shows a partial outcome: e.g. `RecipientCount=5000, SentCount=312, CancelledCount=4688`.

This is feasible only because D4 chose direct SMTP fan-out inside the worker (single connection, single loop). If we had used per-recipient `SendEmailCommand` over the outbox, cancellation would be unreliable.

**Alternatives considered**:

- *Cancel only before fan-out begins*: simpler, but unhelpful — the user-visible reason to cancel is precisely "the send is taking long / I made a mistake" *during* the send.
- *Cancel by short-circuiting the entire SMTP session immediately*: leaves the remote in an undefined state and gives no useful audit. Cooperative cancellation between recipients is cleaner.
- *No per-message delay*: faster, but increases the chance of being throttled or blacklisted by relays for large sends, and makes cancellation feel sluggish on small lists.

### D10 — One worker drives one job at a time, but multiple jobs in parallel

**Decision**: Fan-out runs in a Quartz job with `[DisallowConcurrentExecution]` keyed *per `BulkEmailJob`* (using a per-job trigger group), allowing different jobs to run concurrently while preventing two workers from racing on the same job. Combined with D4's single-SMTP-connection per pickup, this means at most one SMTP connection per active bulk job per worker process.

### D11 — Add a required `TimeZone` to `TicketedEvent` to anchor cadence-based scheduling

**Decision**: Promote a per-event IANA `TimeZone` to a first-class field on `TicketedEvent`. The reconfirm trigger's cron is evaluated in this zone. Without it, a "daily at 09:00" reconfirm would either drift across DST (if scheduled in UTC) or implicitly assume the operator's server zone — both surprising to organizers.

This is a small, opportunistic addition tied to bulk-email because reconfirm-sending is the first feature that actually *uses* event-local clock time. Future features (calendar attachments, time-zone-aware email rendering, ticket countdowns) benefit immediately.

**Alternatives considered**:

- *Defer time-zone to a separate change*: would block reconfirm-sending or force it into UTC, which is unfriendly. Adding the field now keeps the change cohesive.
- *Reuse team-level time zone*: events within a team can legitimately span zones (e.g. a team running global webinars). Per-event is the right grain.
- *Store all datetimes as local with a separate zone id*: stays consistent with existing UTC-`DateTimeOffset` storage; only adds the zone for *display and scheduling* interpretation.

### D12 — Registration carries first/last name, lifecycle status, and reconfirm flag

**Decision**: Extend the `Registration` aggregate (Registrations module) with required `FirstName`/`LastName` value objects (replacing email-local-part-derived display names), a `Status` enum (`Registered`, `Cancelled`) with a `Cancel(reason)` domain method, and a `HasReconfirmed`/`ReconfirmedAt?` pair with a `Reconfirm()` domain method. These are first-class, addressable fields on the aggregate — not facade-only projections.

This change introduces the first feature (bulk-email + reconfirm-sending) that needs to *filter* registrations by status and reconfirm state and to *project* attendee names into bulk-email recipient snapshots. The Admin UI registrations list already pretends these fields exist (it derives a display name from the email local-part and hard-codes the Status column to "Confirmed" and the Reconfirm column to "—"); this change makes them real.

**Alternatives considered**:

- *Keep email-derived display names; add only `Status`/`HasReconfirmed`*: organizers want to address attendees by their actual names in bulk emails (e.g. "Hi Alice,"), and the Admin UI's placeholder already signals the gap. Splitting names out now avoids a second migration.
- *Single `DisplayName` field*: less structured; templates and the admin UI both want to show / sort / filter on family name vs given name independently. First/last name are also what existing registration channels naturally collect.
- *Defer to a separate change*: would block bulk-email's `AttendeeSource` filters (`status=Registered`, `hasReconfirmed=false`) and the reconfirm flow's eligibility query — both are core to this change.

**Migration**: EF migration adds the new columns. Existing rows are backfilled in the same migration as a one-shot SQL statement: `Status='Registered'`, `HasReconfirmed=false`, and `FirstName`/`LastName` derived from the email local-part split on `.` (e.g. `jane.doe@example.com` → `Jane`/`Doe`; single-token local-parts go to `FirstName` with `LastName='-'`). Public self-registration and admin-add request DTOs require both names from this point on; the legacy `Admitto.Application` project is untouched per task 12.1.

## Risks / Trade-offs

- **Risk: Snapshot becomes stale during a slow send.** A registration cancelled at minute 0 still receives the bulk email at minute 5. → **Mitigation**: Acceptable for v1 — organizers expect "the list at the time I clicked send." Document in spec; add a follow-up issue if it bites.
- **Risk: `EmailLog`-based cadence check**. Removed in D5 — eligibility is now determined entirely by `HasReconfirmed` + the cron schedule. No special index is required for reconfirm.
- **Risk: External-list emails to people who never consented.** → **Mitigation**: This is an organizer/policy concern, not a technical one. Add a UI confirmation step ("These addresses will be emailed without registration; you assert you have permission") in the Admin UI capability spec. Audit trail (who triggered, when, how many) gives the platform operator recourse.
- **Risk: Reconfirm cadence collisions when the policy is shortened.** Tightening cadence from 7d → 3d means the per-event Quartz trigger fires more often; un-reconfirmed attendees see prompts more frequently from that point. This is correct behaviour: the cron *is* the cadence. Document explicitly so organizers expect it.
- **Risk: Quartz trigger drift between hosts** (multiple Worker instances with the existing `quartz-db` clustering). → **Mitigation**: Clustered Quartz already handles this; the `[DisallowConcurrentExecution]` decoration prevents double-fanout on a single job.
- **Trade-off: No saved recipient lists.** Organizers who reuse the same external list for several events must paste it again. Acceptable: the legacy implementation barely used the persistence anyway, and the audit snapshot remains.
- **Trade-off: Resolution lives inside the Worker host.** Admins clicking "send" don't get an immediate "your list resolved to N recipients" — they get a "started" response and have to refresh to see the resolved count. → **Mitigation**: Provide a separate `POST /admin/.../bulk-emails/preview` endpoint that runs the same facade query synchronously and returns the count and a sample of (say) up to 100 recipients, without creating a job.

## Migration Plan

- New schema additions only: a new `email.bulk_email_jobs` table and one nullable column on `email.email_log` (`bulk_email_job_id`). No data backfill needed (existing single-sends remain `bulk_email_job_id IS NULL`).
- Standard EF Core migration via `Admitto.Migrations`.
- Legacy `EmailRecipientList` and related code in `src/Admitto.Application/` (orphaned legacy project, not in the active solution) is left untouched — no removal as part of this change.
- Rollback: drop `email.bulk_email_jobs` and the new column on `email.email_log`. No data integrity impact on single-send flows.

## Open Questions

- **OQ1**: Should the reconfirm Quartz trigger respect a per-event time-zone for the cron? (Legacy hardcodes Europe/Amsterdam.) — Likely yes; out of scope for this change unless trivial.
- **OQ2**: Do we need a maximum-recipients guard on a single `BulkEmailJob` (e.g. reject > 5000 to prevent SMTP throttling)? — Probably yes as configuration, but propose a simple constant for v1.
- **OQ3**: Should the `BulkEmailRecipient` snapshot include the `displayName` we'll address the email *to*, or rely on the template to render names? — Snapshot includes `displayName` so external list entries can carry one; templates can still ignore it.
