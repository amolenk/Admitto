## MODIFIED Requirements

### Requirement: Bulk-email jobs are first-class aggregates with a tracked lifecycle
The Email module SHALL persist a `BulkEmailJob` aggregate (in the `email` schema) for every bulk send. Each job SHALL carry a unique `Id`, the owning `TeamId` and `TicketedEventId`, the `EmailType` (one of the canonical set including `reconfirm` and `bulk-custom`), the content fields required for its type, the resolved `Source` descriptor, the user identity that triggered it (or a system-user marker for scheduled reconfirm jobs), the resolved `Recipients` snapshot, the running totals `RecipientCount`/`SentCount`/`FailedCount`/`CancelledCount`, the `Status`, an optional `LastError`, timestamps `CreatedAt`/`StartedAt`/`CompletedAt`/`CancellationRequestedAt?`/`CancelledAt?`, and a `Version` token for optimistic concurrency.

For `bulk-custom`, `Subject`, `TextBody`, and `HtmlBody` SHALL all be required and persisted on the job as the immutable content snapshot. System-triggered built-in email types such as `reconfirm` MAY use code-owned built-in content instead of job-owned content.

Status SHALL be one of: `Pending` (created, not yet picked up), `Resolving` (recipient resolution in progress), `Sending` (recipients resolved, fan-out in progress), `Completed` (every recipient produced a sent or failed terminal state), `PartiallyFailed` (one or more recipients failed terminally but at least one succeeded), `Failed` (resolution failed or all recipients failed terminally), `Cancelled` (cancelled cooperatively at any point before reaching another terminal state).

#### Scenario: Custom job created in Pending state with complete content
- **WHEN** an organizer triggers a custom bulk send for event "DevConf" with `Subject`, `TextBody`, `HtmlBody`, and an attendee source
- **THEN** a new `BulkEmailJob` row is persisted with `Status=Pending`, `RecipientCount=0`, the trigger user id, the content fields, and the source descriptor

#### Scenario: Lifecycle transitions are linear
- **WHEN** a job moves through resolution and fan-out
- **THEN** observed transitions follow `Pending -> Resolving -> Sending -> (Completed | PartiallyFailed | Failed | Cancelled)` and never go backwards

#### Scenario: Cancellation requested while Pending or Resolving stops the job before any send
- **WHEN** a cancel request is issued against a job in `Pending` or `Resolving`
- **THEN** the aggregate sets `CancellationRequestedAt`, transitions to `Cancelled` once the worker observes the request (or immediately if not yet picked up), no fan-out occurs, and `SentCount=0`

#### Scenario: Cancellation requested while Sending stops the job between recipients
- **WHEN** a cancel request is issued against a job in `Sending` (e.g. with 312 of 5000 recipients already sent)
- **THEN** the aggregate sets `CancellationRequestedAt`, the worker observes the flag before the next recipient (within at most one per-message delay cycle), transitions remaining `Pending` recipient rows to `Cancelled`, and finalises the job to `Status=Cancelled` with `SentCount=312`, `CancelledCount=4688`

#### Scenario: Cancel against a terminal job is rejected
- **WHEN** a cancel request is issued against a job in `Completed`, `PartiallyFailed`, `Failed`, or `Cancelled`
- **THEN** the request is rejected with a domain error and the status is unchanged

### Requirement: Fan-out streams over a single SMTP connection per worker pickup
The bulk fan-out worker, upon picking up a `BulkEmailJob` with status `Sending`, SHALL open exactly one SMTP connection (per pickup) and stream every still-`Pending` recipient through it. The worker SHALL NOT publish per-recipient `SendEmailCommand` messages and SHALL NOT use the single-send command pipeline for bulk fan-out.

For each recipient the worker SHALL, in order: (1) render the message from the job-owned content for custom bulk sends or built-in content for system bulk sends, (2) acquire or observe the per-recipient `email_log` claim with `bulk_email_job_id` set and `IdempotencyKey = "bulk:{bulkJobId}:{normalisedRecipientEmail}"`, (3) skip SMTP when a terminal log already exists, (4) send it on the open SMTP connection when the claim is pending, (5) update the log row and per-recipient snapshot to `Sent` or `Failed`, (6) update the parent job's running `SentCount` / `FailedCount`. The existing unique index on `email_log` `(ticketed_event_id, recipient, idempotency_key)` SHALL ensure that a re-run of the fan-out for the same recipient is a no-op at the log level even if the per-recipient status update was lost.

The worker SHALL close the SMTP connection cleanly when the snapshot is exhausted, on cancellation, or on a connection-level failure.

Between consecutive recipient sends, the worker SHALL wait `BulkEmailOptions.PerMessageDelay` (configurable; default `500ms`). The wait SHALL be cancellable so that a cancellation request observed during the wait causes the worker to stop sending immediately without consuming the full delay.

Transient SMTP failures for a recipient SHALL be retried inline a bounded number of times using `BulkEmailOptions.InlineRetryCount`, with `BulkEmailOptions.InlineRetryDelay` between retry attempts. If the attempts are exhausted, that recipient SHALL be recorded as failed and the worker SHALL continue with the next recipient on the same SMTP connection.

#### Scenario: Single connection serves many custom-content recipients
- **WHEN** a 500-recipient custom bulk job is picked up by a single worker
- **THEN** the SMTP sender opens exactly one connection, renders job-owned content per recipient, sends 500 messages on it, and closes it once

#### Scenario: Per-message delay is applied between sends
- **WHEN** a job with 10 recipients is processed with `PerMessageDelay=500ms`
- **THEN** the total fan-out wall-clock time is at least `9 x 500ms` (no delay before the first or after the last send)

#### Scenario: Delay is configurable
- **WHEN** an operator sets `BulkEmailOptions.PerMessageDelay` to `0ms` for a load test
- **THEN** the worker sends recipients back-to-back with no inserted delay

#### Scenario: Cancellation during delay wakes the worker immediately
- **WHEN** a cancel request arrives while the worker is sleeping in the inter-message delay
- **THEN** the wait completes early, no further send is attempted, and the job finalises to `Cancelled`

#### Scenario: Per-recipient status persists through restart
- **WHEN** the worker crashes after sending 200 of 500 recipients
- **THEN** on the next pickup the snapshot shows 200 entries with per-recipient `Status=Sent` and 300 still `Pending`, and only the 300 are re-attempted

#### Scenario: Duplicate fan-out does not double-write the log
- **WHEN** a bug causes the same recipient to be processed twice in one job
- **THEN** the second `email_log` insert hits the unique index and is treated as already-sent; `SentCount` is not double-incremented

#### Scenario: Per-recipient failure marks PartiallyFailed not Failed
- **WHEN** a 100-recipient job sends 99 successfully and 1 hits a terminal SMTP error
- **THEN** the job ends in `PartiallyFailed` with `SentCount=99`, `FailedCount=1`, and `LastError` describing the last failure

#### Scenario: Transient recipient failure retries inline
- **WHEN** a recipient send fails with a transient SMTP transport error and retry attempts remain
- **THEN** the worker waits the configured inline retry delay and retries the same recipient before recording a terminal recipient failure

#### Scenario: All recipients failing marks Failed
- **WHEN** every recipient hits a terminal failure
- **THEN** the job ends in `Failed` with `SentCount=0` and `FailedCount=RecipientCount`

#### Scenario: Empty recipient set completes immediately
- **WHEN** resolution returns zero recipients
- **THEN** the job transitions directly from `Resolving` to `Completed` with all counters at zero

### Requirement: Ad-hoc subject and body on the job override the resolved template
Custom bulk email jobs SHALL use job-owned content directly. A `bulk-custom` job SHALL carry non-null `Subject`, `TextBody`, and `HtmlBody`; the email composer SHALL render those values for every recipient. The worker SHALL NOT resolve an `EmailTemplate`, SHALL NOT apply template fallback, and SHALL NOT support partial custom content for `bulk-custom` jobs.

#### Scenario: Custom bulk content is complete and job-owned
- **WHEN** a job carries `EmailType="bulk-custom"`, `Subject="Schedule update"`, `TextBody="..."`, and `HtmlBody="..."`
- **THEN** every email sent for the job uses those values and no stored template is consulted

#### Scenario: Partial custom content is rejected
- **WHEN** an organizer creates a `bulk-custom` job without `HtmlBody`
- **THEN** the request is rejected by validation and no job is persisted

#### Scenario: Stored templates are not used
- **WHEN** a `bulk-custom` job is processed
- **THEN** the worker does not load an `EmailTemplate` or `CustomBulkTemplate`

### Requirement: Bulk-email admin endpoints follow the slice-per-feature layout
The Email module SHALL expose admin HTTP endpoints under `/admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`:

- `POST /preview` - synchronously resolve a recipient source against live data and return `{count, sample[]}` (sample capped, default 100). Does NOT create a job.
- `POST /` - create a `BulkEmailJob` from a request DTO (`emailType`, required `subject`/`textBody`/`htmlBody` for `bulk-custom`, `source` - exactly one of `attendee`/`externalList`). Returns `Created` with the new job id.
- `GET /` - list jobs for the event, newest first, with status and totals.
- `GET /{id}` - fetch one job's audit detail.
- `POST /{id}/cancel` - cooperatively cancel a job that has not yet reached a terminal state (`Pending`, `Resolving`, or `Sending`). Sets `CancellationRequestedAt` and returns immediately; the worker observes the flag and finalises the job to `Cancelled` between recipients.

All endpoints SHALL require team-membership authorisation on the team owning the event.

#### Scenario: Preview returns count and sample without persisting
- **WHEN** an organizer previews an `AttendeeSource(ticketTypeSlugs=["workshop-a"])` source on event "DevConf"
- **THEN** the response contains the matched count and a sample of up to 100 recipient emails, and no `BulkEmailJob` row is created

#### Scenario: Create returns 201 with the job id
- **WHEN** an organizer posts a valid custom bulk-send request with subject, text body, HTML body, and one source
- **THEN** the response is `201 Created` and includes the new `BulkEmailJob` id; the job is persisted in `Pending`

#### Scenario: Create rejects missing body content
- **WHEN** an organizer posts a `bulk-custom` request missing `TextBody` or `HtmlBody`
- **THEN** the request is rejected with a validation error

#### Scenario: Create rejects a request carrying both source shapes
- **WHEN** the request body somehow contains both `attendee` and `externalList` source fields
- **THEN** the request is rejected with a validation error

#### Scenario: Cancel during Sending is accepted and finalises cooperatively
- **WHEN** a cancel request is issued against a job in `Sending`
- **THEN** the response is `202 Accepted`, `CancellationRequestedAt` is persisted, and the worker stops sending between recipients (so the job's final status is `Cancelled` with whatever `SentCount` was reached)

#### Scenario: Cancel against a terminal job is rejected
- **WHEN** a cancel request is issued against a job in `Completed`, `PartiallyFailed`, `Failed`, or `Cancelled`
- **THEN** the response is a domain validation error and the job's status is unchanged

#### Scenario: Non-team-member denied
- **WHEN** a user who is not a member of the owning team calls any bulk-emails endpoint
- **THEN** the response is `403 Forbidden`
