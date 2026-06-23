# email-sending Specification

## Purpose
TBD - created by archiving change add-email-module. Update Purpose after archive.
## Requirements
### Requirement: Email module sends a registration-confirmation email when an attendee is registered
The Email module SHALL send one registration-confirmation ("ticket") email per successful attendee registration when a worker can acquire the e-mail send claim for that registration occurrence. The email SHALL be triggered by the `AttendeeRegisteredIntegrationEvent` published by the Registrations module. The trigger handler SHALL prepare durable e-mail work by writing an `EmailLog` claim and enqueueing an internal delivery command in the Email outbox; actual SMTP delivery SHALL happen only from the delivery command after that claim has been committed by the message dispatcher. The email SHALL be sent via the SMTP server identified by the effective email settings for the event (see `email-settings`). Email composition SHALL use the resolved template for type `ticket` (see `email-templates`). Sending SHALL happen out-of-band from the originating registration request; the registration MUST succeed even if the email cannot be sent. A successfully committed outbox message that cannot be flushed to the queue immediately SHALL remain pending and be dispatched later by Worker-owned outbox retry processing.

#### Scenario: Successful send for a self-service registration
- **WHEN** an attendee "alice@example.com" successfully self-registers for event "DevConf" whose effective email settings are valid and a `ticket` template resolves
- **THEN** within the worker's processing window, one email of type `ticket` is sent to "alice@example.com" via the configured SMTP server, addressed from the configured from-address, and the email log records the send as `Sent`

#### Scenario: Send is not coupled to the registration request
- **WHEN** an attendee successfully registers but the SMTP server is temporarily unreachable
- **THEN** the registration response still indicates success, the integration event is enqueued or remains in a pending outbox row, and the email remains retryable until it succeeds or reaches the configured terminal failure policy

#### Scenario: Immediate outbox flush fails after registration commit
- **WHEN** registration commits an `AttendeeRegisteredIntegrationEvent` to the Registrations outbox but the immediate queue send fails
- **THEN** the outbox row remains `Pending` and Worker-owned outbox retry processing later sends it to the queue without requiring another registration request

#### Scenario: Delivery command is committed before SMTP
- **WHEN** the Email module handles an `AttendeeRegisteredIntegrationEvent`
- **THEN** it commits the `EmailLog` claim and internal delivery command before any SMTP send is attempted

#### Scenario: No email configuration no send no error to attendee
- **WHEN** an attendee successfully registers for an event whose effective email settings are absent or invalid
- **THEN** the registration succeeds, no SMTP send is attempted, and the email log records a terminal `Failed` entry with reason "email not configured"

---

### Requirement: Sending is idempotent across at-least-once delivery
The Email module SHALL use a committed `EmailLog` row for `(TicketedEventId, recipient, IdempotencyKey)` as the authoritative send-claim and deduplication mechanism. The idempotency key for the registration trigger SHALL be derived deterministically from the registration occurrence: `attendee-registered:{registrationId}:{registeredAt}` where `registeredAt` is the ISO 8601 timestamp captured at the moment of registration or re-registration (i.e. the value changes when a cancelled registration is reset, ensuring a fresh confirmation email can be sent). The claim SHALL be committed before SMTP is attempted. Redelivered trigger messages SHALL skip SMTP when they observe a terminal claim and MAY enqueue delivery again when they observe a pending claim. Because SMTP is not transactionally coupled to the database, the system SHALL minimize duplicate sends but does not claim mathematically perfect exactly-once delivery for rare duplicate delivery races or crashes between SMTP success and final log update.

#### Scenario: Duplicate integration event delivery after sent log exists
- **WHEN** the Registrations module's `AttendeeRegisteredIntegrationEvent` for registration `R1` is delivered to the Email module after the corresponding email log row is already `Sent`
- **THEN** no additional SMTP send is attempted, and the email log still contains exactly one row for `(eventId, recipient, idempotencyKey)`

#### Scenario: Concurrent workers race while preparing the same trigger
- **WHEN** two worker instances process the same triggering message while preparing durable e-mail work
- **THEN** the unique email-log send claim for `(ticketed_event_id, recipient, idempotency_key)` prevents duplicate log rows, and workers that observe a terminal claim skip SMTP

#### Scenario: Worker crashes after claim before SMTP
- **WHEN** a worker commits an email-log send claim and crashes before attempting SMTP
- **THEN** the pending claimed row remains recoverable, and a later trigger redelivery or delivery-command retry may attempt SMTP

#### Scenario: Worker crashes after SMTP success before final log update
- **WHEN** a worker sends SMTP successfully but crashes before updating the email log to `Sent`
- **THEN** a later trigger redelivery or delivery-command retry may send again, and the system records this as the known non-transactional SMTP duplicate window rather than promising perfect exactly-once delivery

#### Scenario: Transient SMTP failure retries before requeue
- **WHEN** SMTP delivery fails with a transient transport error
- **THEN** the delivery handler retries a bounded number of times inline with a small configurable delay between attempts before recording retry metadata and enqueueing another delivery attempt when retry policy allows it

---

### Requirement: Bulk e-mail recipient sends use database-backed claims
For bulk e-mail jobs, the Email module SHALL acquire the per-recipient `EmailLog` claim before sending SMTP to that recipient. Bulk fan-out SHALL remain inside the Quartz bulk job and SHALL NOT enqueue one delivery command per recipient by default. A pre-existing terminal log row for the same `(TicketedEventId, recipient, IdempotencyKey)` SHALL prevent another SMTP send and SHALL be reflected in the recipient snapshot outcome. Duplicate claim conflicts SHALL be handled without leaving failed added entities tracked for later `SaveChanges` attempts.

#### Scenario: Pre-existing bulk log prevents duplicate SMTP
- **WHEN** a bulk fan-out job processes a recipient whose computed idempotency key already has a terminal `Sent` email log row
- **THEN** the job does not send SMTP for that recipient, does not insert a duplicate log row, and continues processing remaining recipients

#### Scenario: Bulk duplicate claim conflict is recoverable
- **WHEN** a bulk fan-out worker hits the email-log unique index while claiming or recording a recipient outcome
- **THEN** the worker reloads the existing row, clears any failed tracked insert state, records the recipient outcome consistently, and completes or continues the job without rethrowing the same duplicate insert on final save

#### Scenario: Bulk delivery keeps one SMTP session per pickup
- **WHEN** a bulk fan-out job sends multiple recipients
- **THEN** it uses the existing single SMTP session per pickup and does not create per-recipient queue messages for normal delivery

### Requirement: Pending outbox rows are retried by the Worker
Every module outbox that persists `Pending` messages SHALL be scanned by Worker-owned background processing. The scanner SHALL send pending rows older than the configured retry minimum age to the queue and mark them `Sent` after successful queue send. The scanner SHALL be safe to run on multiple Worker instances; duplicate queue sends caused by send-success/mark-failure races are permitted and SHALL be handled by downstream idempotency.

#### Scenario: Pending outbox row is eventually sent
- **WHEN** a module outbox contains a `Pending` message from a previously committed unit of work
- **THEN** Worker-owned outbox retry processing sends it to the queue after the row reaches the configured minimum age and marks the row `Sent`

#### Scenario: Outbox scanner duplicate race
- **WHEN** two Worker instances race on the same pending outbox row
- **THEN** at least one sends the message successfully, the row eventually becomes `Sent`, and any duplicate queue delivery is handled by the receiving handler's idempotency behavior

---

### Requirement: SMTP sending is gated to hosts that declare the Email capability
The SMTP-sending delivery command handler and the `IEmailSender` implementation SHALL be registered only in hosts that declare `HostCapability.Email`. The integration-event handler that translates `AttendeeRegisteredIntegrationEvent` into a `SendEmailCommand` SHALL NOT be capability-gated and SHALL run in any host that processes the Registrations module's outbound queue.

#### Scenario: Worker host registers the SMTP sender
- **WHEN** the Worker host (which declares `HostCapability.Email`) starts up
- **THEN** the SMTP `IEmailSender` and `DeliverEmailCommandHandler` are registered in DI

#### Scenario: API host does not register the SMTP sender
- **WHEN** the API host (which does NOT declare `HostCapability.Email`) starts up
- **THEN** the `DeliverEmailCommandHandler` is skipped during assembly scanning, and the host has no SMTP outbound dependency

---

### Requirement: Email module sends a cancellation email when a registration is cancelled

The Email module SHALL handle `RegistrationCancelledIntegrationEvent` and dispatch a cancellation email to the attendee. The template type SHALL be determined by the `Reason` field: `AttendeeRequest` → `cancellation`; `VisaLetterDenied` → `visa-letter-denied`; `ReconfirmAutoCancel` → `reconfirm-cancelled`. The idempotency key SHALL be `registration-cancelled:{registrationId}`.

#### Scenario: Cancellation with AttendeeRequest sends cancellation template

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = AttendeeRequest`
- **WHEN** the handler processes the event
- **THEN** a `SendEmailCommand` is dispatched using the `cancellation` template type

#### Scenario: Cancellation with VisaLetterDenied sends visa-letter-denied template

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = VisaLetterDenied`
- **WHEN** the handler processes the event
- **THEN** a `SendEmailCommand` is dispatched using the `visa-letter-denied` template type

#### Scenario: Cancellation with ReconfirmAutoCancel sends reconfirm-cancelled template

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = ReconfirmAutoCancel`
- **WHEN** the handler processes the event
- **THEN** a `SendEmailCommand` is dispatched using the `reconfirm-cancelled` template type

#### Scenario: Cancellation email is idempotent

- **GIVEN** a `RegistrationCancelledIntegrationEvent` that has already been handled
- **WHEN** the same event is processed again
- **THEN** no additional email is sent (idempotency key `registration-cancelled:{registrationId}`)

#### Scenario: No email configuration skips send without error

- **GIVEN** a ticketed event with no email configuration
- **WHEN** the handler processes the event
- **THEN** no email is sent and no error is raised

#### Scenario: Template parameters are populated

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with all fields present
- **WHEN** the email is sent
- **THEN** the template receives `first_name`, `last_name`, `event_name`, `event_website`, and `register_link`

---

### Requirement: Template render failures do not poison the queue
When email composition or rendering fails (e.g. malformed Scriban template, missing variable), the Email module SHALL record a `Failed` entry in the email log with the error detail and SHALL acknowledge the underlying queue message. The system SHALL NOT enter an indefinite retry loop on a deterministic rendering failure.

#### Scenario: Malformed event-level template
- **WHEN** the resolved `ticket` template for event "DevConf" contains an unparseable Scriban expression
- **THEN** no SMTP send is attempted, the email log records a `Failed` row whose `LastError` describes the parse failure, and the queue message is acknowledged

---
