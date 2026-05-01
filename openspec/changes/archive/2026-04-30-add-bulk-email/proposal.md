## Why

Organizers need to send the same message to many recipients at once — both to attendees already in the system (e.g. "all workshop A attendees", "everyone who registered before May 1st", "all registered for DevConf") and to external lists (e.g. a marketing invitation list pasted into the UI). The system also needs to drive the *reconfirm* flow: now that the `TicketedEvent` aggregate owns a `TicketedEventReconfirmPolicy`, something has to actually fan out reconfirmation emails on cadence inside the policy window. Today, the new Email module can only send single transactional emails (one per `AttendeeRegisteredIntegrationEvent`); there is no batch-send concept, no recipient-selection model, and no scheduled trigger for reconfirms.

## What Changes

- Introduce a new **`bulk-email`** capability owned by the Email module:
  - A `BulkEmailJob` aggregate (in the `email` schema) tracking lifecycle (`Pending`, `Resolving`, `Sending`, `Completed`, `PartiallyFailed`, `Failed`, `Cancelled`) plus per-job totals (`RecipientCount`, `SentCount`, `FailedCount`, `CancelledCount`), `EmailType`, optional ad-hoc subject/body, who triggered it, and the resolved-recipient snapshot for audit.
  - A worker-side processor that resolves recipients, then **streams the entire batch over a single SMTP connection per worker pickup** with a small configurable inter-message delay (`BulkEmailOptions.PerMessageDelay`, default `500ms`) for SMTP-relay friendliness and responsive cooperative cancellation. Does NOT publish per-recipient `SendEmailCommand`s; the single-send pipeline is bypassed for bulk to avoid one-TCP-handshake-per-recipient.
  - Per-recipient idempotency via the existing `EmailLog` unique index, key derived from the job: `bulk:{bulkJobId}:{recipientEmail}`. The bulk worker writes `email_log` rows directly; resume-after-crash is driven by per-recipient `Status` on the snapshot.
- Introduce a **recipient-resolution model** that supports two mutually-exclusive sources per job:
  1. **Attendee source**: filters resolved against the Registrations module (e.g. ticket-type slugs, registration status, has-reconfirmed, registered-before/after, additional-detail equality). Resolution happens at job start, against live Registrations data; the resolved snapshot is then frozen for the lifetime of that job.
  2. **External-list source**: a one-off list of `(email, displayName?)` pairs supplied at request time and stored only in the job's recipient snapshot (no separate "saved recipient list" entity). Replaces the legacy `EmailRecipientList` aggregate.
  3. A single job carries exactly one source. Mixed audiences are expressed as two separate jobs (each with its own audit record).
- Introduce a new **`reconfirm-sending`** capability (also owned by Email):
  - A scheduled (Quartz) job, gated on `HostCapability.Jobs`, ticks during each event's reconfirm window and creates a `BulkEmailJob` with an attendee source filtered to `Status=Registered AND HasReconfirmed=false`. Once an attendee reconfirms they fall out of the candidate set entirely.
  - Cadence is encoded entirely by the cron schedule of the per-event trigger. Eligibility on each tick is `Status=Registered AND HasReconfirmed=false`, queried live from Registrations — no additional `email_log` filtering is performed. Attendees who registered between ticks are picked up; attendees who reconfirmed between ticks fall out automatically.
  - The reconfirm job is (re)scheduled in response to the existing `TicketedEventCreated` integration event and to a new `TicketedEventReconfirmPolicyChanged` integration event from Registrations.
- Extend the **`Registration` aggregate** (Registrations module) with the attendee-identity and lifecycle fields the new bulk-send/reconfirm queries need and that are already implied by the existing UI (which currently fakes them):
  - Required `FirstName` and `LastName` value objects (replacing the placeholder "derive a display name from the email local-part" behaviour the registrations list currently uses).
  - A `Status` enum (`Registered`, `Cancelled`) with a `Cancel(reason)` domain method (registrations created today are always `Registered`; cancellation as a real lifecycle is introduced here so attendee-source filters and the read model can be honest).
  - A `HasReconfirmed` flag plus `ReconfirmedAt?` timestamp and a `Reconfirm()` domain method, raised by the reconfirm flow added in this change.
  - Public self-registration, admin-add registration, and the corresponding HTTP/CLI request DTOs SHALL require `firstName` + `lastName`. EF migration adds the columns; existing rows are backfilled from the email local-part as a one-shot transformation so the new columns can be `NOT NULL`.
- Extend the **Registrations facade** (cross-module read, per arc42 §8.4) so the Email module can resolve recipients without owning Registrations data:
  - `IRegistrationsFacade.QueryRegistrationsAsync(eventId, query)` (intentionally generic — reusable beyond bulk email) returns `(email, firstName, lastName, registrationId, ticketTypeSlugs[], additionalDetails, status, hasReconfirmed)` projections matching the filters.
  - The existing `GetTicketedEventEmailContextAsync` is unchanged; the per-recipient template parameters needed for reconfirm emails are carried in the snapshot's `ParametersJson` populated at resolve time.
- Add admin HTTP endpoints (under `/admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails`):
  - `POST /preview` — resolve the source synchronously and return count + sample (no job created).
  - `POST` — start a bulk send (custom or based on a saved template).
  - `GET` — list bulk jobs for an event with status and totals.
  - `GET /{id}` — fetch one job with the resolved recipient snapshot (per-recipient status), status, totals, and last error.
  - `POST /{id}/cancel` — cooperative cancel of a job in `Pending`, `Resolving`, or `Sending` (the worker observes the request between recipients and finalises to `Cancelled`).
- Add CLI parity commands under `admitto event bulk-email`.
- Extend `TicketedEvent` (Registrations) with a required `TimeZone` (IANA zone id). The reconfirm cron evaluates in this zone so cadences like "daily" fire at the same local hour year-round, including across DST. Adds a new `TicketedEventTimeZoneChanged` integration event consumed by the reconfirm scheduler.
- **Note**: legacy `Admitto.Application` code (orphaned, not in the active solution) is left untouched — no removal as part of this change.

## Capabilities

### New Capabilities

- `bulk-email`: starting, tracking, and fanning out a single bulk send. Owns the `BulkEmailJob` aggregate, the two-shape recipient-source model (attendee | external), the single-SMTP-connection fan-out semantics, and the audit snapshot.
- `reconfirm-sending`: scheduled trigger that turns a `TicketedEventReconfirmPolicy` into recurring `BulkEmailJob`s of `email_type='reconfirm'` during the event's reconfirm window, honouring cadence per recipient.

### Modified Capabilities

- `email-templates`: add `reconfirm` and `bulk-custom` to the canonical template-type set so they participate in the same scope-precedence resolution; ad-hoc per-send subject/body overrides the resolved template when supplied on the job.
- `email-log`: extend the row to optionally carry `BulkEmailJobId` so per-job send history can be answered with a single index.
- `cli-admin-parity`: add CLI commands for the new bulk-email admin endpoints.
- `event-management`: `TicketedEvent` gains a required `TimeZone` (IANA zone id), validated synchronously on create/update; new `TicketedEventTimeZoneChanged` integration event published on update.
- `attendee-registration`: `Registration` gains required `FirstName`/`LastName`, a `Status` (`Registered`/`Cancelled`) with a `Cancel` domain method, and a `HasReconfirmed`/`ReconfirmedAt?` pair with a `Reconfirm` domain method. Self-service and admin-add registration request DTOs require `firstName` + `lastName`.
- `registration-listing`: list projection now exposes `firstName`, `lastName`, `status`, `hasReconfirmed` per row.
- `admin-ui-event-management`: Create / General-tab forms gain a TZ selector; all event-scoped pickers and read-only datetime displays use the event's zone with a visible zone caption.
- `admin-ui-event-policies`: registration / cancellation / reconfirm policy date-time pickers honour the event's zone (entry and display).
- `admin-ui-registrations`: add-registration form requires first/last name; registrations list shows real first/last names instead of an email-derived display name, real `Status` (Registered/Cancelled) instead of hard-coded "Confirmed", and real `Reconfirm` (yes/no with timestamp) instead of "—".

## Impact

- **New code**: `BulkEmailJob` aggregate + persistence in the `email` schema; recipient-source value objects; resolver service; worker processor (Quartz job that also drives the in-flight fan-out for an active job); reconfirm scheduler job; admin endpoints + CLI commands. **No new Admin UI for bulk email or reconfirm sending in this change** — backend + CLI only; UI follows in a later change. UI changes here are limited to adding the per-event TZ selector and making event-scoped date-time pickers TZ-aware.
- **Cross-module surface**: extends `IRegistrationsFacade` (in `Admitto.Module.Registrations.Contracts`) with one query method; adds a new module event `TicketedEventReconfirmPolicyChanged` published by Registrations when the policy is set/updated/cleared.
- **Database**: one new table (`email.bulk_email_jobs`) plus one nullable column on `email.email_log` (`bulk_email_job_id`); plus four new columns on `registrations.registrations` (`first_name`, `last_name`, `status`, `has_reconfirmed`, `reconfirmed_at`) with a one-shot backfill for existing rows; EF migrations in `Admitto.Migrations`.
- **Hosts**: bulk fan-out and reconfirm scheduling run only on hosts with `HostCapability.Jobs | HostCapability.Email` (the Worker host today). Admin endpoints live on the API host as usual.
- **Removed**: legacy `EmailRecipientList` aggregate and its endpoints/CLI commands are not carried forward into the new module structure.
- **Docs**: update `docs/arc42/05-building-block-view.md` to mention the bulk-email capability and `docs/arc42/06-runtime-view.md` to add the bulk-send and reconfirm-tick sequence diagrams. New ADR for the recipient-source design choice (criteria-vs-IDs-vs-external).
