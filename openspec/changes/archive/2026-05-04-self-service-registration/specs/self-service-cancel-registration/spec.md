## ADDED Requirements

### Requirement: Attendee can self-cancel their registration
The system SHALL expose a public endpoint `POST /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/cancel` that allows an attendee to cancel their own registration. The `registrationId` in the URL path serves as the bearer credential — possession of the ID proves authorization. No additional authentication token is required.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the given event; return HTTP 404 if not found or the registration does not belong to this event.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if already `Cancelled`.
3. Transition the registration to `Cancelled` with `CancellationReason.AttendeeRequest`.
4. Release ticket capacity on the `TicketCatalog`.
5. Raise a `RegistrationCancelledIntegrationEvent` (same as admin cancel).

No reason field is accepted from the attendee; the reason is always recorded as `AttendeeRequest`.

#### Scenario: SC001 Successful self-service cancellation returns 204
- **GIVEN** a registration in state `Registered` with id "reg-abc" on event "devconf-2026"
- **WHEN** the attendee posts to `/events/acme/devconf-2026/registrations/reg-abc/cancel`
- **THEN** the response is HTTP 204, the registration transitions to `Cancelled` with reason `AttendeeRequest`, and ticket capacity is released

#### Scenario: SC002 Registration not found returns 404
- **WHEN** the attendee posts to the cancel endpoint with a registration ID that does not exist or belongs to a different event
- **THEN** the response is HTTP 404 Not Found

#### Scenario: SC003 Already cancelled registration returns 409
- **GIVEN** a registration already in state `Cancelled` with id "reg-abc"
- **WHEN** the attendee posts to the cancel endpoint
- **THEN** the response is HTTP 409 Conflict
