## ADDED Requirements

### Requirement: Admin can request a ticket-confirmation resend for a registered attendee

The system SHALL expose an admin-only action that requests a new ticket-confirmation email for an existing `Registered` registration scoped to the supplied team and ticketed event. The action SHALL reuse the built-in `TicketConfirmation`/`ticket` email template and the existing Email module send pipeline. The API host SHALL NOT send SMTP inline and SHALL NOT create Email module send claims directly; it SHALL durably enqueue a Registrations integration event for the Email-capable Worker. The Worker SHALL create or reuse the Email module send claim and enqueue delivery work.

The resend SHALL use current registration facts from the Registrations module for recipient email address, attendee name, registration id, and ticket names. The resend SHALL use Email-owned event rendering context for reusable event/team facts, branding, and public links. Email SHALL NOT query Registrations DbContext directly.

Each accepted resend request SHALL use an idempotency key that is distinct from the original registration-triggered key, so the resend is not suppressed by a prior successful `attendee-registered:{registrationId}:{registeredAt}` ticket email. Redelivery or repeated handling of the same resend request SHALL remain idempotent through the existing EmailLog send-claim mechanism.

#### Scenario: Admin requests resend for registered attendee

- **WHEN** an authorized team member requests a ticket-confirmation resend for registration `R1` that is `Registered` in event `E1`
- **THEN** the system creates durable Email work for one `ticket` email to `R1`'s attendee email address using `R1`'s attendee name and ticket names

#### Scenario: Resend is allowed after original ticket email was sent

- **WHEN** registration `R1` already has a terminal `Sent` email log row with idempotency key `attendee-registered:{registrationId}:{registeredAt}`
- **THEN** a resend request for `R1` creates a separate `ticket` email log claim with a resend-specific idempotency key

#### Scenario: Duplicate processing of same resend request is idempotent

- **WHEN** the same accepted resend request is processed more than once with the same resend idempotency key
- **THEN** the EmailLog send claim prevents duplicate log rows and terminal rows prevent another SMTP send for that request

#### Scenario: Resend for missing registration is rejected

- **WHEN** an authorized team member requests a ticket-confirmation resend for a registration id that does not exist in the supplied team and event scope
- **THEN** the system returns not found and no Email work is created

#### Scenario: Resend for non-registered registration is rejected

- **WHEN** an authorized team member requests a ticket-confirmation resend for a registration that is not currently `Registered`
- **THEN** the system rejects the request and no Email work is created

#### Scenario: API response is not coupled to SMTP delivery

- **WHEN** an authorized team member requests a ticket-confirmation resend and the Email work is durably requested
- **THEN** the Admin API returns an accepted response before SMTP delivery is attempted by the Worker

### Requirement: Ticket-confirmation resend is exposed via an Admin API endpoint

The system SHALL expose `POST /admin/teams/{teamId}/events/{eventId}/registrations/{registrationId}/ticket-email/resend` for requesting a ticket-confirmation resend. The endpoint SHALL be protected by the same authenticated team-membership authorization and route-scope resolution used by registration detail and attendee email history endpoints. On success, the endpoint SHALL return `202 Accepted`.

#### Scenario: Endpoint requires authentication

- **WHEN** an unauthenticated client calls the ticket-confirmation resend endpoint
- **THEN** the endpoint responds with `401 Unauthorized` and no Email work is created

#### Scenario: Endpoint rejects non-member

- **WHEN** an authenticated user without membership in the route team calls the ticket-confirmation resend endpoint
- **THEN** the endpoint responds with `403 Forbidden` and no Email work is created

#### Scenario: Endpoint accepts organizer request

- **WHEN** an authenticated organizer for the route team calls the ticket-confirmation resend endpoint for a registered attendee
- **THEN** the endpoint responds with `202 Accepted` and the resend appears in attendee email history through the normal email log fields
