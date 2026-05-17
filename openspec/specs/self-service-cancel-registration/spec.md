# Self-Service Cancel Registration Specification

## Purpose

Attendees can cancel their own event registrations through a public endpoint using their registration ID as the bearer credential. The system enforces that cancellations are only permitted before the event has started.

## Requirements

### Requirement: Attendee can self-cancel their registration
The system SHALL expose a public endpoint `POST /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/cancel` that allows an attendee to cancel their own registration. The `registrationId` in the URL path serves as the bearer credential — possession of the ID proves authorization. No additional authentication token is required. The endpoint SHALL NOT inspect the `Authorization` header and SHALL NOT require a bearer token of any kind.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the given event; return HTTP 404 if not found or the registration does not belong to this event.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if already `Cancelled`.
3. Verify `now < event.StartsAt`; return HTTP 409 with reason "event has already started" if the event has begun.
4. Transition the registration to `Cancelled` with `CancellationReason.AttendeeRequest`.
5. Release ticket capacity on the `TicketCatalog`.
6. Raise a `RegistrationCancelledIntegrationEvent` (same as admin cancel).

No reason field is accepted from the attendee; the reason is always recorded as `AttendeeRequest`.

#### Scenario: Successful self-service cancellation returns 204
- **GIVEN** a registration in state `Registered` with id "reg-abc" on event "devconf-2026" whose `StartsAt` is in the future
- **WHEN** the attendee posts to `/events/acme/devconf-2026/registrations/reg-abc/cancel` without an Authorization header
- **THEN** the response is HTTP 204, the registration transitions to `Cancelled` with reason `AttendeeRequest`, and ticket capacity is released

#### Scenario: Registration not found returns 404
- **WHEN** the attendee posts to the cancel endpoint with a registration ID that does not exist or belongs to a different event
- **THEN** the response is HTTP 404 Not Found

#### Scenario: Already cancelled registration returns 409
- **GIVEN** a registration already in state `Cancelled` with id "reg-abc"
- **WHEN** the attendee posts to the cancel endpoint
- **THEN** the response is HTTP 409 Conflict

#### Scenario: Cancellation rejected after event has started
- **GIVEN** a registration in state `Registered` and the event's `StartsAt` is in the past
- **WHEN** the attendee posts to the cancel endpoint
- **THEN** the response is HTTP 409 Conflict with a reason indicating the event has already started
