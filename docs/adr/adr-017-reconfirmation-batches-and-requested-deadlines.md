# ADR-017: Recurring reconfirmation evaluation and requested deadlines

## Status

Accepted and implemented. Supersedes [ADR-016](adr-016-hourly-reconfirmation-evaluation.md). The bulk and reconfirmation portions of [ADR-009](adr-009-bulk-email-design.md) are superseded where they conflict with this decision.

## Context

ADR-016 couples reconfirmation to an hourly evaluator plus an additional Quartz trigger for the policy boundary. The existing bulk-email model also provides persisted recipient snapshots and resumable fan-out work. The confirmed design needs a smaller operational model: one recurring evaluator and a requested reconfirmation deadline. Generic bulk-email capability is not part of the target design.

## Decision

### Scheduling and deadline semantics

- Reconfirmation uses exactly one recurring clustered Quartz trigger. Its Worker restart-required interval is configured by `Email:Reconfirmation:Interval`, defaults to one hour, and must be at least one minute. There are no per-event reconfirmation triggers and no additional trigger for the requested deadline; changing the interval requires a Worker restart.
- `closesAt` is the requested reconfirmation deadline. It requests when terminal evaluation should occur; it is not an exact execution instant. The first subsequent configured-interval evaluation at or after `closesAt` performs the terminal evaluation.
- Quiet hours and the requested reconfirmation deadline only gate starting routine reconfirmation delivery. An active evaluation is allowed to finish and is not cancelled or suspended merely because quiet hours begin or the deadline passes.

### Execution coordination and delivery audit

- Mark the recurring Quartz job with `[DisallowConcurrentExecution]`. Quartz's clustered PostgreSQL-backed persistent scheduler acquires the recurring trigger on only one Worker node and prevents overlapping executions across the cluster.
- Do not persist a `ReconfirmationBatch` lifecycle, recipient snapshot, resumable work list, or batch audit record. Each recurring evaluation uses live Registrations authorization and one SMTP session for that evaluation.
- `EmailLog` remains the delivery-level audit and idempotency record. Its claims and cycle-scoped successful-delivery history determine whether an individual reconfirmation may be sent; they do not represent a batch lifecycle.
- If an evaluation is interrupted, it has no run state to resume. A later evaluation queries fresh candidates and relies on `EmailLog` delivery claims/history.

### Policy-close behavior

- Preserve the existing policy-close behavior. The first configured-interval evaluation at or after `closesAt` performs one durable terminal evaluation, creates no routine reconfirmation delivery work, ignores quiet hours and the minimum interval, and auto-cancels only registered, unreconfirmed attendees already at the effective maximum. Below-maximum attendees remain registered, and repeated interval ticks are no-ops for the terminal evaluation.

### Email scope

- Remove generic/admin bulk email, custom bulk contents, bulk snapshots and recipients, dynamic fan-out, and the associated UI/API surface.
- Normal transactional email remains. This decision removes durable reconfirmation batch state and generic bulk capability; it does not remove the normal transactional email flow.

### Deployment

Implementation is complete. The schema migration `20260829000004_RemoveGenericBulkEmail` removes the obsolete generic-bulk persistence, including the `bulk_email_jobs` table and its `EmailLog` foreign key. The forward removal migration `20260831000001_RemoveReconfirmationBatch` removes the obsolete `ReconfirmationBatch` persistence from the earlier batch-based implementation; it does not remove delivery-level `EmailLog` claims or history. The recurring evaluator retains the legacy durable Quartz job and trigger identities (`RequestReconfirmationsJob` and `RequestReconfirmationsJob.Hourly`) for in-place replacement. Quartz scheduling overwrite is enabled so the renamed job and configurable simple trigger replace the prior persisted definitions without duplicates.

## Rationale

- A single configurable recurring trigger keeps scheduler state fixed and makes a requested deadline honest about its time precision.
- Gating only the start of routine evaluation prevents a deadline or quiet-hours transition from abandoning delivery that has already begun.
- Live eligibility on each evaluation avoids stale recipient snapshots. `EmailLog` retains delivery history and idempotency claims without becoming a batch lifecycle or audit store.
- Keeping transactional email separate preserves the reliable, business-event-driven messages that are not reconfirmation batches.

## Consequences

### Positive

- Reconfirmation has one predictable scheduler entry point and no additional durable batch state.
- Clustered Quartz prevents overlapping executions, while interrupted work is reconsidered from current attendee state on a later evaluation.
- The generic/admin bulk surface and its snapshot, custom-content, and dynamic fan-out complexity are removed.

### Negative / tradeoffs

- A requested deadline is evaluated on the first subsequent configured-interval run rather than at an exact instant.
- An interrupted evaluation does not resume from a persisted run position; the next evaluation must select fresh live-eligible work and rely on `EmailLog` delivery history.

## Alternatives considered

- **Keep an exact-deadline Quartz trigger** — rejected because the deadline is a request for terminal evaluation, not a second scheduler cadence.
- **Persist recipient snapshots and resume batches** — rejected because it retains stale recipient state and requires bulk-job lifecycle and per-recipient persistence that the confirmed design does not need.
- **Persist a minimal reconfirmation batch lifecycle** — rejected because clustered Quartz with `[DisallowConcurrentExecution]` prevents overlapping executions, while `EmailLog` already provides the delivery-level audit and idempotency needed for individual sends.
