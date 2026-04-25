# ADR-009: Bulk-email fan-out design (snapshot recipients, single SMTP connection, per-event time zone)

## Status
Accepted.

## Context
The Email module historically only sent single transactional emails (one per `AttendeeRegisteredIntegrationEvent`). Two new use cases — organizer-initiated **custom bulk** sends and policy-driven **reconfirm** sends — require fan-out to many recipients with strong audit and resume guarantees.

Three sub-decisions in the design of the new `BulkEmailJob` aggregate (introduced by the `add-bulk-email` change) are large enough to warrant a permanent record:

1. **How is the recipient list materialised?** Live on every retry, snapshotted once, or supplied by the caller?
2. **How is fan-out actually performed?** One `SendEmailCommand` per recipient over the existing single-send pipeline (which opens an SMTP connection per command), or a dedicated bulk path that owns its own connection?
3. **What is the time reference for cadence-driven scheduling (reconfirm)?** UTC, the operator's server zone, the team's zone, or a per-event zone?

These three decisions are tightly coupled — snapshot semantics enable single-connection fan-out (the worker owns its own loop), and per-event time zones are the only sensible reference for cadence cron expressions like "daily at 09:00" once reconfirm becomes a real flow.

## Decision

### D1 — Recipients are snapshotted once when a `BulkEmailJob` starts and frozen for its lifetime

When a `BulkEmailJob` transitions `Pending → Resolving`, the resolver calls `IRegistrationsFacade.QueryRegistrationsAsync(eventId, filters)` (for an attendee source) or reads the embedded list (for an external source) and persists a `BulkEmailRecipient` snapshot on the job: `(email, displayName, registrationId?, parametersJson, status)`. Subsequent retries of the fan-out re-read the snapshot; they do **not** re-query Registrations.

Reconfirm jobs are scheduled per cadence tick — each tick is a fresh job, so the at-tick state is naturally captured.

### D2 — Bulk fan-out uses a single SMTP connection per worker pickup, with per-recipient `EmailLog` idempotency

`BulkEmailFanOutJob` opens **one** SMTP connection per pickup (via the existing SMTP sender in "session" mode) and streams every snapshot row through it. Per recipient it writes an `EmailLog` row keyed `bulk:{bulkJobId}:{normalizedEmail}`; the existing unique index on `(ticketed_event_id, recipient, idempotency_key)` makes each per-recipient write idempotent under worker re-runs.

The single-send pipeline (`SendEmailCommand` published from integration-event handlers) is **not** reused for bulk fan-out — that pipeline opens an SMTP connection per command and would be wasteful at any meaningful scale.

The aggregate's `Recipients` snapshot tracks per-recipient send state (`Pending` / `Sent` / `Failed` / `Cancelled`). The worker iterates only `Pending` rows so a resumed job picks up where it left off without re-sending.

### D3 — `TicketedEvent` carries a required IANA `TimeZone` for cadence-based scheduling

`TicketedEvent` gains a required `TimeZone` (IANA zone id) as a first-class field. The reconfirm Quartz trigger's cron is evaluated in this zone. Without it, a "daily at 09:00" reconfirm would either drift across DST (UTC) or implicitly assume the operator's server zone — both surprising to organizers. Per-event grain (rather than per-team) is correct because a single team can legitimately run events in different zones.

A new `TicketedEventTimeZoneChanged` integration event from Registrations triggers the Email module to recompute and re-register the per-event Quartz trigger.

## Rationale

- **Snapshot semantics give an honest audit.** "I clicked send at 14:02; here's the list as it was at 14:02" matches organizer expectations. Re-resolving on retry would let cancelled-mid-send registrations disappear and new ones appear, breaking that promise. Reconfirm jobs are independently honest because each tick *is* a fresh resolve.
- **Single SMTP connection matches how SMTP is intended to be used.** A 5,000-recipient send becomes one TLS handshake plus 5,000 `MAIL FROM` / `RCPT TO` / `DATA` exchanges instead of 5,000 TLS handshakes. Per-recipient `EmailLog` keeps the audit-and-idempotency story identical to single-send.
- **Cooperative cancellation works only because the worker owns the loop.** A `CancellationRequestedAt` flag on the aggregate is checked between recipients (and during the per-message delay) and a partial outcome is recorded. With per-recipient outbox commands, in-flight cancellation would be unreliable.
- **Per-event time zones are the simplest correct reference.** They survive DST, do not depend on operator infrastructure, and are needed regardless for future calendar attachments and zone-aware rendering.

## Consequences

### Positive
- One bulk-send pipeline serves both reconfirm and custom-send flows; the Email module has full ownership.
- Per-recipient `EmailLog` history can be answered by a single index on `(bulk_email_job_id)`.
- Resume-after-crash is automatic: re-pickups iterate only `Pending` snapshot rows.
- Reconfirm cron expressions like "daily at 09:00" mean what an organizer expects, year-round.

### Negative
- A registration cancelled between snapshot and last-recipient-sent still receives the email. Acceptable for v1; documented in the design.
- The bulk fan-out path is a second SMTP code path alongside single-send. Mitigated by reusing the SMTP sender abstraction in "session" mode.
- Adding a required column to `TicketedEvent` requires a one-shot backfill in the EF migration (existing rows default to a configurable platform zone).

### Neutral
- A configurable `BulkEmailOptions.PerMessageDelay` (default `500ms`) is added. It both improves cancellation responsiveness and reduces relay throttling risk.

## References
- arc42 chapter 5 — building-block view (Email module: bulk email, reconfirm sending).
- arc42 chapter 6 — runtime view (§6.9 bulk-email fan-out, §6.10 reconfirm scheduling).
- Change: `openspec/changes/add-bulk-email/` (proposal, design, specs).
