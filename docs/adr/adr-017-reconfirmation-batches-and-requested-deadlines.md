# ADR-017: Reconfirmation batches and requested deadlines

## Status

Accepted and implemented. Supersedes [ADR-016](adr-016-hourly-reconfirmation-evaluation.md). The bulk and reconfirmation portions of [ADR-009](adr-009-bulk-email-design.md) are superseded where they conflict with this decision.

## Context

ADR-016 couples reconfirmation to an hourly evaluator plus an additional Quartz trigger for the policy boundary. The existing bulk-email model also provides persisted recipient snapshots and resumable fan-out work. The confirmed design needs a smaller operational model: one hourly evaluator, a requested reconfirmation deadline, and a minimal persisted reconfirmation batch. Generic bulk-email capability is not part of the target design.

## Decision

### Scheduling and deadline semantics

- Reconfirmation uses exactly one recurring Quartz trigger, running hourly. There are no per-event reconfirmation triggers and no additional trigger for the requested deadline.
- `closesAt` is the requested reconfirmation deadline. It requests when terminal evaluation should occur; it is not an exact execution instant. The first subsequent hourly evaluation at or after `closesAt` performs the terminal evaluation.
- Quiet hours and the requested reconfirmation deadline only gate starting new reconfirmation batches. An active batch is allowed to finish and is not cancelled or suspended merely because quiet hours begin or the deadline passes.

### Reconfirmation batch lifecycle

- Persist only the minimal `ReconfirmationBatch` state needed to record run lifecycle and prevent overlapping runs. It has no recipient snapshot, per-recipient resume state, or resumable work list.
- Each run uses one SMTP session for the run.
- If a batch is interrupted, it is marked failed rather than resumed. A later hourly evaluation creates fresh work from currently live-eligible attendees and uses `EmailLog` when determining what may be sent; it does not continue the failed batch.

### Email scope

- Remove generic/admin bulk email, custom bulk contents, bulk snapshots and recipients, dynamic fan-out, and the associated UI/API surface.
- Normal transactional email remains. This decision changes reconfirmation batching and removes generic bulk capability; it does not remove the normal transactional email flow.

### Deployment

Implementation is complete. The schema migration `20260829000004_RemoveGenericBulkEmail` removes the obsolete generic-bulk persistence, including the `bulk_email_jobs` table and its `EmailLog` foreign key. No legacy Quartz cleanup is required because the implementation uses one recurring hourly trigger and no per-event reconfirmation windows.

## Rationale

- A single hourly trigger keeps scheduler state fixed and makes a requested deadline honest about its time precision.
- Gating only batch starts prevents a deadline or quiet-hours transition from abandoning work that has already begun.
- Live eligibility on each new run avoids stale recipient snapshots. `EmailLog` retains the send history needed to avoid treating a failed or interrupted batch as resumable work.
- Keeping transactional email separate preserves the reliable, business-event-driven messages that are not reconfirmation batches.

## Consequences

### Positive

- Reconfirmation has one predictable scheduler entry point and a small persisted lifecycle record.
- Interrupted work fails cleanly and is reconsidered from current attendee state on a later hourly evaluation.
- The generic/admin bulk surface and its snapshot, custom-content, and dynamic fan-out complexity are removed.

### Negative / tradeoffs

- A requested deadline is evaluated on the first subsequent hourly run rather than at an exact instant.
- An interrupted batch does not resume from its prior position; the next evaluation must select fresh live-eligible work and rely on `EmailLog` history.

## Alternatives considered

- **Keep an exact-deadline Quartz trigger** — rejected because the deadline is a request for terminal evaluation, not a second scheduler cadence.
- **Persist recipient snapshots and resume batches** — rejected because it retains stale recipient state and requires bulk-job lifecycle and per-recipient persistence that the confirmed design does not need.
