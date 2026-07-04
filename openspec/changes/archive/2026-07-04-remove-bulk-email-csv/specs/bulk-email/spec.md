## MODIFIED Requirements

### Requirement: Bulk-email jobs are first-class aggregates with a tracked lifecycle

The Email module SHALL persist a `BulkEmailJob` aggregate (in the `email` schema) for every bulk send. Each job SHALL carry a unique `Id`, the owning `TeamId` and `TicketedEventId`, the `EmailType` (one of the canonical set including `reconfirm` and `bulk-custom`), the content fields required for its type, the resolved attendee recipient filter (an Email-owned value persisted independently of the Registrations query contract), the user identity that triggered it (or a system-user marker for scheduled reconfirm jobs), the resolved `Recipients` snapshot, the running totals `RecipientCount`/`SentCount`/`FailedCount`/`CancelledCount`, the `Status`, an optional `LastError`, timestamps `CreatedAt`/`StartedAt`/`CompletedAt`/`CancellationRequestedAt?`/`CancelledAt?`, and a `Version` token for optimistic concurrency.

For `bulk-custom`, `Subject`, `TextBody`, and `HtmlBody` SHALL all be required and persisted on the job as the immutable content snapshot. System-triggered built-in email types such as `reconfirm` MAY use code-owned built-in content instead of job-owned content.

Status SHALL be one of: `Pending` (created, not yet picked up), `Resolving` (recipient resolution in progress), `Sending` (recipients resolved, fan-out in progress), `Completed` (every recipient produced a sent or failed terminal state), `PartiallyFailed` (one or more recipients failed terminally but at least one succeeded), `Failed` (resolution failed or all recipients failed terminally), `Cancelled` (cancelled cooperatively at any point before reaching another terminal state).

#### Scenario: Custom job created in Pending state with complete content

- **WHEN** an organizer triggers a custom bulk send for event "DevConf" with `Subject`, `TextBody`, `HtmlBody`, and an attendee filter
- **THEN** a new `BulkEmailJob` row is persisted with `Status=Pending`, `RecipientCount=0`, the trigger user id, the content fields, and the attendee filter

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

### Requirement: Recipient resolution snapshots once and freezes

When a job transitions from `Pending` to `Resolving`, the resolver SHALL persist the resolved recipient set as `BulkEmailRecipient` value objects on the job (each with `Email`, `DisplayName`, `RegistrationId`, `ParametersJson` for any per-recipient template parameters, and a per-recipient `Status` field with values `Pending`/`Sent`/`Failed`/`Cancelled` plus optional `LastError`). Because every recipient originates from a registered attendee, `DisplayName` and `RegistrationId` SHALL always be populated. Subsequent re-runs of the fan-out SHALL re-read from this snapshot and SHALL NOT re-query the Registrations facade.

#### Scenario: Snapshot persisted before fan-out begins

- **WHEN** the resolver finishes
- **THEN** the job carries a complete `Recipients` collection with every entry in per-recipient `Status=Pending` and the job transitions to `Sending`, all in the same database transaction

#### Scenario: Every recipient carries a registration identity

- **WHEN** the resolver materialises the attendee snapshot
- **THEN** every `BulkEmailRecipient` has a non-null `RegistrationId` and a non-null `DisplayName` derived from the attendee's first and last name

#### Scenario: Worker restart resumes from snapshot

- **WHEN** the worker process restarts mid-`Sending` and the job is rescheduled
- **THEN** the resumed fan-out reads the existing `Recipients` snapshot and processes only entries still in per-recipient `Status=Pending`

#### Scenario: Source registrations cancelled mid-send still appear

- **WHEN** a registration matching the criteria is cancelled in Registrations after the snapshot but before its email is sent
- **THEN** the bulk send still attempts to email that recipient because the snapshot is authoritative

### Requirement: Bulk-email admin endpoints follow the slice-per-feature layout

The Email module SHALL expose admin HTTP endpoints under `/admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`:

- `POST /preview` - synchronously resolve an attendee recipient filter against live data and return `{count, sample[]}` (sample capped, default 100). Does NOT create a job.
- `POST /` - create a `BulkEmailJob` from a request DTO (`emailType`, required `subject`/`textBody`/`htmlBody` for `bulk-custom`, and an attendee recipient filter). Returns `Created` with the new job id.
- `GET /` - list jobs for the event, newest first, with status and totals.
- `GET /{id}` - fetch one job's audit detail.
- `POST /{id}/cancel` - cooperatively cancel a job that has not yet reached a terminal state (`Pending`, `Resolving`, or `Sending`). Sets `CancellationRequestedAt` and returns immediately; the worker observes the flag and finalises the job to `Cancelled` between recipients.

All endpoints SHALL require team-membership authorisation on the team owning the event.

#### Scenario: Preview returns count and sample without persisting

- **WHEN** an organizer previews an attendee filter (`ticketTypeIds=["workshop-a"]`) on event "DevConf"
- **THEN** the response contains the matched count and a sample of up to 100 recipient emails, and no `BulkEmailJob` row is created

#### Scenario: Create returns 201 with the job id

- **WHEN** an organizer posts a valid custom bulk-send request with subject, text body, HTML body, and an attendee filter
- **THEN** the response is `201 Created` and includes the new `BulkEmailJob` id; the job is persisted in `Pending`

#### Scenario: Create rejects missing body content

- **WHEN** an organizer posts a `bulk-custom` request missing `TextBody` or `HtmlBody`
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

## ADDED Requirements

### Requirement: A bulk-email job targets registered attendees via an Email-owned filter

A `BulkEmailJob` SHALL resolve its recipients from registered attendees only. The job SHALL persist a single attendee recipient filter that is consumable by `IRegistrationsFacade.GetRegistrationsAsync`, including at minimum: `TicketTypeIds?` (any-of match), `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`/`RegisteredBefore?`, `AdditionalDetailEquals?` (key/value pairs), and `RegistrationIds?` (allowlist for system-triggered sends).

The persisted filter SHALL be an Email-module-owned value object, NOT the Registrations query contract type. The Email module SHALL translate its owned filter into the Registrations query contract only transiently at the facade call boundary, so the Registrations query DTO SHALL NOT be part of the Email module's durable state.

There SHALL NOT be any recipient source other than registered attendees; arbitrary/external recipient lists SHALL NOT be supported.

#### Scenario: Attendee filter resolves against live Registrations data at job start

- **WHEN** a job with an attendee filter (`ticketTypeIds=["workshop-a"]`) enters `Resolving`
- **THEN** the resolver maps the Email-owned filter to the Registrations query contract, calls `IRegistrationsFacade.GetRegistrationsAsync`, and receives one row per matching registration

#### Scenario: Registrations query contract is not persisted

- **WHEN** a bulk-email job is persisted
- **THEN** the stored filter is the Email-owned value object and no `Registrations.Contracts` query DTO is written to the Email schema

## REMOVED Requirements

### Requirement: A bulk-email job has exactly one recipient source - attendee or external list

**Reason**: The external-list (CSV / arbitrary recipient) source is being removed to protect sending-domain reputation; bulk email now targets registered attendees only, so the two-source discriminated model no longer exists.

**Migration**: No data migration required — the `bulk_email_jobs` table is empty. The single remaining recipient shape is defined by the new requirement "A bulk-email job targets registered attendees via an Email-owned filter". Organizers who previously emailed external lists must use a dedicated marketing tool outside Admitto.
