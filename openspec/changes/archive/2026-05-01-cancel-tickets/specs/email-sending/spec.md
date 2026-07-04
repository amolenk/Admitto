## Capability: email-sending (delta)

### Summary

The Email module handles `RegistrationCancelledIntegrationEvent` and dispatches a cancellation email to the attendee. The template type is determined by the `Reason` field: `AttendeeRequest` → `cancellation`; `VisaLetterDenied` → `visa-letter-denied`.

### Acceptance Scenarios

**SC001_CancellationEmail_AttendeeRequest_SendsCancellationTemplate**
Given a `RegistrationCancelledIntegrationEvent` with `Reason = AttendeeRequest`
When the handler processes the event
Then a `SendEmailCommand` is dispatched using the `cancellation` template type.

**SC002_CancellationEmail_VisaLetterDenied_SendsVisaLetterDeniedTemplate**
Given a `RegistrationCancelledIntegrationEvent` with `Reason = VisaLetterDenied`
When the handler processes the event
Then a `SendEmailCommand` is dispatched using the `visa-letter-denied` template type.

**SC003_CancellationEmail_Idempotent_SecondEventIsNoOp**
Given a `RegistrationCancelledIntegrationEvent` that has already been handled
When the same event is processed again
Then no additional email is sent (idempotency key `registration-cancelled:{registrationId}`).

**SC004_CancellationEmail_NoEmailConfig_SkipsSend**
Given a ticketed event with no email configuration
When the handler processes the event
Then no email is sent and no error is raised.

**SC005_CancellationEmail_TemplateParameters_ArePopulated**
Given a `RegistrationCancelledIntegrationEvent` with all fields present
When the email is sent
Then the template receives `first_name`, `last_name`, `event_name`, `event_website`, and `register_link`.
