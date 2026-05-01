## Design Overview

The cancel-tickets change wires together two layers: a new admin HTTP endpoint and an email handler in the Email module.

## Key Decisions

### No changes to RegistrationCancelledIntegrationEvent

The existing event already carries `TeamId`, `TicketedEventId`, `RegistrationId`, `RecipientEmail`, and `Reason` (string of enum value). The email handler calls `IRegistrationsFacade.GetTicketedEventEmailContextAsync(ticketedEventId, registrationId)` to retrieve all template context in a single call — no need to add `FirstName`/`LastName` or any other fields to the event. The `RegistrationsMessagePolicy` requires no changes.

### CancellationReason rationalisation

`RequestForVisaLetter` (value 1) is renamed `VisaLetterDenied` — no migration required, integer unchanged.

`Other` (value 3) is removed. A pre-check confirms no rows hold value 3 before deployment.

`TicketTypesRemoved` (value 2) remains as an internal/system-only reason. Cannot be supplied by admin callers. A future "cancel ticket types" feature will use this reason; the email handler treats it as a no-op for now.

### Admin endpoint accepts only two reasons

The HTTP request body `reason` field accepts `AttendeeRequest` or `VisaLetterDenied`. `TicketTypesRemoved` is rejected by FluentValidation.

### Email template routing

| CancellationReason | EmailTemplateType |
|---|---|
| AttendeeRequest | `cancellation` |
| VisaLetterDenied | `visa-letter-denied` |
| TicketTypesRemoved | _(no-op, future change)_ |

### Email template defaults

Two built-in default templates ported from the legacy project:

- `canceled.txt/html` → `cancellation.txt/html` in Email module `Defaults/`.
- `visa-letter-denied.txt/html` copied as-is.

### Email idempotency

Key: `registration-cancelled:{registrationId}` — follows pattern of `attendee-registered:{registrationId}`.

### HTTP verb

`POST` with a request body (reason required).
