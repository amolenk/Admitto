# email-log Specification

## Purpose
TBD - created by archiving change add-email-module. Update Purpose after archive.
## Requirements
### Requirement: Every send attempt is recorded in the email log
The Email module SHALL persist one row in the `email_log` table for every email it attempts to send. Each row SHALL record at minimum: id, team id, ticketed event id, idempotency key, recipient, email type, subject, provider (sender implementation name), status, sent-at, status-updated-at, and last-error (nullable). Status SHALL be one of `Sent`, `Delivered`, `Bounced`, or `Failed`; this change writes only `Sent` and `Failed`.

#### Scenario: Successful send is logged as Sent
- **WHEN** the SMTP sender successfully delivers a `ticket` email to "alice@example.com" for event "DevConf"
- **THEN** an `email_log` row exists with status=`Sent`, sent_at=now, recipient="alice@example.com", email_type="ticket", and a non-null provider name

#### Scenario: Render failure is logged as Failed
- **WHEN** template rendering fails before any SMTP I/O is attempted
- **THEN** an `email_log` row exists with status=`Failed`, sent_at=null, and last_error populated with the failure detail

#### Scenario: SMTP failure after retry exhaustion is logged as Failed
- **WHEN** the SMTP send fails permanently after the queue's retry policy is exhausted
- **THEN** an `email_log` row exists with status=`Failed` and last_error populated with the SMTP error

---

### Requirement: Email log enforces deduplication
The `email_log` table SHALL have a unique index on `(ticketed_event_id, recipient, idempotency_key)`. The unique index SHALL be the authoritative deduplication mechanism: a second insert with the same key SHALL be rejected by the database.

#### Scenario: Duplicate insert is rejected by the unique index
- **WHEN** code attempts to insert a second row with the same `(ticketed_event_id, recipient, idempotency_key)` triple
- **THEN** the database raises a unique-constraint violation, and the calling handler swallows it as "already sent"

#### Scenario: Same recipient, different idempotency key
- **WHEN** the same recipient is emailed for the same event with two different idempotency keys (e.g. one registration, one future bulk-reconfirm)
- **THEN** both rows persist successfully

---

### Requirement: Email log lives in the email schema and is owned by the Email module
The `email_log` table SHALL live in the `email` PostgreSQL schema and SHALL be read and written only by the Email module. No other module SHALL query it directly.

#### Scenario: Schema placement
- **WHEN** the EF migration runs
- **THEN** `email.email_log` exists in the database

#### Scenario: Cross-module read attempt
- **WHEN** another module needs to know whether an email was sent
- **THEN** it MUST go through an Email module facade, not query `email_log` directly

