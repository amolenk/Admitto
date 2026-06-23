## Why

The current e-mail flow prevents duplicate log rows but can still send duplicate e-mails when a worker crashes after SMTP success and before committing `EmailLog`, or when duplicate deliveries race before either worker commits. The outbox also documents background retry for pending messages, but orphaned outbox dispatch is not implemented, so accepted registration-triggered e-mails can be stranded.

## What Changes

- Harden registration-triggered, cancellation, OTP, coupon, waitlist, and tickets-changed single e-mail sends by splitting preparation from delivery: the trigger handler writes the pending `EmailLog` row and enqueues an internal delivery command, while the delivery handler performs SMTP only after that claim is committed.
- Make the `EmailLog` unique index the authoritative claim mechanism, not just a post-send deduplication guard.
- Add bounded inline SMTP retries in the delivery handler, followed by retry-safe re-enqueueing/scheduling when attempts are exhausted and policy still allows another try.
- Add retry-safe handling for claimed-but-not-final e-mail log rows so transient SMTP failures and worker crashes do not permanently suppress sends.
- Implement background dispatch of pending outbox rows so messages committed to a module outbox are eventually delivered even if the immediate best-effort flush fails.
- Harden bulk e-mail per-recipient deduplication by writing a pending `EmailLog` row before each recipient SMTP send, while keeping bulk delivery inside the existing Quartz fan-out and single SMTP session rather than creating per-recipient queue messages.
- Update architecture/spec documentation to state the practical guarantee: at-least-once trigger delivery with database-backed send claiming, minimizing duplicates under SMTP's non-transactional constraints.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: Clarify and strengthen idempotency, retry, and outbox reliability requirements for single-send and bulk e-mail paths.

## Impact

- `Admitto.Core/Email` single-send handlers, `SendEmailHandler`, `EmailLog`, and bulk fan-out job behavior.
- New internal Email delivery command/handler used after the pending `EmailLog` row is committed.
- Shared outbox infrastructure under `Admitto.Core/Shared/Infrastructure/Persistence/Outbox` and Worker background processing.
- EF Core model/migrations for any additional e-mail log claim/lease metadata needed to distinguish pending, sending, sent, failed, and retryable states.
- Integration tests for redelivery, concurrent duplicate processing, stranded outbox rows, and bulk dedup recovery.
- `docs/arc42/06-runtime-view.md` and related e-mail sending sections, if runtime flow diagrams or guarantees change.
