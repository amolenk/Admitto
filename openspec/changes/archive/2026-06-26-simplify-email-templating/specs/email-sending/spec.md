## MODIFIED Requirements

### Requirement: Email module sends a registration-confirmation email when an attendee is registered
The Email module SHALL send one registration-confirmation ("ticket") email per successful attendee registration when a worker can acquire the e-mail send claim for that registration occurrence. The email SHALL be triggered by the `AttendeeRegisteredIntegrationEvent` published by the Registrations module. The trigger handler SHALL prepare durable e-mail work by writing an `EmailLog` claim and enqueueing an internal delivery command in the Email outbox; actual SMTP delivery SHALL happen only from the delivery command after that claim has been committed by the message dispatcher. The email SHALL be sent via the SMTP server identified by the owning team's email settings (see `email-settings`). Email composition SHALL use built-in themed content for type `ticket` (see `email-templates`). Sending SHALL happen out-of-band from the originating registration request; the registration MUST succeed even if the email cannot be sent. A successfully committed outbox message that cannot be flushed to the queue immediately SHALL remain pending and be dispatched later by Worker-owned outbox retry processing.

#### Scenario: Successful send for a self-service registration
- **WHEN** an attendee "alice@example.com" successfully self-registers for event "DevConf" whose owning team has valid email settings
- **THEN** within the worker's processing window, one email of type `ticket` is sent to "alice@example.com" via the configured SMTP server, addressed from the configured from-address, rendered from built-in themed content, and the email log records the send as `Sent`

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
- **WHEN** an attendee successfully registers for an event whose owning team's email settings are absent or invalid
- **THEN** the registration succeeds, no SMTP send is attempted, and the email log records a terminal `Failed` entry with reason "email not configured"

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

#### Scenario: No email configuration skips send without error
- **GIVEN** a ticketed event whose owning team has no email configuration
- **WHEN** the handler processes the event
- **THEN** no email is sent and no error is raised

#### Scenario: Template parameters are populated
- **GIVEN** a `RegistrationCancelledIntegrationEvent` with all fields present
- **WHEN** the email is sent
- **THEN** the built-in content receives `first_name`, `last_name`, `event_name`, `event_website`, and `register_link`

### Requirement: Template render failures do not poison the queue
When email composition or rendering fails (e.g. malformed built-in content or malformed custom bulk job content), the Email module SHALL record a `Failed` entry in the email log with the error detail and SHALL acknowledge the underlying queue message when the failure is deterministic for the current payload. The system SHALL NOT enter an indefinite retry loop on a deterministic rendering failure.

#### Scenario: Malformed built-in transactional content
- **WHEN** the built-in `ticket` content contains an unparseable Scriban expression
- **THEN** no SMTP send is attempted, the email log records a `Failed` row whose `LastError` describes the parse failure, and the queue message is acknowledged

#### Scenario: Malformed custom bulk job content
- **WHEN** a custom bulk job's HTML body contains an unparseable Scriban expression
- **THEN** the recipient send is recorded as failed and the bulk job continues or finalises according to bulk-email failure rules
