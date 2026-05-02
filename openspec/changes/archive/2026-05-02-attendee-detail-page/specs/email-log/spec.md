## MODIFIED Requirements

### Requirement: Every send attempt is recorded in the email log

The Email module SHALL persist one row in the `email_log` table for every email it attempts to send, regardless of whether it was a single-send (e.g. registration confirmation) or part of a bulk fan-out. Each row SHALL record at minimum: id, team id, ticketed event id, idempotency key, recipient, email type, subject, provider (sender implementation name), status, sent-at, status-updated-at, last-error (nullable), `bulk_email_job_id` (nullable), and `registration_id` (nullable). Status SHALL be one of `Sent`, `Delivered`, `Bounced`, or `Failed`.

`bulk_email_job_id` SHALL be set to the parent `BulkEmailJob.Id` for any send dispatched as part of a bulk fan-out (the bulk worker writes the row directly), and SHALL be null for single-recipient sends triggered by integration events such as `AttendeeRegistered`.

`registration_id` SHALL be set to the GUID of the associated `Registration` for any send that is directly linked to a registration (single-send transactional emails triggered by registration events and bulk fan-out sends where `BulkEmailRecipient.RegistrationId` is non-null). It SHALL be null for external-list bulk sends and any send that is not tied to a specific registration.

#### Scenario: Successful single send is logged with null bulk job id

- **WHEN** the SMTP sender successfully delivers a `ticket` email to "alice@example.com" for event "DevConf" in response to an `AttendeeRegistered` integration event
- **THEN** an `email_log` row exists with status=`Sent`, sent_at=now, recipient="alice@example.com", email_type="ticket", non-null provider name, `bulk_email_job_id` is null, and `registration_id` is set to the registration's GUID

#### Scenario: Successful bulk send with registration is logged with both job id and registration id

- **WHEN** the bulk fan-out worker successfully delivers a `reconfirm` email to "alice@example.com" as part of bulk job `B` where Alice's `BulkEmailRecipient.RegistrationId` is non-null
- **THEN** an `email_log` row exists with status=`Sent`, `bulk_email_job_id = B`, and `registration_id` set to Alice's registration GUID

#### Scenario: External-list bulk send has null registration id

- **WHEN** the bulk fan-out worker delivers a bulk email to an external-list recipient (no associated registration)
- **THEN** the `email_log` row has `registration_id = null`

#### Scenario: Render failure is logged as Failed

- **WHEN** template rendering fails before any SMTP I/O is attempted
- **THEN** an `email_log` row exists with status=`Failed`, sent_at=null, and last_error populated with the failure detail

#### Scenario: SMTP failure on a single recipient within a bulk send is logged as Failed

- **WHEN** within a bulk fan-out, one recipient's `MAIL FROM`/`RCPT TO`/`DATA` exchange fails with a terminal SMTP error
- **THEN** an `email_log` row exists for that recipient with status=`Failed`, `bulk_email_job_id` set to the parent job, and last_error populated; the worker continues with the next recipient on the same connection
