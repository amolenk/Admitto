# email-sending Specification

## Purpose
TBD - created by archiving change add-email-module. Update Purpose after archive.
## Requirements
### Requirement: Email module sends a registration-confirmation email when an attendee is registered
The Email module SHALL send exactly one registration-confirmation ("ticket") email per successful attendee registration. The email SHALL be triggered by the `AttendeeRegisteredIntegrationEvent` published by the Registrations module and SHALL be sent via the SMTP server identified by the effective email settings for the event (see `email-settings`). Email composition SHALL use the resolved template for type `ticket` (see `email-templates`). Sending SHALL happen out-of-band from the originating registration request — the registration MUST succeed even if the email cannot be sent.

#### Scenario: Successful send for a self-service registration
- **WHEN** an attendee "alice@example.com" successfully self-registers for event "DevConf" whose effective email settings are valid and a `ticket` template resolves
- **THEN** within the worker's processing window, exactly one email of type `ticket` is sent to "alice@example.com" via the configured SMTP server, addressed from the configured from-address

#### Scenario: Send is not coupled to the registration request
- **WHEN** an attendee successfully registers but the SMTP server is temporarily unreachable
- **THEN** the registration response still indicates success, the integration event is enqueued, and the email is retried by the queue's existing redelivery policy until it succeeds or is marked failed

#### Scenario: No email configuration → no send, no error to attendee
- **WHEN** an attendee successfully registers for an event whose effective email settings are absent or invalid
- **THEN** the registration succeeds, no SMTP send is attempted, and the email log records a `Failed` entry with reason "email not configured"

---

### Requirement: Sending is idempotent across at-least-once delivery
The Email module SHALL ensure that the same triggering integration event redelivered any number of times produces at most one sent email per `(TicketedEventId, recipient, IdempotencyKey)`. The idempotency key for the registration trigger SHALL be derived deterministically from the registration identity (`attendee-registered:{registrationId}`). A unique database index on `(ticketed_event_id, recipient, idempotency_key)` in the email log SHALL be the authoritative deduplication mechanism.

#### Scenario: Duplicate integration event delivery
- **WHEN** the Registrations module's `AttendeeRegisteredIntegrationEvent` for registration `R1` is delivered to the Email module twice (e.g. queue at-least-once)
- **THEN** the recipient receives the email exactly once, and the email log contains exactly one row for `(eventId, recipient, "attendee-registered:R1")`

#### Scenario: Concurrent workers race on the same trigger
- **WHEN** two worker instances pick up the same triggering message before either has written to the email log
- **THEN** the unique index on `(ticketed_event_id, recipient, idempotency_key)` rejects the second insert, and the second worker logs a skip without sending

---

### Requirement: SMTP sending is gated to hosts that declare the Email capability
The SMTP-sending command handler and the `IEmailSender` implementation SHALL be registered only in hosts that declare `HostCapability.Email`. The integration-event handler that translates `AttendeeRegisteredIntegrationEvent` into a `SendEmailCommand` SHALL NOT be capability-gated and SHALL run in any host that processes the Registrations module's outbound queue.

#### Scenario: Worker host registers the SMTP sender
- **WHEN** the Worker host (which declares `HostCapability.Email`) starts up
- **THEN** the SMTP `IEmailSender` and `SendEmailCommandHandler` are registered in DI

#### Scenario: API host does not register the SMTP sender
- **WHEN** the API host (which does NOT declare `HostCapability.Email`) starts up
- **THEN** the `SendEmailCommandHandler` is skipped during assembly scanning, and the host has no SMTP outbound dependency

---

### Requirement: Template render failures do not poison the queue
When email composition or rendering fails (e.g. malformed Scriban template, missing variable), the Email module SHALL record a `Failed` entry in the email log with the error detail and SHALL acknowledge the underlying queue message. The system SHALL NOT enter an indefinite retry loop on a deterministic rendering failure.

#### Scenario: Malformed event-level template
- **WHEN** the resolved `ticket` template for event "DevConf" contains an unparseable Scriban expression
- **THEN** no SMTP send is attempted, the email log records a `Failed` row whose `LastError` describes the parse failure, and the queue message is acknowledged

