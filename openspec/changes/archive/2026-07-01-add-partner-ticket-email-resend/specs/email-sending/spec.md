## ADDED Requirements

### Requirement: Ticket-confirmation resend is exposed via a Partner API endpoint

The system SHALL expose `POST /api/events/{eventSlug}/registrations/{registrationId}/ticket-email/resend` for requesting a ticket-confirmation resend from trusted Partner API clients. The endpoint SHALL require a valid active `X-Api-Key`, derive `TeamId` from the API-key principal, resolve `{eventSlug}` to a `TicketedEventId` within that team scope, and request resend work for the scoped registration.

On success, the endpoint SHALL return `202 Accepted` after the resend request has been durably recorded through the Registrations outbox. The API host SHALL NOT send SMTP inline and SHALL NOT create Email module send claims directly. The endpoint SHALL reuse the same resend command and Email module delivery pipeline as the Admin API resend action.

#### Scenario: Partner request accepts registered attendee resend

- **WHEN** a Partner API client calls `POST /api/events/devconf/registrations/{registrationId}/ticket-email/resend` with a valid active API key for the event's team and `{registrationId}` belongs to a `Registered` attendee in that event
- **THEN** the endpoint returns `202 Accepted` and durable Email work is requested for one `ticket` email through the existing resend pipeline

#### Scenario: Partner request requires API key

- **WHEN** a client calls the Partner resend endpoint without `X-Api-Key`
- **THEN** the endpoint returns `401 Unauthorized` and no Email work is created

#### Scenario: Partner request rejects invalid API key

- **WHEN** a client calls the Partner resend endpoint with an invalid or revoked `X-Api-Key`
- **THEN** the endpoint returns `401 Unauthorized` and no Email work is created

#### Scenario: Partner request is scoped to API key owner team

- **WHEN** a client calls the Partner resend endpoint for event slug `devconf` with a valid API key that belongs to a different team than that event
- **THEN** event resolution uses the API-key owner's team scope, the endpoint returns not found, and no Email work is created

#### Scenario: Partner request rejects missing registration

- **WHEN** a Partner API client calls the resend endpoint with a valid API key for the event's team but `{registrationId}` does not identify a registration in the resolved event
- **THEN** the endpoint returns not found and no Email work is created

#### Scenario: Partner request rejects non-registered attendee

- **WHEN** a Partner API client calls the resend endpoint for a registration that is not currently `Registered`
- **THEN** the endpoint rejects the request and no Email work is created

#### Scenario: Partner request is not coupled to SMTP delivery

- **WHEN** a Partner API client requests a ticket-confirmation resend and the resend request is durably recorded
- **THEN** the endpoint returns `202 Accepted` before SMTP delivery is attempted by the Worker
