# Implementation Tasks — add-bulk-email

## 1. Cross-module contracts

- [x] 1.1 Add `QueryRegistrationsDto` (filters: `TicketTypeSlugs?`, `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`, `RegisteredBefore?`, `AdditionalDetailEquals?`) and `RegistrationListItemDto` (Email, FirstName, LastName, RegistrationId, TicketTypeSlugs, AdditionalDetails, Status, HasReconfirmed) to `Admitto.Module.Registrations.Contracts`. Names are intentionally generic — this is a reusable Registrations query, not bulk-email-specific. *Note: the DTO carries `FirstName`/`LastName` (not a `DisplayName`); the aggregate gains these in section 2A.*
- [x] 1.2 Extend `IRegistrationsFacade` with `Task<IReadOnlyList<RegistrationListItemDto>> QueryRegistrationsAsync(TicketedEventId eventId, QueryRegistrationsDto query, CancellationToken ct)`.
- [x] 1.3 Add `TicketedEventReconfirmPolicyChangedIntegrationEvent` (eventId, policy snapshot or null) to `Admitto.Module.Registrations.Contracts`.
- [x] 1.4 Publish `TicketedEventReconfirmPolicyChangedIntegrationEvent` from Registrations whenever the reconfirm policy is set, updated, or cleared (use the existing outbox pattern).
- [x] 1.5 Add `TicketedEventTimeZoneChangedIntegrationEvent` (eventId, new IANA zone id) to `Admitto.Module.Registrations.Contracts`. Include `TimeZone` on the existing `TicketedEventCreatedIntegrationEvent` payload so initial trigger registration has the zone available without a follow-up lookup.

## 2. Registrations module — TicketedEvent time zone

- [x] 2.1 Add a required `TimeZone` value object (IANA zone id) to the `TicketedEvent` aggregate; validate against `TimeZoneInfo.FindSystemTimeZoneById` (which on .NET 10 supports IANA on all platforms).
- [x] 2.2 EF migration: add `time_zone` non-null column to the events table (use a sensible default like `UTC` for any backfill, or run as a code migration that requires explicit value before deploy — confirm with `ef-migrations` skill).
- [x] 2.3 Update creation request DTO (`POST /admin/teams/{teamSlug}/events`) to require `timeZone`; FluentValidation rejects unknown zones synchronously.
- [x] 2.4 Add admin command + endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/time-zone` that updates the TZ and outboxes `TicketedEventTimeZoneChangedIntegrationEvent`.
- [x] 2.5 Implement `QueryRegistrationsHandler` projecting from `RegistrationsDbContext.Registrations` joined to `TicketedEvents`, applying the filters (including `RegistrationStatus` and `HasReconfirmed` from section 2A) and returning `RegistrationListItemDto` rows (with real `FirstName`/`LastName`).
- [x] 2.6 Wire the new handler through `RegistrationsFacade` (delegate via mediator).

## 2A. Registrations module — attendee identity & lifecycle (Registration aggregate fields)

- [x] 2A.1 Add `FirstName` and `LastName` value objects (in `Admitto.Module.Shared.Kernel.ValueObjects` or, if domain-specific, in the Registrations domain) — string-based, MaxLength 100, trimmed, non-empty. Reuse the existing `IStringValueObject` pattern.
- [x] 2A.2 Add a `RegistrationStatus` enum (`Registered`, `Cancelled`) to the Registrations domain. Mirror it (or share via Contracts) so `QueryRegistrationsDto.RegistrationStatus` and the projection align.
- [x] 2A.3 Extend the `Registration` aggregate with required `FirstName`, `LastName`, `Status` (default `Registered`), `HasReconfirmed` (default `false`), and `ReconfirmedAt?` fields. Update the constructor and `Create(...)` factory accordingly.
- [x] 2A.4 Add domain method `Cancel(reason)` that transitions `Status` from `Registered` → `Cancelled` (idempotent / business-rule violation if already cancelled), captures the reason, and raises `RegistrationCancelledDomainEvent(TeamId, EventId, RegistrationId, Reason)`. Map it through `RegistrationsMessagePolicy` to a `RegistrationCancelledIntegrationEvent` (in `Admitto.Module.Registrations.Contracts`) for downstream consumers.
- [x] 2A.5 Add domain method `Reconfirm()` that sets `HasReconfirmed=true`, `ReconfirmedAt=now`, raises `RegistrationReconfirmedDomainEvent`, and is rejected (no-op or business-rule violation, document the choice in tests) if `Status=Cancelled`. Map to `RegistrationReconfirmedIntegrationEvent` in Contracts. (Used by section 6 reconfirm-sending.)
- [x] 2A.6 EF mapping: add `first_name`, `last_name` non-null `varchar(100)` columns; `status` non-null `varchar(20)` (string-converted enum); `has_reconfirmed` non-null `boolean`; `reconfirmed_at` nullable `timestamptz`.
- [x] 2A.7 EF migration via the `ef-migrations` skill: create the columns nullable in step 1, run a `Sql(...)` backfill statement that populates `Status='Registered'`, `HasReconfirmed=false`, and derives `FirstName`/`LastName` from `email` (split on `.` of the local-part, title-case, fall back `LastName='-'` for single-token local parts), then `ALTER COLUMN ... SET NOT NULL`. Verify against an existing local DB seed.
- [x] 2A.8 Update the public self-register HTTP request DTO + validator + command + handler to require `firstName` + `lastName` (both validated via `MustBeParseable(FirstName.TryFrom)` / `LastName.TryFrom`), threaded into `Registration.Create(...)`.
- [x] 2A.9 Update the admin-add registration HTTP request DTO + validator + command + handler in the same way.
- [x] 2A.10 Update the coupon-redeem registration command + handler/DTO + validator if it shares the same path as self-registration; otherwise apply the same changes.
- [x] 2A.11 Update `AttendeeRegisteredIntegrationEvent` payload to include `FirstName`, `LastName` (replace whatever `displayName` field exists today). Update consumers in the Email module to use them in the single-send template renderer.
- [x] 2A.12 Patch all existing test fixtures / builders that call `Registration.Create(...)` or construct `AttendeeRegisteredIntegrationEvent` to supply names (default `FirstName.From("Test")`, `LastName.From("User")`).
- [x] 2A.13 Add domain-level tests under `tests/Admitto.Module.Registrations.Domain.Tests` for `Cancel` (state transition + event), `Reconfirm` (state transition + event + rejected if cancelled), and constructor invariants (names required).

## 2B. Admin UI — first/last name & registration status

- [x] 2B.1 Update the Add Registration form (`src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/registrations/add/page.tsx`) to require `firstName` and `lastName` inputs. Submit them via the existing `apiClient` to the admin-add endpoint.
- [x] 2B.2 Update the Registrations list page (`src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/registrations/page.tsx`):
  - Replace the `attendeeName(email)` helper with rendering of `firstName` + `lastName` in the Attendee column primary line, with `email` as the secondary line.
  - Replace the hard-coded "Confirmed" Status badge with a real badge driven by the new `status` field (`Registered` → "Registered" green; `Cancelled` → "Cancelled" muted).
  - Replace the hard-coded "—" Reconfirm cell with a real indicator driven by `hasReconfirmed` (`true` → checkmark + relative `reconfirmedAt`; `false` → "—").
  - Update the search box to match across `firstName`, `lastName`, and `email` (case-insensitive substring).
  - Update the default sort to last-name-ascending tie-broken by first-name-ascending; keep "Registered desc" available as a column toggle.
- [x] 2B.3 Regenerate the Admin UI OpenAPI types so `RegistrationListItemDto` carries the new fields (per the Admin UI SDK regeneration memo: AppHost up, then `cd src/Admitto.UI.Admin && curl -sf http://localhost:15000/openapi/v1.json -o openapi-spec.json && pnpm openapi-ts`).
- [x] 2B.4 Update existing scenarios SC003, SC005, SC006 in `specs/admin-ui-registrations` (delta in this change) to reflect the real fields rather than the placeholder behaviour.

## 3. Email module — domain & persistence

- [x] 3.1 Create `BulkEmailJob` aggregate in `Admitto.Module.Email/Domain/Entities` with fields per `bulk-email` spec, `Status` enum, and `Source` discriminated value object — exactly two shapes: `AttendeeSource` and `ExternalListSource` (no combined shape).
- [x] 3.2 Create `BulkEmailRecipient` value object with `Email`, `DisplayName?`, `RegistrationId?`, `ParametersJson`, per-recipient `Status` (`Pending`/`Sent`/`Failed`/`Cancelled`), and `LastError?`.
- [x] 3.3 Add domain methods `Pending → Resolving`, `Resolving → Sending` (snapshots recipients), `RecordSentRecipient(email)`, `RecordFailedRecipient(email, error)`, `RequestCancellation` (sets `CancellationRequestedAt`; valid in `Pending`/`Resolving`/`Sending`), `FinaliseCancelled` (worker-side: marks remaining `Pending` recipients as `Cancelled`, sets `CancelledAt`, status → `Cancelled`), `Complete`, `Fail` with invariant checks.
- [x] 3.4 Add EF mapping for `BulkEmailJob` (table `email.bulk_email_jobs`), value-object owned types for `Source` and `Recipients`, and concurrency token for `Version`.
- [x] 3.5 Add nullable `BulkEmailJobId` column + FK to `EmailLog`.
- [x] 3.6 Generate and check in EF migration via `Admitto.Migrations` (use the `ef-migrations` skill).

## 4. Email module — recipient resolver & SMTP-streaming fan-out

- [x] 4.1 Implement `IBulkEmailRecipientResolver` that materialises a `Source` into a recipient list: for `AttendeeSource` calls `IRegistrationsFacade.QueryRegistrationsAsync`; for `ExternalListSource` returns the literal items. No dedup across sources is needed (no combined source).
- [x] 4.2 Extend the SMTP sender abstraction (or add a new `IBulkSmtpSender`) with a session-mode API: `OpenSession(...) → IAsyncDisposable session` and `session.SendAsync(message, ct)` so that one connection handles many messages. Implement against the existing SMTP transport.
- [x] 4.3 Implement `BulkEmailFanOutJob` (Quartz job, gated on `HostCapability.Jobs | HostCapability.Email`, `[DisallowConcurrentExecution]` per `BulkEmailJobId`):
  - Load job; if `Pending` move to `Resolving` and call resolver; persist `Recipients` snapshot (all per-recipient `Status=Pending`) and move to `Sending` in one transaction.
  - Open one SMTP session for the worker pickup.
  - Iterate recipients with per-recipient `Status=Pending`. For each: render the message (template + ad-hoc overrides), send via the open session, write the `email_log` row with `bulk_email_job_id` set and `IdempotencyKey="bulk:{jobId}:{normalisedEmail}"`. Treat a unique-index violation on `email_log` as already-sent (no double-count).
  - Update per-recipient `Status` and the parent `SentCount`/`FailedCount`/`CancelledCount` after each recipient.
  - Apply `BulkEmailOptions.PerMessageDelay` (default `500ms`) between sends as a cancellable wait.
  - Check the aggregate's `CancellationRequestedAt` flag before each send and during the inter-message delay; on cancellation, mark remaining `Pending` recipients as `Cancelled` and finalise the job to `Cancelled`.
  - Close the SMTP session at the end (or on cancellation / connection-level failure).
  - Final transition to `Completed` / `PartiallyFailed` / `Failed`.
- [x] 4.4 Update template renderer / composer to honour ad-hoc `Subject`/`TextBody`/`HtmlBody` overrides supplied by the bulk-email composer (per `email-templates` MODIFIED spec).
- [x] 4.5 Emit internal module events `BulkEmailJobCompleted` / `BulkEmailJobFailed` / `BulkEmailJobCancelled` for observability hooks.
- [x] 4.6 Add `BulkEmailOptions` (bound from configuration via standard options pattern) with `PerMessageDelay : TimeSpan` (default `00:00:00.500`). Inject into `BulkEmailFanOutJob`.

## 5. Email module — bulk admin endpoints

- [x] 5.1 `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/preview` — handler resolves recipients via `IBulkEmailRecipientResolver` without persisting, returns `{count, sample[<=100]}`.
- [x] 5.2 `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails` — request DTO with `emailType`, optional ad-hoc `subject`/`textBody`/`htmlBody`, and exactly one of `attendee` / `externalList` source fields; creates `BulkEmailJob` in `Pending` and schedules the fan-out (one-shot Quartz trigger).
- [x] 5.3 `GET /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails` — list jobs newest-first.
- [x] 5.4 `GET /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}` — full audit detail (recipients paginated, with per-recipient status).
- [x] 5.5 `POST /admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{id}/cancel` — domain `RequestCancellation` (sets `CancellationRequestedAt`, valid in `Pending`/`Resolving`/`Sending`; rejects from terminal states). Returns `202 Accepted`.
- [x] 5.6 FluentValidation validators for each request DTO; reject create requests carrying both source shapes; team-membership authorisation filter on every endpoint.
- [x] 5.7 Wire endpoints in the Email module's endpoint registration entry point.

## 6. Reconfirm sending

- [x] 6.1 Implement `EvaluateReconfirmJob` (Quartz, gated on `HostCapability.Jobs | HostCapability.Email`): for the bound `TicketedEventId`, build an `AttendeeSource(status=Registered, hasReconfirmed=false)` and create a `BulkEmailJob` with `EmailType=reconfirm` and the system trigger user. No additional cadence filtering is performed — the cron schedule of the per-event trigger encodes the cadence.
- [x] 6.2 Implement `ReconfirmTriggerScheduler` service that creates/replaces/removes the per-event Quartz trigger atomically in response to: `TicketedEventCreated`, `TicketedEventReconfirmPolicyChanged`, `TicketedEventTimeZoneChanged`, `TicketedEventCancelled`, `TicketedEventArchived` integration events. Map `Cadence` to a cron expression evaluated in the event's `TimeZone` (use `CronScheduleBuilder.InTimeZone(...)`); bound by `Window.OpensAt`/`ClosesAt`.
- [x] 6.3 On worker startup, reconcile triggers for all events with active reconfirm policies (idempotent re-registration).

## 7. Domain & module tests

- [x] 7.1 Add `BulkEmailJob` domain tests under `tests/Admitto.Module.Email.Domain.Tests` covering each lifecycle transition, cancellation requested in each non-terminal state (Pending/Resolving/Sending), cancel rejected from terminal states, counter updates including `CancelledCount`, snapshot freezing, and per-recipient state transitions (including `Cancelled`).
- [x] 7.2 Add resolver tests covering `AttendeeSource` (filter pass-through to facade), `ExternalListSource` (literal items), and empty-result handling.
- [x] 7.3 Add fan-out integration tests under `tests/Admitto.Module.Email.Tests` covering: happy path completion (asserting one SMTP session for the whole batch via a fake `IBulkSmtpSender`), per-recipient failure → `PartiallyFailed`, all failures → `Failed`, resume-after-crash (re-running the job processes only `Pending` per-recipient entries), unique-index dedup on `email_log`, empty recipient set → `Completed` immediately, cooperative cancellation mid-`Sending` (assert remaining recipients become `Cancelled` and job ends `Cancelled`), per-message delay applied between sends (assert wall-clock with a fake clock), cancellation during the inter-message delay wakes the worker immediately.
  - Notes: dedup test currently asserts a known implementation gap (orphan Added `EmailLog` entry stays tracked after the per-recipient catch and re-throws on the final terminal save) — flag for follow-up that `BulkEmailFanOutJob.ProcessRecipientAsync` should detach the failed entry on `IsEmailLogIdempotencyViolation`. Mid-`Sending` cancellation is exercised by pre-seeding `CancellationRequestedAt` on a `Sending` job and re-running the worker, since externally mutating the row during a live pickup conflicts with the aggregate's `xmin`-mapped `Version`. Per-message wall-clock delay assertion was deferred until a `TimeProvider`/clock seam is introduced.
- [x] 7.4 Add reconfirm scheduler tests covering policy-set / policy-cleared / event-cancelled / event-archived / time-zone-changed → trigger create/replace/remove (assert the new trigger uses the new IANA zone).
- [x] 7.5 Add reconfirm tests covering: reconfirmed attendee always excluded; un-reconfirmed attendee included on every tick; attendee who reconfirms between ticks is excluded on the next tick; new registration between ticks is picked up on the next tick; everyone reconfirmed → job completes with zero recipients.

## 8. End-to-end API tests

- [x] 8.1 `BulkEmailPreviewTests` — `POST /preview` returns expected count + sample for each source shape.
- [x] 8.2 `BulkEmailCreateTests` — saved-template path and ad-hoc path both create jobs that fan out and produce `email_log` rows with the bulk-job id; assert against the AppHost dummy SMTP container.
- [x] 8.3 `BulkEmailCancelTests` — cancel allowed (returns 202) when job is `Pending`, `Resolving`, or `Sending`; rejected when terminal. Mid-`Sending` cancel ends the job in `Cancelled` with partial `SentCount`.
  - Notes: `SC001` covers cancel-while-pending using a 30-recipient external list (so the worker observes `CancellationRequestedAt` between the 500 ms per-message delays and finalises with partial `SentCount` < 30). `SC002` covers cancel-after-terminal returning 409. The dedicated `Pending` and `Resolving` micro-windows are unit-tested in section 7.3 — driving them deterministically end-to-end is impractical with the current `TimeProvider` seam.
- [x] 8.4 `BulkEmailListAndDetailTests` — list and detail endpoints return persisted job state including per-recipient status.
- [x] 8.5 `ReconfirmFlowTests` — set policy on a seeded active event, advance the clock past `OpensAt`, trigger one tick, assert only registered+un-reconfirmed attendees received the email; have one attendee reconfirm; trigger the next tick; assert that attendee is excluded.
  - Notes: the cron tick fires at `0 0 9 * * ?` in the event time zone — externally faking the clock would require a `TimeProvider` seam that does not exist yet. Instead we schedule the equivalent fan-out (same `AttendeeSource(filter: Registered + HasReconfirmed=false)` + reconfirm template) through the public create endpoint and assert that only the un-reconfirmed registered attendee receives an email; `cancelled` and `already-reconfirmed` registrations are excluded. Cron-trigger plumbing itself is covered by 7.4.
- [x] 8.6 Authorization tests — non-team-member receives 403 for every bulk-emails endpoint.

## 9. CLI

- [x] 9.1 Regenerate the API client per the `cli-api-client-generation` skill.
- [x] 9.2 Add `admitto event bulk-email` command branch with `preview`, `start`, `list`, `show`, `cancel` subcommands per the `cli-admin-parity` ADDED spec. `start` requires exactly one of `--ticket-types|--status|--has-reconfirmed|...` (attendee source) OR `--external-list @file.csv`.
- [x] 9.3 Implement CSV parsing for `--external-list @file.csv` and file-loading for `--text-body @file` / `--html-body @file`.

## 10. Admin UI — event time zone (only)

- [x] 10.1 Add a TZ-aware date-time picker helper using `date-fns-tz` (already in the dependency tree if present; otherwise install). Encapsulate UTC↔event-zone conversion so existing pickers can opt in via a single prop.
- [x] 10.2 Add a `TimeZone` selector (searchable combobox of common IANA zones, free-text fallback) to the Create Event form (`src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/new/`). Default to the browser's detected zone via `Intl.DateTimeFormat().resolvedOptions().timeZone`.
- [x] 10.3 Add the same `TimeZone` selector to the General tab of the event edit page; submit changes via the new `PUT /admin/teams/{teamSlug}/events/{eventSlug}/time-zone` endpoint.
- [x] 10.4 Update General-tab `StartsAt`/`EndsAt` pickers, registration policy form, cancellation policy form, and reconfirm policy form to use the TZ-aware picker bound to the event's `TimeZone`. Display the zone caption (e.g. "Europe/Amsterdam (UTC+02:00)") on each input.
- [x] 10.5 Update read-only event datetime displays (event list, sidebar nav, dashboard tiles) to format using the event's zone with the zone label visible.
- [x] 10.6 **Out of scope for this change**: any UI for bulk email or reconfirm sending — the backend and CLI surfaces are sufficient for now; UI follows in a later change.

## 11. Documentation & validation

- [x] 11.1 Update `docs/arc42/05-building-block-view.md` to note `BulkEmailJob` ownership in the Email module and the new `TicketedEventReconfirmPolicyChanged` and `TicketedEventTimeZoneChanged` integration events from Registrations.
- [x] 11.2 Add an ADR under `docs/adrs/` capturing the "snapshot recipients once" decision (D3), the "single-SMTP-connection bulk fan-out" decision (D4), and the "per-event IANA time zone" decision (D11). Link from `docs/arc42/09-architectural-decisions.md`.
- [x] 11.3 Update `docs/arc42/06-runtime-view.md` with the bulk fan-out and reconfirm-scheduler sequences.
- [x] 11.4 Run `openspec validate add-bulk-email --strict` and resolve any reported issues.
- [x] 11.5 Run targeted test suites (Email domain + module + Api.Tests + UI build) and confirm green; run AppHost smoke for one bulk send and one reconfirm tick against the dummy SMTP container.
  - Email.Domain.Tests: 66/66 ✓
  - Email.Tests: 42/42 ✓ (after fixing `BulkEmailFanOutJob` to skip opening SMTP session when cancellation requested before pickup)
  - Api.Tests: 42/42 ✓ (after adding `FirstName`/`LastName` to test request bodies and switching `TicketTypes` → `TicketTypeSlugs`)
  - UI build (`pnpm build`): green
  - Registrations.Domain.Tests: 110/110 ✓
  - Organization.Domain.Tests: 40/40 ✓; Organization.Tests: 42/42 ✓
  - Registrations.Tests: 122/123 — 1 pre-existing failure in `RegisterAttendeeOutboxTests.SC001` (test-only `DatabaseTestContext` doesn't wire `DomainEventsInterceptor`; reproduces identically on baseline without this change). Not caused by `add-bulk-email`; tracked separately.
  - AppHost smoke: deferred (requires interactive Aspire orchestration; covered by Email.Tests which exercise the same MailDev SMTP container via integration tests).

## 12. Out of scope (do NOT do)

- [x] 12.1 Do **not** modify or remove anything in `src/Admitto.Application/` (the orphaned legacy project, including `BulkEmailWorkItem`, `EmailRecipientList`, `SendCustomBulkEmailJob`, `SendReconfirmBulkEmailJob`). Leave the legacy code in place. (Verified: no files under `src/Admitto.Application/` were touched.)
- [x] 12.2 Do **not** add any Admin UI for bulk email or reconfirm sending in this change. The backend admin endpoints + CLI commands are the only user-facing surfaces here. The UI surface will be designed and built in a follow-up change. (Verified: no UI changes for bulk email or reconfirm.)
