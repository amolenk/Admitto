## ADDED Requirements

### Requirement: Successful attendee registration publishes an integration event
The Registrations module SHALL publish an `AttendeeRegisteredIntegrationEvent` whenever an attendee registration is successfully persisted, regardless of whether the registration originated from self-service or an admin flow. The integration event SHALL be derived from the existing `AttendeeRegisteredDomainEvent` via the module's `MessagePolicy` and SHALL be enqueued through the existing outbox so that delivery is at-least-once and durable.

The integration event SHALL carry at minimum: `TeamId`, `TicketedEventId`, `RegistrationId`, the recipient email address, and the recipient's display name. It SHALL be defined in `Admitto.Module.Registrations.Contracts.IntegrationEvents`.

#### Scenario: Self-service registration publishes the event
- **WHEN** an attendee successfully self-registers for event "DevConf" as "alice@example.com"
- **THEN** an `AttendeeRegisteredIntegrationEvent` is enqueued in the Registrations module's outbox containing `TicketedEventId`, the new `RegistrationId`, recipient="alice@example.com", and the team id

#### Scenario: Failed registration does not publish the event
- **WHEN** a registration attempt is rejected (capacity full, window closed, domain mismatch, etc.)
- **THEN** no `AttendeeRegisteredIntegrationEvent` is enqueued

#### Scenario: Event delivery is at-least-once via the existing outbox + queue
- **WHEN** a registration succeeds and the event is enqueued
- **THEN** the event is delivered through the same outbox + queue infrastructure as other Registrations integration events, with the same retry semantics
