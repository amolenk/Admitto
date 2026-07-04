## ADDED Requirements

### Requirement: Bulk-email jobs are first-class aggregates with a tracked lifecycle

The Email module SHALL persist a `BulkEmailJob` aggregate (in the `email` schema) for every bulk send. Each job SHALL carry a unique `Id`, the owning `TeamId` and `TicketedEventId`, the `EmailType` (one of the canonical set including `reconfirm` and `bulk-custom`), an optional ad-hoc `Subject`/`TextBody`/`HtmlBody`, the resolved `Source` descriptor, the user identity that triggered it (or a system-user marker for scheduled reconfirm jobs), the resolved `Recipients` snapshot, the running totals `RecipientCount`/`SentCount`/`FailedCount`/`CancelledCount`, the `Status`, an optional `LastError`, timestamps `CreatedAt`/`StartedAt`/`CompletedAt`/`CancellationRequestedAt?`/`CancelledAt?`, and a `Version` token for optimistic concurrency.

Status SHALL be one of: `Pending` (created, not yet picked up), `Resolving` (recipient resolution in progress), `Sending` (recipients resolved, fan-out in progress), `Completed` (every recipient produced a sent or failed terminal state), `PartiallyFailed` (one or more recipients failed terminally but at least one succeeded), `Failed` (resolution failed or all recipients failed terminally), `Cancelled` (cancelled cooperatively at any point before reaching another terminal state).

#### Scenario: Job created in Pending state
- **WHEN** an organizer triggers a bulk send for event "DevConf" with an attendee source
- **THEN** a new `BulkEmailJob` row is persisted with `Status=Pending`, `RecipientCount=0`, the trigger user id, and the source descriptor

#### Scenario: Lifecycle transitions are linear
- **WHEN** a job moves through resolution and fan-out
- **THEN** observed transitions follow `Pending → Resolving → Sending → (Completed | PartiallyFailed | Failed | Cancelled)` and never go backwards

#### Scenario: Cancellation requested while Pending or Resolving stops the job before any send
- **WHEN** a cancel request is issued against a job in `Pending` or `Resolving`
- **THEN** the aggregate sets `CancellationRequestedAt`, transitions to `Cancelled` once the worker observes the request (or immediately if not yet picked up), no fan-out occurs, and `SentCount=0`

#### Scenario: Cancellation requested while Sending stops the job between recipients
- **WHEN** a cancel request is issued against a job in `Sending` (e.g. with 312 of 5000 recipients already sent)
- **THEN** the aggregate sets `CancellationRequestedAt`, the worker observes the flag before the next recipient (within at most one per-message delay cycle), transitions remaining `Pending` recipient rows to `Cancelled`, and finalises the job to `Status=Cancelled` with `SentCount=312`, `CancelledCount=4688`

#### Scenario: Cancel against a terminal job is rejected
- **WHEN** a cancel request is issued against a job in `Completed`, `PartiallyFailed`, `Failed`, or `Cancelled`
- **THEN** the request is rejected with a domain error and the status is unchanged

---

### Requirement: A bulk-email job has exactly one recipient source — attendee or external list

A `BulkEmailJob.Source` SHALL be exactly one of two discriminated value types: `AttendeeSource` or `ExternalListSource`. There SHALL NOT be a combined / multi-source shape; an organizer who needs to email both registered attendees and an external list SHALL create two separate jobs.

`AttendeeSource` SHALL carry filters consumable by `IRegistrationsFacade.QueryRegistrationsAsync`, including at minimum: `TicketTypeSlugs?` (any-of match), `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`/`RegisteredBefore?`, and `AdditionalDetailEquals?` (key/value pairs).

`ExternalListSource` SHALL carry an array of `(Email, DisplayName?)` items supplied at request time. There SHALL NOT be a separate persisted "saved recipient list" entity.

#### Scenario: Attendee source resolves against live Registrations data at job start
- **WHEN** a job with `AttendeeSource(ticketTypeSlugs=["workshop-a"])` enters `Resolving`
- **THEN** the resolver calls `IRegistrationsFacade.QueryRegistrationsAsync` with the filters and receives one row per matching registration

#### Scenario: External list source needs no facade call
- **WHEN** a job with `ExternalListSource([("alice@x.org","Alice"),("bob@x.org",null)])` enters `Resolving`
- **THEN** the resolver materialises exactly those two recipients without calling the Registrations facade

#### Scenario: Two-job pattern for mixed audiences
- **WHEN** an organizer needs to email both all "workshop-a" attendees and an external invite list
- **THEN** they submit two separate bulk jobs and each carries its own audit record

---

### Requirement: Recipient resolution snapshots once and freezes

When a job transitions from `Pending` to `Resolving`, the resolver SHALL persist the resolved recipient set as `BulkEmailRecipient` value objects on the job (each with `Email`, `DisplayName?`, `RegistrationId?`, `ParametersJson` for any per-recipient template parameters, and a per-recipient `Status` field with values `Pending`/`Sent`/`Failed`/`Cancelled` plus optional `LastError`). Subsequent re-runs of the fan-out SHALL re-read from this snapshot and SHALL NOT re-query the Registrations facade for attendee sources.

#### Scenario: Snapshot persisted before fan-out begins
- **WHEN** the resolver finishes
- **THEN** the job carries a complete `Recipients` collection with every entry in per-recipient `Status=Pending` and the job transitions to `Sending`, all in the same database transaction

#### Scenario: Worker restart resumes from snapshot
- **WHEN** the worker process restarts mid-`Sending` and the job is rescheduled
- **THEN** the resumed fan-out reads the existing `Recipients` snapshot and processes only entries still in per-recipient `Status=Pending`

#### Scenario: Source registrations cancelled mid-send still appear
- **WHEN** a registration matching the criteria is cancelled in Registrations after the snapshot but before its email is sent
- **THEN** the bulk send still attempts to email that recipient because the snapshot is authoritative

---

### Requirement: Fan-out streams over a single SMTP connection per worker pickup

The bulk fan-out worker, upon picking up a `BulkEmailJob` with status `Sending`, SHALL open exactly one SMTP connection (per pickup) and stream every still-`Pending` recipient through it. The worker SHALL NOT publish per-recipient `SendEmailCommand` messages and SHALL NOT use the single-send command pipeline for bulk fan-out.

For each recipient the worker SHALL, in order: (1) render the message (template + ad-hoc overrides), (2) send it on the open SMTP connection, (3) write a single `email_log` row with `bulk_email_job_id` set and `IdempotencyKey = "bulk:{bulkJobId}:{normalisedRecipientEmail}"`, (4) update the per-recipient `Status` on the snapshot to `Sent` or `Failed`, (5) update the parent job's running `SentCount` / `FailedCount`. The existing unique index on `email_log` `(ticketed_event_id, recipient, idempotency_key)` SHALL ensure that a re-run of the fan-out for the same recipient is a no-op at the log level even if the per-recipient status update was lost.

The worker SHALL close the SMTP connection cleanly when the snapshot is exhausted, on cancellation, or on a connection-level failure.

Between consecutive recipient sends, the worker SHALL wait `BulkEmailOptions.PerMessageDelay` (configurable; default `500ms`). The wait SHALL be cancellable so that a cancellation request observed during the wait causes the worker to stop sending immediately without consuming the full delay.

#### Scenario: Per-message delay is applied between sends
- **WHEN** a job with 10 recipients is processed with `PerMessageDelay=500ms`
- **THEN** the total fan-out wall-clock time is at least `9 × 500ms` (no delay before the first or after the last send)

#### Scenario: Delay is configurable
- **WHEN** an operator sets `BulkEmailOptions.PerMessageDelay` to `0ms` for a load test
- **THEN** the worker sends recipients back-to-back with no inserted delay

#### Scenario: Cancellation during delay wakes the worker immediately
- **WHEN** a cancel request arrives while the worker is sleeping in the inter-message delay
- **THEN** the wait completes early, no further send is attempted, and the job finalises to `Cancelled`

#### Scenario: Single connection serves many recipients
- **WHEN** a 500-recipient job is picked up by a single worker
- **THEN** the SMTP sender opens exactly one connection, sends 500 messages on it, and closes it once

#### Scenario: Per-recipient status persists through restart
- **WHEN** the worker crashes after sending 200 of 500 recipients
- **THEN** on the next pickup the snapshot shows 200 entries with per-recipient `Status=Sent` and 300 still `Pending`, and only the 300 are re-attempted

#### Scenario: Duplicate fan-out does not double-write the log
- **WHEN** a bug causes the same recipient to be processed twice in one job
- **THEN** the second `email_log` insert hits the unique index and is treated as already-sent; `SentCount` is not double-incremented

#### Scenario: Per-recipient failure marks PartiallyFailed not Failed
- **WHEN** a 100-recipient job sends 99 successfully and 1 hits a terminal SMTP error
- **THEN** the job ends in `PartiallyFailed` with `SentCount=99`, `FailedCount=1`, and `LastError` describing the last failure

#### Scenario: All recipients failing marks Failed
- **WHEN** every recipient hits a terminal failure
- **THEN** the job ends in `Failed` with `SentCount=0` and `FailedCount=RecipientCount`

#### Scenario: Empty recipient set completes immediately
- **WHEN** resolution returns zero recipients
- **THEN** the job transitions directly from `Resolving` to `Completed` with all counters at zero

---

### Requirement: Ad-hoc subject and body on the job override the resolved template

When a `BulkEmailJob` carries a non-null `Subject`, `TextBody`, or `HtmlBody`, the email composer SHALL use those values instead of the corresponding fields from the resolved `EmailTemplate`. Fields that are null SHALL fall back to the resolved template. Templates SHALL still resolve via the standard `email-templates` precedence (event > team > built-in default).

#### Scenario: Full ad-hoc override
- **WHEN** a job carries a `Subject="Schedule update"`, `TextBody="..."`, and `HtmlBody="..."`
- **THEN** every email sent for the job uses those values, and the resolved template is consulted only for parameter availability

#### Scenario: Partial ad-hoc override
- **WHEN** a job carries only `Subject="Schedule update"` and the resolved template provides text+html bodies
- **THEN** the rendered email uses the ad-hoc subject and the template's text+html bodies

#### Scenario: No ad-hoc override
- **WHEN** a job carries no ad-hoc fields
- **THEN** the email uses the resolved template's subject, text body, and html body unchanged

---

### Requirement: Bulk fan-out runs only on hosts with Email and Jobs capabilities

The bulk fan-out worker (Quartz job) and the recipient resolver service SHALL be registered only in hosts whose declared `HostCapability` includes both `Jobs` and `Email`. The `BulkEmailJob` aggregate, its EF mapping, and the read-side queries SHALL be available in any host that registers the Email module.

#### Scenario: Worker host registers fan-out
- **WHEN** the Worker host (declares `HostCapability.Jobs | HostCapability.Email`) starts up
- **THEN** the `BulkEmailFanOutJob` Quartz job and the resolver service are present in DI

#### Scenario: API host does not register fan-out
- **WHEN** the API host (does not declare `HostCapability.Jobs`) starts up
- **THEN** the `BulkEmailFanOutJob` and resolver are absent from DI; the `BulkEmailJob` write store is still available so the API can create a job in `Pending`

---

### Requirement: Two jobs run concurrently but never the same job twice

The fan-out worker SHALL be configured so that multiple `BulkEmailJob`s can be in `Sending` concurrently across worker instances, while the same `BulkEmailJob` SHALL never have two fan-outs in flight at once. This SHALL be enforced through Quartz `[DisallowConcurrentExecution]` keyed per `BulkEmailJobId` (per-job trigger group). Combined with the single-SMTP-connection-per-pickup rule, this means at most one SMTP connection per active bulk job per worker process.

#### Scenario: Two events sending in parallel
- **WHEN** organizers start bulk sends for two different events at the same time
- **THEN** both jobs progress concurrently, each over its own SMTP connection

#### Scenario: Same job picked up twice
- **WHEN** a worker crash leaves a job in `Sending` and a second worker also tries to pick it up
- **THEN** Quartz prevents concurrent execution for that specific `BulkEmailJobId` and the second worker waits or skips

---

### Requirement: Per-job audit data is queryable

The Email module SHALL expose a query that returns, for any `BulkEmailJob`, the full snapshot (recipients with per-recipient status, source, ad-hoc content if any, trigger user, timestamps, status, totals, last error). This is the data backing both the admin endpoint and the Admin UI detail page.

#### Scenario: Fetch a completed job's audit
- **WHEN** the audit query is run for a completed job
- **THEN** the response contains exactly one row per resolved recipient with its per-recipient send status, the totals, and the trigger user identity

---

### Requirement: Bulk-email admin endpoints follow the slice-per-feature layout

The Email module SHALL expose admin HTTP endpoints under `/admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`:

- `POST /preview` — synchronously resolve a recipient source against live data and return `{count, sample[]}` (sample capped, default 100). Does NOT create a job.
- `POST /` — create a `BulkEmailJob` from a request DTO (`emailType`, optional ad-hoc `subject`/`textBody`/`htmlBody`, `source` — exactly one of `attendee`/`externalList`). Returns `Created` with the new job id.
- `GET /` — list jobs for the event, newest first, with status and totals.
- `GET /{id}` — fetch one job's audit detail.
- `POST /{id}/cancel` — cooperatively cancel a job that has not yet reached a terminal state (`Pending`, `Resolving`, or `Sending`). Sets `CancellationRequestedAt` and returns immediately; the worker observes the flag and finalises the job to `Cancelled` between recipients.

All endpoints SHALL require team-membership authorisation on the team owning the event.

#### Scenario: Preview returns count and sample without persisting
- **WHEN** an organizer previews an `AttendeeSource(ticketTypeSlugs=["workshop-a"])` source on event "DevConf"
- **THEN** the response contains the matched count and a sample of up to 100 recipient emails, and no `BulkEmailJob` row is created

#### Scenario: Create returns 201 with the job id
- **WHEN** an organizer posts a valid bulk-send request
- **THEN** the response is `201 Created` and includes the new `BulkEmailJob` id; the job is persisted in `Pending`

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
