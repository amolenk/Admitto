## MODIFIED Requirements

### Requirement: Attendee can change their ticket selection via self-service
The system SHALL expose a public endpoint `PUT /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/tickets` that allows an attendee to change the ticket-type selection on their existing `Registered` registration. The `registrationId` in the URL path serves as the bearer credential. No additional authentication token is required. The endpoint SHALL NOT inspect the `Authorization` header and SHALL NOT require a bearer token of any kind.

The request MAY include a waitlist coupon code. When present, the coupon SHALL act as a capacity grant for the offered ticket type. The final requested ticket selection SHALL include the offered ticket type. The offered ticket type SHALL bypass capacity and WaitlistMode checks, but the final registration ticket set SHALL still satisfy normal duplicate, unknown, cancelled, and overlapping time-slot validation. Any newly added ticket type not covered by the waitlist coupon SHALL use normal self-service capacity enforcement.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the given event; return HTTP 404 if not found or the registration does not belong to this event.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if `Cancelled`.
3. Load the `TicketedEvent` and verify `Status` is `Active`; reject with reason "event not active" if not.
4. Verify registration is within the registration window (`now in [opensAt, closesAt)`) and `EventRegistrationPolicy.RegistrationStatus` is `Open`; reject with reason "registration not open" if not.
5. If a waitlist coupon code is supplied, validate that the coupon is valid, unexpired, unredeemed, issued for the same event, issued to the registration email, and sourced from waitlist.
6. Load the `TicketCatalog` and validate the new ticket selection: no duplicates, no unknown ticket types, no cancelled ticket types, no overlapping time slots.
7. Compute capacity delta: `toRelease` = current ids minus new ids; `toClaim` = new ids minus current ids.
8. Call `catalog.Release(toRelease)`.
9. Claim added capacity: the waitlist coupon's offered ticket type is claimed with coupon bypass semantics; all other `toClaim` ticket types are claimed with `enforce: true`.
10. Mark the waitlist coupon redeemed when one was supplied.
11. Call `registration.ChangeTickets(newTickets)` to update the snapshot and raise a `TicketsChangedDomainEvent`.

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
- **AND** the attendee holds ["Early Bird"] and requests ["Workshop"] without a waitlist coupon for Workshop
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

#### Scenario: SC008 Waitlist coupon changes existing registration
- **GIVEN** a registration holding ["Workshop A"] and a valid waitlist coupon for overlapping "Workshop B" issued to the registration email
- **WHEN** the attendee submits a ticket change with `ticketTypeIds = [Workshop B]` and the waitlist coupon code
- **THEN** the registration is changed from "Workshop A" to "Workshop B", the coupon is marked redeemed, and the offered ticket bypasses capacity and WaitlistMode checks

#### Scenario: SC009 Waitlist coupon rejected when final selection omits offered ticket
- **GIVEN** a valid waitlist coupon for "Workshop B"
- **WHEN** the attendee submits a ticket change with the coupon code but a final ticket selection that does not include "Workshop B"
- **THEN** the request is rejected and the coupon remains unredeemed

#### Scenario: SC010 Waitlist coupon rejected when final selection still overlaps
- **GIVEN** a registration holding ["Workshop A"] and a valid waitlist coupon for overlapping "Workshop B"
- **WHEN** the attendee submits a ticket change with `ticketTypeIds = [Workshop A, Workshop B]` and the waitlist coupon code
- **THEN** the request is rejected with reason "overlapping time slots" and the coupon remains unredeemed
