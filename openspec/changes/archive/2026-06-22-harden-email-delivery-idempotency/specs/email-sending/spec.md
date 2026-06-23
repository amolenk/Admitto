## MODIFIED Requirements

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
The Email module SHALL ensure that the same triggering integration event redelivered any number of times causes at most one worker at a time to attempt SMTP for `(TicketedEventId, recipient, IdempotencyKey)`. The idempotency key for the registration trigger SHALL be derived deterministically from the registration occurrence: `attendee-registered:{registrationId}:{registeredAt}` where `registeredAt` is the ISO 8601 timestamp captured at the moment of registration or re-registration (i.e. the value changes when a cancelled registration is reset, ensuring a fresh confirmation email can be sent). A unique database index on `(ticketed_event_id, recipient, idempotency_key)` in the email log SHALL be the authoritative send-claim and deduplication mechanism. The claim SHALL be committed before SMTP is attempted. Because SMTP is not transactionally coupled to the database, the system SHALL minimize duplicate sends but does not claim mathematically perfect exactly-once delivery after crashes between SMTP success and final log update.

#### Scenario: Duplicate integration event delivery after sent log exists
- **WHEN** the Registrations module's `AttendeeRegisteredIntegrationEvent` for registration `R1` is delivered to the Email module after the corresponding email log row is already `Sent`
- **THEN** no additional SMTP send is attempted, and the email log still contains exactly one row for `(eventId, recipient, idempotencyKey)`

#### Scenario: Concurrent workers race on the same trigger before send
- **WHEN** two worker instances process the same triggering message before either has attempted SMTP
- **THEN** exactly one worker commits the email-log send claim for `(ticketed_event_id, recipient, idempotency_key)`, only the claim owner attempts SMTP, and the losing worker skips or retries without sending

#### Scenario: Worker crashes after claim before SMTP
- **WHEN** a worker commits an email-log send claim and crashes before attempting SMTP
- **THEN** the claimed row remains recoverable, and a later retry may acquire the stale claim and attempt SMTP according to the configured stale-claim policy

#### Scenario: Worker crashes after SMTP success before final log update
- **WHEN** a worker sends SMTP successfully but crashes before updating the email log to `Sent`
- **THEN** a later stale-claim recovery may retry the send, and the system records this as the known non-transactional SMTP duplicate window rather than promising perfect exactly-once delivery

#### Scenario: Transient SMTP failure retries before requeue
- **WHEN** SMTP delivery fails with a transient transport error
- **THEN** the delivery handler retries a bounded number of times inline before recording retry metadata and scheduling or enqueueing a later delivery attempt when retry policy allows it

---

## ADDED Requirements

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
Every module outbox that persists `Pending` messages SHALL be scanned by Worker-owned background processing. The scanner SHALL send pending rows to the queue and mark them `Sent` after successful queue send. The scanner SHALL be safe to run on multiple Worker instances; duplicate queue sends caused by send-success/mark-failure races are permitted and SHALL be handled by downstream idempotency.

#### Scenario: Pending outbox row is eventually sent
- **WHEN** a module outbox contains a `Pending` message from a previously committed unit of work
- **THEN** Worker-owned outbox retry processing sends it to the queue and marks the row `Sent`

#### Scenario: Outbox scanner duplicate race
- **WHEN** two Worker instances race on the same pending outbox row
- **THEN** at least one sends the message successfully, the row eventually becomes `Sent`, and any duplicate queue delivery is handled by the receiving handler's idempotency behavior
