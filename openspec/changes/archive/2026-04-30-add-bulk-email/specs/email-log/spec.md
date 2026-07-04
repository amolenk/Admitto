## MODIFIED Requirements

### Requirement: Every send attempt is recorded in the email log

The Email module SHALL persist one row in the `email_log` table for every email it attempts to send, regardless of whether it was a single-send (e.g. registration confirmation) or part of a bulk fan-out. Each row SHALL record at minimum: id, team id, ticketed event id, idempotency key, recipient, email type, subject, provider (sender implementation name), status, sent-at, status-updated-at, last-error (nullable), and `bulk_email_job_id` (nullable). Status SHALL be one of `Sent`, `Delivered`, `Bounced`, or `Failed`.

`bulk_email_job_id` SHALL be set to the parent `BulkEmailJob.Id` for any send dispatched as part of a bulk fan-out (the bulk worker writes the row directly), and SHALL be null for single-recipient sends triggered by integration events such as `AttendeeRegistered`.

#### Scenario: Successful single send is logged with null bulk job id
- **WHEN** the SMTP sender successfully delivers a `ticket` email to "alice@example.com" for event "DevConf" in response to an `AttendeeRegistered` integration event
- **THEN** an `email_log` row exists with status=`Sent`, sent_at=now, recipient="alice@example.com", email_type="ticket", non-null provider name, and `bulk_email_job_id` is null

#### Scenario: Successful bulk send is logged with the parent job id
- **WHEN** the bulk fan-out worker successfully delivers a `reconfirm` email to "alice@example.com" as part of bulk job `B`
- **THEN** an `email_log` row exists with status=`Sent` and `bulk_email_job_id = B`

#### Scenario: Render failure is logged as Failed
- **WHEN** template rendering fails before any SMTP I/O is attempted
- **THEN** an `email_log` row exists with status=`Failed`, sent_at=null, and last_error populated with the failure detail

#### Scenario: SMTP failure on a single recipient within a bulk send is logged as Failed
- **WHEN** within a bulk fan-out, one recipient's `MAIL FROM`/`RCPT TO`/`DATA` exchange fails with a terminal SMTP error
- **THEN** an `email_log` row exists for that recipient with status=`Failed`, `bulk_email_job_id` set to the parent job, and last_error populated; the worker continues with the next recipient on the same connection

---

### Requirement: Email log lives in the email schema and is owned by the Email module

The `email_log` table SHALL live in the `email` PostgreSQL schema and SHALL be read and written only by the Email module. No other module SHALL query it directly.

#### Scenario: Schema placement
- **WHEN** the EF migration runs
- **THEN** `email.email_log` exists in the database

#### Scenario: Cross-module read attempt
- **WHEN** another module needs to know whether an email was sent
- **THEN** it MUST go through an Email module facade, not query `email_log` directly
