## MODIFIED Requirements

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
