# admin-cancel-registration Specification

## Purpose

Admins can cancel an individual registration via `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel`. A `reason` field is required and must be one of the two admin-selectable values: `AttendeeRequest` or `VisaLetterDenied`. `TicketTypesRemoved` is an internal system reason and cannot be supplied by an admin caller.

## Requirements

### Requirement: Admin can cancel a registration with a reason

The system SHALL expose `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel` accepting a required `reason` field. The `reason` SHALL be limited to `AttendeeRequest` or `VisaLetterDenied`. The value `TicketTypesRemoved` is an internal system reason and SHALL NOT be accepted from an admin caller. On success the registration state transitions to `Cancelled` and the system publishes a `RegistrationCancelledIntegrationEvent`.

#### Scenario: SC001 Cancel with AttendeeRequest reason returns 204

- **GIVEN** a valid registration in state `Registered`
- **WHEN** an admin POSTs to the cancel endpoint with `reason: AttendeeRequest`
- **THEN** the response is 204 and the registration state changes to `Cancelled`

#### Scenario: SC002 Cancel with VisaLetterDenied reason returns 204

- **GIVEN** a valid registration in state `Registered`
- **WHEN** an admin POSTs to the cancel endpoint with `reason: VisaLetterDenied`
- **THEN** the response is 204 and the registration state changes to `Cancelled`

#### Scenario: SC003 Cancel an already-cancelled registration returns 409

- **GIVEN** a registration that is already in state `Cancelled`
- **WHEN** an admin POSTs to the cancel endpoint with any valid reason
- **THEN** the response is 409 Conflict

#### Scenario: SC004 Registration not found returns 404

- **GIVEN** a `registrationId` that does not exist for the given event
- **WHEN** an admin POSTs to the cancel endpoint
- **THEN** the response is 404 Not Found

#### Scenario: SC005 Missing reason returns 400

- **WHEN** an admin POSTs to the cancel endpoint without a `reason` field
- **THEN** the response is 400 Bad Request with a validation error on `reason`

#### Scenario: SC006 Invalid reason returns 400

- **WHEN** an admin POSTs to the cancel endpoint with `reason: TicketTypesRemoved` or any unknown string
- **THEN** the response is 400 Bad Request

#### Scenario: SC007 Wrong team or event returns 404

- **GIVEN** a `registrationId` that exists but belongs to a different event
- **WHEN** an admin POSTs to the cancel endpoint
- **THEN** the response is 404 Not Found

#### Scenario: SC008 CLI cancel with reason succeeds

- **GIVEN** a valid registration
- **WHEN** an admin runs `admitto event registration cancel <id> --reason AttendeeRequest` (or `VisaLetterDenied`)
- **THEN** the command succeeds and prints a confirmation message

#### Scenario: SC009 CLI cancel without reason shows error

- **WHEN** an admin runs `admitto event registration cancel <id>` without `--reason`
- **THEN** the CLI shows a validation error

#### Scenario: SC010 CLI cancel with invalid reason shows error

- **WHEN** an admin runs `admitto event registration cancel <id> --reason TicketTypesRemoved` or an unknown value
- **THEN** the CLI shows a validation error
