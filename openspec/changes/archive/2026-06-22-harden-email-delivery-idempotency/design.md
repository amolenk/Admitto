## Context

Admitto already uses module outboxes, Service Bus at-least-once delivery, and an `EmailLog` unique index for e-mail idempotency. The current single-send path sends SMTP before committing the `EmailLog` row, so the log prevents duplicate rows but cannot prevent duplicate SMTP sends when a worker crashes or when duplicate deliveries race. The shared outbox also only flushes messages immediately after a successful unit-of-work commit; pending rows from a failed best-effort flush are indexed but not currently dispatched by a background worker.

SMTP cannot participate in the application database transaction and does not provide a portable idempotency key. The best achievable guarantee is therefore not mathematical exactly-once delivery, but database-backed claiming that prevents duplicate workers from sending the same logical e-mail where the database can decide first, plus retry/recovery for stranded claims and outbox rows.

## Goals / Non-Goals

**Goals:**

- Ensure a worker obtains a durable `EmailLog` send claim before attempting SMTP for single-send e-mails.
- Ensure duplicate deliveries and concurrent workers consult the same claim and only one proceeds to SMTP while a claim is active or terminal.
- Preserve retry for transient SMTP failures and worker crashes without losing accepted trigger messages.
- Dispatch pending module outbox rows in the Worker so a failed immediate flush does not strand e-mail triggers.
- Apply the same claim-first principle to bulk e-mail recipients and fix duplicate-log recovery so the job can continue after a dedup conflict.
- Document the practical e-mail delivery semantics honestly: no lost accepted triggers under normal infrastructure operation, and duplicate SMTP sends minimized within SMTP constraints.

**Non-Goals:**

- True end-to-end exactly-once SMTP delivery.
- Provider-specific idempotency integrations or replacing SMTP with an API-based provider.
- Changing public registration, coupon, OTP, cancellation, or waitlist contracts.
- Moving Keycloak account-action e-mails into the Email module.

## Decisions

1. Split e-mail preparation from SMTP delivery with an internal outbox command.

   The integration-event handler will still dispatch an in-memory `SendEmailCommand`, but `SendEmailCommandHandler` will only prepare durable work: create or acquire the `EmailLog` row keyed by `(TicketedEventId, Recipient, IdempotencyKey)` for event-scoped mail and `(Recipient, IdempotencyKey)` for system mail, render or record deterministic preparation failures, and enqueue an internal `DeliverEmailCommand` in the Email module outbox. It will not call SMTP. The existing queue/message dispatcher then commits the Email unit of work, preserving the endpoint/message-handler-owned transaction boundary.

   `DeliverEmailCommandHandler` is the only handler that calls SMTP. It loads the already-committed `EmailLog` row, verifies that delivery is still due and not terminal, performs SMTP delivery, updates the row to `Sent` or retryable/failed state, and returns so the message dispatcher commits the update.

   Alternative considered: have `SendEmailCommandHandler` insert the row, commit directly, then send. This violates the current unit-of-work convention because handlers must not own commits.

2. Use `EmailLog` as the durable send claim.

   The unique index remains the authoritative concurrency guard. If preparing the log row wins, a `DeliverEmailCommand` is enqueued. If preparing loses, the handler reloads the existing row and only enqueues delivery when the row is retryable or stale. A terminal row is a no-op.

   Alternative considered: keep send-before-log and catch unique violations. This preserves duplicate-log prevention but cannot prevent duplicate SMTP sends because the external side effect already happened.

3. Add explicit claim lifecycle metadata instead of overloading `Sent` and `Failed`.

   Extend the e-mail log model with enough state to distinguish claimed/sending, sent, deterministic failure, transient failure, and stale in-progress work. Exact naming can be finalized during implementation, but the model needs at least a claim timestamp and retry/last-error metadata. Existing `Pending`, `Sent`, and `Failed` values can be reused where appropriate if their semantics are made precise.

   Alternative considered: insert a `Pending` log and never retry it if the worker dies. That prevents duplicates but can permanently suppress an e-mail after a pre-SMTP crash.

4. Treat deterministic failures as terminal and transient SMTP failures as retryable.

   Missing/invalid settings and render errors are deterministic for the current payload/template snapshot and should write a terminal `Failed` log and acknowledge the queue message. SMTP transport errors should not create a terminal suppressing failure on the first attempt. `DeliverEmailCommandHandler` should perform a small bounded number of immediate retries for transient SMTP errors in the same handler invocation to absorb short hiccups without another queue roundtrip. If those attempts fail, it should update retry metadata and enqueue another `DeliverEmailCommand` only when a retry is still allowed.

   To avoid a tight immediate retry loop, the deferred retry needs either scheduled queue delivery support in the outbox sender or a due-time check backed by a retry scanner. If scheduled Service Bus send is added, `DeliverEmailCommand` can carry or derive the next attempt time and the outbox sender can schedule it. If scheduled send is deferred, a Worker retry job should scan due retryable `EmailLog` rows and enqueue `DeliverEmailCommand` at the due time.

   Alternative considered: continue logging any SMTP exception as `Failed` and throwing. That combines a terminal log with queue retry and can cause redelivery to skip future attempts because a log row already exists.

5. Keep the performance cost bounded and acceptable for single-send e-mails.

   Single-send e-mails will add one extra durable message boundary: trigger handler commit (`EmailLog` + `DeliverEmailCommand`) followed by delivery handler commit (`Sent`/retry state). This is additional database and queue work, but these paths are per registration/OTP/coupon rather than high-throughput bulk fan-out. The trade-off is appropriate because it is the point where the system moves from accepting responsibility for an e-mail to performing an irreversible external side effect.

   The implementation should avoid extra cross-module queries in `DeliverEmailCommandHandler`: all rendered content or all data needed to render deterministically should be captured before enqueueing delivery. For registration/cancellation templates that currently query `IRegistrationsFacade`, that query stays in the preparation step, not in every delivery retry.

   Alternative considered: deliver directly from the integration-event handler for lower latency. This is faster but keeps the crash/concurrency duplicate window that this change is intended to close.

6. Implement orphaned outbox dispatch as Worker-owned background processing.

   Add a hosted service or scheduled job that scans each module outbox for `Pending` rows, sends them to Service Bus, and marks them `Sent` after successful send. It should use bounded batches and concurrency-safe selection so multiple Worker instances can run without duplicate marking conflicts. Duplicate queue messages are acceptable because downstream handlers are idempotent.

   Alternative considered: rely only on immediate outbox flush from request handlers. This contradicts the documented outbox guarantee and loses messages whenever the process or Service Bus send fails after the database commit.

7. Harden bulk fan-out without per-recipient queue messages.

   Bulk e-mail should not enqueue one `DeliverEmailCommand` per recipient by default. That would turn one bulk job into thousands of queue messages and lose the current single SMTP connection optimization. The Quartz `SendBulkEmailJob` remains the delivery orchestrator: it resolves the snapshot once, opens one SMTP session per pickup, and processes recipients sequentially. For each recipient, it acquires and commits the `EmailLog` claim before SMTP send, then updates the log and recipient snapshot after the send. A pre-existing terminal log should mark the recipient outcome without sending. A duplicate active claim should not send. On duplicate insert conflicts, the job must detach or clear failed tracked entities before continuing so final `SaveChanges` does not rethrow the same unique violation.

   For transient per-recipient SMTP errors, the bulk job can retry a small number of times inline while the SMTP session is open. If the recipient still fails, record that recipient as failed and continue; bulk retry of only failed recipients can be handled by a later explicit retry feature, not by creating one command per recipient in this change.

   Alternative considered: rely solely on `BulkEmailRecipientStatus`. That helps resume-after-crash but does not protect against log/pre-existing row races and leaves known tracked-entity recovery issues.

## Risks / Trade-offs

- [Risk] A worker can crash after SMTP success but before marking the claim `Sent`. A later stale-claim recovery may resend. → Mitigation: use conservative stale-claim timeouts, record provider message IDs when available, and document that SMTP prevents a perfect guarantee.
- [Risk] Claim-before-send adds extra database and queue work around single e-mail delivery. → Mitigation: keep the extra message boundary only for single-send correspondence where correctness matters more than latency, and keep bulk delivery inside the existing Quartz fan-out path.
- [Risk] Existing `Failed` log semantics may need migration. → Mitigation: preserve historical rows as terminal observations and only apply retryable semantics to new claim states/metadata.
- [Risk] Outbox background dispatch can produce duplicate queue messages if a send succeeds but marking `Sent` fails. → Mitigation: queue handlers must remain idempotent; this is the normal outbox trade-off.
- [Risk] Multiple module outboxes require repetitive scanning code. → Mitigation: keep the dispatcher generic over `IOutboxDbContext` and register module-specific workers through existing module database services.

## Migration Plan

1. Add EF migration for any new `EmailLog` status values and claim/retry metadata.
2. Deploy code that can read old `EmailLog` rows and treats existing `Sent`/`Failed` rows as terminal unless explicitly retryable metadata exists.
3. Enable orphaned outbox dispatch in Worker after the scanner is covered by integration tests.
4. Rollback strategy: stop Worker outbox scanner first, then roll back application code. Database columns can remain unused if rollback is needed.

## Open Questions

- What exact stale-claim timeout should be used for single-send and bulk e-mails?
- Should transient SMTP retry limits live on `EmailLog`, queue delivery count, configuration, or a combination?
- Should a deterministic missing-settings failure be terminal forever, or should operators be able to manually retry after settings are fixed?
