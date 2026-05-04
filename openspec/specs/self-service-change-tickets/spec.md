## ADDED Requirements

### Requirement: Attendee can change their ticket selection via self-service
The system SHALL expose a public endpoint `PUT /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/tickets` that allows an attendee to change the ticket-type selection on their existing `Registered` registration. The `registrationId` in the URL path serves as the bearer credential. No additional authentication token is required. The endpoint SHALL NOT inspect the `Authorization` header and SHALL NOT require a bearer token of any kind.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the given event; return HTTP 404 if not found or the registration does not belong to this event.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if `Cancelled`.
3. Load the `TicketedEvent` and verify `Status` is `Active`; reject with reason "event not active" if not.
4. Verify registration is within the registration window (`now ∈ [opensAt, closesAt)`) and `EventRegistrationPolicy.RegistrationStatus` is `Open`; reject with reason "registration not open" if not.
5. Load the `TicketCatalog` and validate the new ticket selection: no duplicates, no unknown ticket types, no cancelled ticket types, no overlapping time slots.
6. Compute capacity delta: `toRelease` = current slugs ∖ new slugs; `toClaim` = new slugs ∖ current slugs.
7. Call `catalog.Release(toRelease)`.
8. Call `catalog.Claim(toClaim, enforce: true)` — capacity is enforced for self-service (unlike admin change).
9. Call `registration.ChangeTickets(newTickets)` to update the snapshot and raise a `TicketsChangedDomainEvent`.

The `TicketsChangedDomainEvent` carries the same fields as for admin ticket changes.

#### Scenario: SC001 Successful self-service ticket change returns 200
- **GIVEN** a registration with id "reg-abc" holding ["Early Bird"] on event "devconf-2026" (Status Active, registration Open), "Workshop" has capacity 5/20 used
- **WHEN** the attendee submits `{"tickets": ["Workshop"]}` to `/events/acme/devconf-2026/registrations/reg-abc/tickets` without an Authorization header
- **THEN** the response is HTTP 200, the registration's ticket snapshot is updated to ["Workshop"], "Early Bird" capacity decreases by 1, "Workshop" capacity increases by 1

#### Scenario: SC002 Registration not found returns 404
- **WHEN** the attendee submits a change request with a registration ID that does not exist or belongs to a different event
- **THEN** the response is HTTP 404 Not Found

#### Scenario: SC003 Capacity full rejects attendee change
- **GIVEN** "Workshop" has capacity 20/20 used
- **AND** the attendee holds ["Early Bird"] and requests ["Workshop"]
- **WHEN** the attendee submits the change
- **THEN** the response is HTTP 422 with reason "ticket type at capacity"

#### Scenario: SC004 Registration window closed rejects change
- **GIVEN** the event's registration window has closed (closesAt is in the past) or registration status is not Open
- **WHEN** the attendee submits a ticket change
- **THEN** the response is HTTP 422 with reason "registration not open"

#### Scenario: SC005 Cancelled registration returns 409
- **GIVEN** a registration in state `Cancelled` with id "reg-abc"
- **WHEN** the attendee submits a ticket change
- **THEN** the response is HTTP 409 Conflict

#### Scenario: SC006 Unknown ticket type returns 422
- **WHEN** the attendee submits a ticket selection containing a slug that does not exist in the catalog
- **THEN** the response is HTTP 422 with reason "unknown ticket type"

#### Scenario: SC007 Identical ticket set is a no-op success
- **GIVEN** a registration holding ["General Admission"]
- **WHEN** the attendee submits the same selection ["General Admission"] without an Authorization header
- **THEN** the response is HTTP 200, no capacity delta occurs, and a `TicketsChangedDomainEvent` is still raised

---

### Requirement: Self-service ticket change rejects ticket types not enabled for self-service
The system SHALL reject a self-service ticket change that would add a ticket type
with `SelfServiceEnabled = false` to the registration. The check applies only to
ticket types being newly claimed (i.e. in `toClaim`, not `toRelease`). Admin ticket
changes are not subject to this check.

#### Scenario: Self-service change rejected — new ticket type not self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "vip" has `SelfServiceEnabled = false`
- **WHEN** the attendee submits a self-service change to ["vip"]
- **THEN** the response is HTTP 422 with reason "ticket type not available for self-service"

#### Scenario: Self-service change allowed when all new ticket types are self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "workshop" has `SelfServiceEnabled = true`
- **WHEN** the attendee submits a self-service change to ["workshop"]
- **THEN** the change succeeds (assuming capacity and window are valid)

