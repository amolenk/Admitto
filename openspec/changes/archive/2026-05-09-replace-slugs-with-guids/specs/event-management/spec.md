## MODIFIED Requirements

### Requirement: Organizer can create a ticketed event
The system SHALL allow organizers to request creation of a ticketed event with a
name, website URL, base URL, and start/end dates. A slug is no longer required.
Event creation remains a two-phase asynchronous flow:

1. The **Organization** module receives the request at
   `POST /admin/teams/{teamId}/events`, validates team-level preconditions
   (team not archived, request payload well-formed, end date on or after start
   date), increments the team's `PendingEventCount`, records a
   `TeamEventCreationRequest` with a new `CreationRequestId`, outboxes a
   `TicketedEventCreationRequested` integration event, and returns
   `202 Accepted` with a `Location` header pointing to the creation-status
   endpoint.
2. The **Registrations** module consumes the integration event and attempts
   to create the `TicketedEvent` aggregate. The system assigns a `EventId` (UUID)
   on creation. On success it outboxes `TicketedEventCreated`; on validation
   failure it outboxes `TicketedEventCreationRejected` carrying the
   `CreationRequestId` and a reason.

Organization handles both response events to advance team counters and to mark
the `TeamEventCreationRequest` terminal (see team-management).

#### Scenario: Successfully accept a creation request
- **WHEN** an organizer of team with ID "11111111-0000-0000-0000-000000000001" posts a creation request for an event with name "Acme Conf 2026", website "https://conf.acme.org", base URL "https://tickets.acme.org", starting 2026-06-01 and ending 2026-06-03
- **THEN** the response is `202 Accepted`, the `Location` header points to the creation-status endpoint, the team's `PendingEventCount` is incremented, and a `TicketedEventCreationRequested` event is outboxed

#### Scenario: Registrations materialises the event
- **WHEN** Registrations processes a `TicketedEventCreationRequested` for name "Acme Conf 2026" that contains no validation errors
- **THEN** a `TicketedEvent` aggregate is created with the provided details and a system-assigned UUID, its status is Active, and a `TicketedEventCreated` integration event is outboxed

#### Scenario: Reject end date before start date (synchronous)
- **WHEN** an organizer posts a creation request with start 2026-06-03 and end 2026-06-01
- **THEN** Organization rejects the request with a `400` validation error and does not increment `PendingEventCount`

#### Scenario: Reject creating an event for an archived team (synchronous)
- **WHEN** a team is archived and an organizer posts a creation request for it
- **THEN** Organization rejects the request with a `409` error because the team is archived and does not increment `PendingEventCount`

#### Scenario: Crew member cannot create events
- **WHEN** a Crew member posts a creation request
- **THEN** Organization rejects the request as unauthorized

---

### Requirement: Team member can view event details
The system SHALL allow team members with Crew role or above to view a ticketed
event's details by event ID. The `TicketedEvent` aggregate lives in the
Registrations module and the read is served from there.

#### Scenario: View event details
- **WHEN** a Crew member views the event with ID "22222222-0000-0000-0000-000000000001"
- **THEN** the event's ID, name, dates, URLs, and status are returned

#### Scenario: Non-member cannot view events
- **WHEN** a user who is not a member of the team owning the event attempts to view it
- **THEN** the request is rejected as unauthorized

---

### Requirement: Team member can list team events
The system SHALL allow team members with Crew role or above to list all events
for their team via `GET /admin/teams/{teamId}/events`. Archived events SHALL be
excluded from listings.

#### Scenario: List active events excludes archived
- **WHEN** a Crew member of team "Acme" lists events and "Acme Conf 2026" (active), "Q1 Meetup" (cancelled), and "Acme Conf 2025" (archived) exist
- **THEN** "Acme Conf 2026" and "Q1 Meetup" are returned and "Acme Conf 2025" is not included

## REMOVED Requirements

### Requirement: Reject duplicate event slug within a team (asynchronous)
**Reason**: Event slugs are removed; events are identified by system-assigned UUID.
**Migration**: Remove the `slug` field from create-event requests. The unique index on `(TeamId, Slug)` in the Registrations schema is dropped.
