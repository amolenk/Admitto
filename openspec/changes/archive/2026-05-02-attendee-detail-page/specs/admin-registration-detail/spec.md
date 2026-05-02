# Admin Registration Detail Specification

## Purpose

This capability covers fetching the full details of a single registration for admin use. It provides the query, handler, DTO, and admin HTTP endpoint that surface all persisted data for one registration: attendee identity, tickets, additional detail fields, status, reconfirmation, and cancellation metadata.

## Requirements

### Requirement: Admin can retrieve full details of a single registration

The system SHALL expose a query that returns the complete data for one registration, identified by its ID scoped to a team and event.

The returned data SHALL include:
- `id`: registration GUID
- `email`: attendee email address
- `firstName`: attendee first name
- `lastName`: attendee last name
- `status`: `Registered` or `Cancelled`
- `registeredAt`: timestamp when the registration was created
- `hasReconfirmed`: boolean
- `reconfirmedAt`: nullable timestamp (present when `hasReconfirmed` is true)
- `cancellationReason`: nullable string (present when `status` is `Cancelled`)
- `tickets`: list of `{ slug, name }` pairs from the registration's stored snapshot
- `additionalDetails`: dictionary of string-to-string pairs (may be empty)
- `activities`: ordered list (oldest first) of `ActivityLog` entries for this registration, each containing: `activityType` (`Registered`, `Reconfirmed`, or `Cancelled`), `occurredAt` (timestamp), and `metadata` (nullable string, e.g. cancellation reason)

#### Scenario: SC001 Returns full details for an existing registration

- **GIVEN** a registration exists for the requested event with `firstName="Alice"`, `lastName="Smith"`, one ticket "Early bird", `additionalDetails={"dietary":"vegan"}`, and `hasReconfirmed=false`
- **WHEN** an admin requests the registration by ID
- **THEN** the system returns all fields including the additional details, the empty reconfirmedAt, and an `activities` list containing at least a single `Registered` entry

#### Scenario: SC002 Returns reconfirmation timestamp when attendee has reconfirmed

- **GIVEN** a registration with `hasReconfirmed=true`, `reconfirmedAt="2026-05-01T10:00Z"`, and a corresponding `Reconfirmed` ActivityLog entry
- **WHEN** an admin requests the registration by ID
- **THEN** the returned DTO carries `hasReconfirmed=true`, `reconfirmedAt="2026-05-01T10:00Z"`, and the `activities` list includes a `Reconfirmed` entry at the same timestamp

#### Scenario: SC003 Returns cancellation reason when registration is cancelled

- **GIVEN** a registration with `status=Cancelled` and `cancellationReason=AttendeeRequest`
- **WHEN** an admin requests the registration by ID
- **THEN** the returned DTO carries `status=Cancelled` and `cancellationReason="AttendeeRequest"`

#### Scenario: SC004 Returns empty additional details when none were provided

- **GIVEN** a registration that was created without additional detail fields
- **WHEN** an admin requests the registration by ID
- **THEN** `additionalDetails` is an empty object

#### Scenario: SC005 Returns multiple tickets

- **GIVEN** a registration with two tickets ("Early bird" and "Workshop")
- **WHEN** an admin requests the registration by ID
- **THEN** the `tickets` array contains both entries with correct slugs and names

#### Scenario: SC006 Returns 404 when registration ID does not exist

- **WHEN** an admin requests a registration ID that does not exist in the given event
- **THEN** the system returns a not-found result

#### Scenario: SC007 Returns 404 when team slug is unknown

- **WHEN** an admin requests the endpoint with an unknown team slug
- **THEN** the system returns a not-found result

#### Scenario: SC008 Returns 404 when event slug is unknown

- **WHEN** an admin requests the endpoint with a known team but unknown event slug
- **THEN** the system returns a not-found result

### Requirement: Registration detail is exposed via an admin HTTP endpoint

The system SHALL expose `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}`, restricted to authenticated members of the team.

#### Scenario: SC009 Authenticated organizer receives registration details

- **GIVEN** a user is a member of the team with the Organizer role
- **WHEN** they call `GET /admin/teams/acme/events/devconf/registrations/{id}`
- **THEN** the system returns 200 with the full registration detail payload

#### Scenario: SC010 Non-member of the team is forbidden

- **GIVEN** a user is authenticated but not a member of the team
- **WHEN** they call the endpoint
- **THEN** the system returns 403
