# email-sending Specification

## Purpose

The Email module sends application-owned transactional and bulk email through durable claims, outbox-backed work, and SMTP delivery hosted by workers with the Email capability. Transactional emails use deployment-provided system SMTP settings and code-owned built-in themed content.

## Requirements

### Requirement: Email module sends a registration-confirmation email when an attendee is registered

The Email module SHALL send one registration-confirmation ("ticket") email per successful attendee registration when a worker can acquire the e-mail send claim for that registration occurrence. The email SHALL be triggered by the `AttendeeRegisteredIntegrationEvent` published by the Registrations module. The trigger handler SHALL prepare durable e-mail work by writing an `EmailLog` claim and enqueueing an internal delivery command in the Email outbox; actual SMTP delivery SHALL happen only from the delivery command after that claim has been committed by the message dispatcher. The email SHALL be sent via the Admitto system SMTP sender configured for the deployment. Email composition SHALL use the resolved built-in template for type `ticket` and SHALL include team branding values and event public-link values from module-owned context. Sending SHALL happen out-of-band from the originating registration request; the registration MUST succeed even if the email cannot be sent. A successfully committed outbox message that cannot be flushed to the queue immediately SHALL remain pending and be dispatched later by Worker-owned outbox retry processing.

#### Scenario: Successful send for a self-service registration

- **WHEN** an attendee "alice@example.com" successfully self-registers for event "DevConf" and the deployment has valid system SMTP configuration
- **THEN** within the worker's processing window, one email of type `ticket` is sent to "alice@example.com" via the configured Admitto SMTP sender, addressed from the configured Admitto from-address, and the email log records the send as `Sent`

#### Scenario: Send is not coupled to the registration request

- **WHEN** an attendee successfully registers but the SMTP server is temporarily unreachable
- **THEN** the registration response still indicates success, the integration event is enqueued or remains in a pending outbox row, and the email remains retryable until it succeeds or reaches the configured terminal failure policy

#### Scenario: Immediate outbox flush fails after registration commit

- **WHEN** registration commits an `AttendeeRegisteredIntegrationEvent` to the Registrations outbox but the immediate queue send fails
- **THEN** the outbox row remains `Pending` and Worker-owned outbox retry processing later sends it to the queue without requiring another registration request

#### Scenario: Delivery command is committed before SMTP

- **WHEN** the Email module handles an `AttendeeRegisteredIntegrationEvent`
- **THEN** it commits the `EmailLog` claim and internal delivery command before any SMTP send is attempted

#### Scenario: Missing system SMTP configuration no error to attendee

- **WHEN** an attendee successfully registers but the deployment's system SMTP configuration is missing or invalid
- **THEN** the registration succeeds, no SMTP send is attempted successfully, and the email log or operational telemetry records the configuration failure

---

### Requirement: Application email uses Admitto system sender identity

All application email sent by the Email module SHALL use a configured Admitto-controlled sender address for the SMTP `From` address. The visible display name MAY include the event name or another Admitto-controlled display value, but the sender email-address domain SHALL remain Admitto-controlled. When the owning team has an optional reply-to email address, the Email module SHALL set the SMTP `Reply-To` header to that address without changing the `From` address.

#### Scenario: Event display name with Admitto from-address

- **WHEN** the system sends a ticket email for event "Azure Fest 2026"
- **THEN** the message uses an Admitto-controlled `From` address and may use "Azure Fest 2026" as the display name

#### Scenario: Team reply-to does not replace from-address

- **WHEN** the system sends a ticket email for a team with reply-to email address `help@example.com`
- **THEN** the message uses the Admitto-controlled `From` address and `Reply-To: help@example.com`

---

### Requirement: Built-in email templates use team accent color

Built-in email templates SHALL receive the owning team's accent color as a rendering parameter. When the team has no explicit accent color, the system default accent color SHALL be used.

#### Scenario: Team accent color is rendered in ticket email

- **WHEN** team "acme" has accent color `#0f766e` and a ticket email is rendered for one of its events
- **THEN** the rendered email uses `#0f766e` for accent-colored template elements

---

### Requirement: Ticket email includes change-tickets link only for multiple public ticket types

The ticket-confirmation email SHALL include a change-tickets link only when the event has at least two ticket types with `SelfServiceEnabled == true`. Sold-out state and waitlist mode SHALL NOT suppress the link. When fewer than two public self-service ticket types exist, the template SHALL omit the change-tickets section entirely.

#### Scenario: Change-tickets link included for two public ticket types

- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types
- **THEN** the email parameters include a change-tickets link and the rendered ticket email shows the change-tickets CTA

#### Scenario: Change-tickets link omitted for one public ticket type

- **WHEN** a ticket email is prepared for an event with one self-service-enabled ticket type
- **THEN** the email parameters do not include a change-tickets link and the rendered ticket email omits the change-tickets CTA

#### Scenario: Sold-out public ticket still counts

- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types and one is sold out
- **THEN** the email parameters include a change-tickets link

#### Scenario: Waitlist-mode public ticket still counts

- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types and one is in waitlist mode
- **THEN** the email parameters include a change-tickets link

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

---

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

The SMTP-sending delivery command handler and the `IEmailSender` implementation SHALL be registered only in hosts that declare `HostCapability.Email`. The integration-event handlers that translate email-triggering integration events into durable email work SHALL NOT be capability-gated and SHALL run in any host that processes the corresponding outbound queue.

#### Scenario: Worker host registers the SMTP sender

- **WHEN** the Worker host (which declares `HostCapability.Email`) starts up
- **THEN** the SMTP `IEmailSender` and delivery command handler are registered in DI

#### Scenario: API host does not register the SMTP sender

- **WHEN** the API host (which does NOT declare `HostCapability.Email`) starts up
- **THEN** the delivery command handler is skipped during assembly scanning, and the host has no SMTP outbound dependency

---

### Requirement: Email module sends a cancellation email when a registration is cancelled

The Email module SHALL handle `RegistrationCancelledIntegrationEvent` and dispatch a cancellation email to the attendee using built-in themed content. The content type SHALL be determined by the `Reason` field: `AttendeeRequest` -> `cancellation`; `VisaLetterDenied` -> `visa-letter-denied`; `ReconfirmAutoCancel` -> `reconfirm-cancelled`. The idempotency key SHALL be `registration-cancelled:{registrationId}`.

#### Scenario: Cancellation with AttendeeRequest sends cancellation content

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = AttendeeRequest`
- **WHEN** the handler processes the event
- **THEN** a cancellation email is dispatched using built-in `cancellation` content

#### Scenario: Cancellation with VisaLetterDenied sends visa-letter-denied content

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = VisaLetterDenied`
- **WHEN** the handler processes the event
- **THEN** a cancellation email is dispatched using built-in `visa-letter-denied` content

#### Scenario: Cancellation with ReconfirmAutoCancel sends reconfirm-cancelled content

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with `Reason = ReconfirmAutoCancel`
- **WHEN** the handler processes the event
- **THEN** a cancellation email is dispatched using built-in `reconfirm-cancelled` content

#### Scenario: Cancellation email is idempotent

- **GIVEN** a `RegistrationCancelledIntegrationEvent` that has already been handled
- **WHEN** the same event is processed again
- **THEN** no additional email is sent (idempotency key `registration-cancelled:{registrationId}`)

#### Scenario: Missing system SMTP configuration skips send without error

- **GIVEN** the deployment's system SMTP configuration is missing or invalid
- **WHEN** the handler processes the event
- **THEN** no email is sent successfully and no error is raised to the caller

#### Scenario: Template parameters are populated

- **GIVEN** a `RegistrationCancelledIntegrationEvent` with all fields present
- **WHEN** the email is sent
- **THEN** the built-in content receives `first_name`, `last_name`, `event_name`, `event_website`, and `register_link`

---

### Requirement: Template render failures do not poison the queue

When email composition or rendering fails (e.g. malformed built-in content or malformed custom bulk job content), the Email module SHALL record a `Failed` entry in the email log with the error detail and SHALL acknowledge the underlying queue message when the failure is deterministic for the current payload. The system SHALL NOT enter an indefinite retry loop on a deterministic rendering failure.

#### Scenario: Malformed built-in transactional content

- **WHEN** the built-in `ticket` content contains an unparseable Scriban expression
- **THEN** no SMTP send is attempted, the email log records a `Failed` row whose `LastError` describes the parse failure, and the queue message is acknowledged

#### Scenario: Malformed custom bulk job content

- **WHEN** a custom bulk job's HTML body contains an unparseable Scriban expression
- **THEN** the recipient send is recorded as failed and the bulk job continues or finalises according to bulk-email failure rules

---

### Requirement: Email owns team and event rendering context

The Email module SHALL persist Email-owned rendering context projections for each team and ticketed event that can receive application email. The projections SHALL contain only email-rendering, reply routing, and scheduling facts, including the owning team id, ticketed event id, team accent color, optional team reply-to email address, event name, event website URL, public event slug or equivalent link inputs, event time zone, reconfirm policy snapshot when present, self-service ticket-type count, and lifecycle state needed by Email.

The projection SHALL be updated from Organization and Registrations integration events. Email SHALL NOT call Organization synchronously for team branding while preparing application email.

#### Scenario: Team accent color comes from Email projection

- **WHEN** a ticket email is prepared for an event whose Email projection has team accent color `#0f766e`
- **THEN** the email render parameters use `#0f766e` without calling the Organization facade for branding

#### Scenario: Team reply-to comes from Email projection

- **WHEN** a ticket email is sent for a team whose Email projection has reply-to email address `help@example.com`
- **THEN** the SMTP message uses `Reply-To: help@example.com` without calling the Organization facade for team contact metadata

#### Scenario: Event links come from Email projection inputs

- **WHEN** a ticket email is prepared for an event whose Email projection has public slug `devconf-2026`
- **THEN** the ticket, cancel, QR-code, and change-ticket links are derived from the Email projection inputs and configured public base URL

#### Scenario: Missing required rendering context is recorded deterministically

- **WHEN** an email-triggering integration event is handled before the required event rendering context exists in Email
- **THEN** Email records or defers the send according to the send pipeline's deterministic failure/retry rules and does not query sibling module DbContexts directly

### Requirement: Transactional email handlers combine trigger payload facts with Email context

Transactional email handlers SHALL use the triggering integration-event payload for occurrence-specific facts such as recipient address, registration id, attendee name when supplied, ticket names, OTP code, coupon code, cancellation reason, and idempotency timestamp. They SHALL use the Email-owned event rendering context projection for reusable team/event facts such as event name, website URL, public links, team accent color, and change-ticket availability.

Transactional email handlers SHALL NOT use `IRegistrationsFacade.GetEventRegistrationSnapshotAsync` to obtain reusable team/event rendering context.

#### Scenario: Registration confirmation combines payload and projection

- **WHEN** Email handles an `AttendeeRegisteredIntegrationEvent`
- **THEN** recipient, attendee name, ticket names, registration id, and idempotency timestamp come from the event payload, while event name, website URL, public links, team accent color, and change-ticket availability come from the Email projection

#### Scenario: OTP email uses projected branding

- **WHEN** Email handles an OTP-code requested event for an event with projected team accent color
- **THEN** the verification-code email uses the projected accent color and the OTP code from the trigger payload

### Requirement: Email rendering context is eventually consistent

Application email rendering SHALL tolerate eventual consistency between Organization/Registrations changes and the Email-owned rendering context projection. A queued email MAY render using the latest context that has reached Email at handling time. Email SHALL NOT use the projection for registration correctness, authorization, ticket capacity, or attendee eligibility decisions.

#### Scenario: Recent branding change may lag

- **WHEN** a team accent color is changed and a ticket email is handled before the corresponding branding integration event updates Email
- **THEN** the email may render with the previous accent color and the send remains valid

#### Scenario: Projection not used for capacity decisions

- **WHEN** an attendee registers or changes tickets
- **THEN** ticket capacity and lifecycle checks are still enforced by Registrations-owned aggregates, not by Email rendering context
