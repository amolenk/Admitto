# reconfirm-auto-cancel Specification

## Purpose

When the Email module's reconfirm scheduler determines that a registration has exhausted its allowed reconfirm attempts without a response, it publishes a `ReconfirmAutoExpiredIntegrationEvent`. The Registrations module handles this event by cancelling each listed unconfirmed registration with reason `ReconfirmAutoCancel`.

## Requirements

### Requirement: Registrations are auto-cancelled when the email log count reaches MaxReconfirmAttempts

When the Registrations module receives a `ReconfirmAutoExpiredIntegrationEvent` (published by the Email module when a tick identifies registrations whose email log count has reached `MaxReconfirmAttempts`), it SHALL call `registration.Cancel(ReconfirmAutoCancel)` for each listed registration whose current state is `Registered` and `HasReconfirmed=false`. `ReconfirmAutoCancel` is a new value added to the `RegistrationCancellationReason` enum. The handler SHALL be idempotent (use the event-id as deduplication key). Already-cancelled or already-reconfirmed registrations SHALL be silently skipped.

#### Scenario: Registration is cancelled when the event is received

- **WHEN** a `ReconfirmAutoExpiredIntegrationEvent` is received listing a registration with `Status=Registered, HasReconfirmed=false`
- **THEN** `registration.Cancel(ReconfirmAutoCancel)` is called and a `RegistrationCancelledIntegrationEvent` with `Reason=ReconfirmAutoCancel` is enqueued

#### Scenario: Already-reconfirmed registration is skipped

- **WHEN** a `ReconfirmAutoExpiredIntegrationEvent` lists a registration that now has `HasReconfirmed=true`
- **THEN** the handler skips that registration — no cancellation

#### Scenario: Already-cancelled registration is skipped

- **WHEN** a `ReconfirmAutoExpiredIntegrationEvent` lists a registration that is already `Cancelled`
- **THEN** the handler silently skips it — no error, no duplicate cancel

#### Scenario: Handler is idempotent on re-delivery

- **WHEN** the same `ReconfirmAutoExpiredIntegrationEvent` is delivered twice
- **THEN** each registration is cancelled at most once
