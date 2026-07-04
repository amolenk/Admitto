## MODIFIED Requirements

### Requirement: Attendee can self-cancel their registration
The system SHALL expose an API-key-protected public endpoint `POST /api/events/{eventId}/registrations/{registrationId}/cancel` that allows an attendee to cancel their own registration. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventId}` and `{registrationId}` from the URL path. The `registrationId` in the URL path serves as the attendee bearer credential for the registration itself, while the required `X-Api-Key` authorizes the external event site/team integration. The endpoint SHALL NOT require an `Authorization` bearer token of any kind.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the given event and API-key team; return HTTP 404 if not found or the registration does not belong to this event/team scope.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if already `Cancelled`.
3. Verify `now < event.StartsAt`; return HTTP 409 with reason "event has already started" if the event has begun.
4. Transition the registration to `Cancelled` with `CancellationReason.AttendeeRequest`.
5. Release ticket capacity on the `TicketCatalog`.
6. Raise a `RegistrationCancelledIntegrationEvent` (same as admin cancel).

No reason field is accepted from the attendee; the reason is always recorded as `AttendeeRequest`.

#### Scenario: Successful self-service cancellation returns 204
- **GIVEN** a registration in state `Registered` with id "reg-abc" on a future event
- **WHEN** the attendee posts to `/api/events/{eventId}/registrations/reg-abc/cancel` with a valid API key for the event's team and without an Authorization header
- **THEN** the response is HTTP 204, the registration transitions to `Cancelled` with reason `AttendeeRequest`, and ticket capacity is released

#### Scenario: Registration not found returns 404
- **WHEN** the attendee posts to the cancel endpoint with a registration ID that does not exist, belongs to a different event, or belongs to a different team than the API key owner
- **THEN** the response is HTTP 404 Not Found

#### Scenario: Already cancelled registration returns 409
- **GIVEN** a registration already in state `Cancelled` with id "reg-abc"
- **WHEN** the attendee posts to the cancel endpoint
- **THEN** the response is HTTP 409 Conflict

#### Scenario: Cancellation rejected after event has started
- **GIVEN** a registration in state `Registered` and the event's `StartsAt` is in the past
- **WHEN** the attendee posts to the cancel endpoint
- **THEN** the response is HTTP 409 Conflict with a reason indicating the event has already started

#### Scenario: Missing API key is rejected
- **WHEN** the attendee posts to the cancel endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the cancellation handler
