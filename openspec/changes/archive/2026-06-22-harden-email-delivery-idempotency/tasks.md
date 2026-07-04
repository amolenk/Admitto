## 1. Model And Migration

- [x] 1.1 Extend `EmailLog` with claim/retry metadata needed to distinguish active claim, sent, deterministic failure, transient failure, and stale in-progress work.
- [x] 1.2 Add an EF Core migration for the `EmailLog` changes using the official EF tooling.
- [x] 1.3 Update `EmailLogEntityConfiguration` indexes and constraints so existing idempotency keys remain authoritative for event-scoped and system e-mails.
- [x] 1.4 Update tests/builders that construct `EmailLog` rows to use the new model consistently.

## 2. Single-Send Claim Flow

- [x] 2.1 Refactor `SendEmailHandler` to prepare durable work only: create or acquire the `EmailLog` row and enqueue an internal `DeliverEmailCommand` in the Email outbox without calling SMTP.
- [x] 2.2 Add `DeliverEmailCommand` and `DeliverEmailCommandHandler` to perform SMTP delivery and update the already-committed `EmailLog` row.
- [x] 2.3 Make duplicate claim conflicts reload the existing log row and skip SMTP when the existing row is terminal or actively claimed.
- [x] 2.4 Mark deterministic no-settings and render failures as terminal failed logs without attempting SMTP and without poisoning the queue.
- [x] 2.5 Add bounded inline retries for transient SMTP transport failures in `DeliverEmailCommandHandler`.
- [x] 2.6 After inline retries are exhausted, update retry metadata and enqueue or schedule a follow-up `DeliverEmailCommand` only when retry policy allows it.
- [x] 2.7 Add stale-claim recovery logic for claims left unfinished by worker crashes before SMTP or before final log update.

## 3. Outbox Retry Processing

- [x] 3.1 Implement pending outbox row dispatch in shared outbox infrastructure with bounded batches.
- [x] 3.2 Register Worker-owned background processing for pending outbox rows across modules that implement `IOutboxDbContext`.
- [x] 3.3 Make outbox dispatch safe under multiple Worker instances and tolerate duplicate queue sends caused by send-success/mark-failure races.
- [x] 3.4 Add logging and configuration for batch size, polling interval, and retry error visibility.

## 4. Bulk Email Hardening

- [x] 4.1 Refactor `SendBulkEmailJob` to claim each recipient's `EmailLog` row before SMTP send without creating per-recipient queue messages.
- [x] 4.2 Make pre-existing terminal recipient logs skip SMTP and update the recipient snapshot consistently.
- [x] 4.3 Fix duplicate-log conflict handling so failed added entities are detached or cleared before later `SaveChanges` calls.
- [x] 4.4 Add bounded inline per-recipient retries for transient SMTP errors while preserving the single SMTP session per pickup.
- [x] 4.5 Preserve cancellation and resume-after-crash behavior while using the claim-first recipient flow.

## 5. Verification

- [x] 5.1 Add single-send integration tests for duplicate redelivery after `Sent`, concurrent duplicate processing before send, no-settings terminal failure, render terminal failure, and retryable SMTP failure.
- [x] 5.2 Add integration tests for stale claim recovery before SMTP and document the post-SMTP crash duplicate window.
- [x] 5.3 Add outbox integration tests proving pending rows are dispatched by Worker retry processing and tolerate duplicate scanner races.
- [x] 5.4 Add bulk fan-out integration tests for pre-existing logs, duplicate claim conflicts, cancellation, and resume-after-crash.
- [x] 5.5 Run architecture tests first, then targeted Email and Shared integration tests.

## 6. Documentation

- [x] 6.1 Update `docs/arc42/06-runtime-view.md` e-mail and outbox runtime flows to show claim-before-send and Worker outbox retry.
- [x] 6.2 Update `docs/arc42/08-crosscutting-concepts.md` if outbox retry or e-mail claim semantics become a shared convention.
- [x] 6.3 Update `openspec/specs/email-sending/spec.md` through archive after implementation is verified.
