## MODIFIED Requirements

### Requirement: Attendee can change their ticket selection via self-service
The system SHALL expose an API-key-protected Partner API endpoint `PUT /api/events/{eventSlug}/registrations/{registrationId}` that allows an attendee to update the attendee-editable state of an existing `Registered` registration in one atomic request. The endpoint SHALL replace the prior ticket-only endpoint contract `PUT /api/events/{eventSlug}/registrations/{registrationId}/tickets`; the ticket-only endpoint SHALL NOT remain part of the public contract.

The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventSlug}` and `{registrationId}` from the URL path. `{eventSlug}` SHALL be resolved to the event's internal ID within the API-key owner's team scope. The `registrationId` in the URL path serves as the attendee bearer credential for the registration itself, while the required `X-Api-Key` authorizes the external event site/team integration. The endpoint SHALL NOT require an `Authorization` bearer token of any kind.

The request body SHALL carry the attendee's full desired editable registration state:
- `firstName`: required attendee first name.
- `lastName`: required attendee last name.
- `ticketTypeIds`: required array of final registered ticket type IDs.
- `additionalDetails`: optional map of additional-detail key/value strings; omitted or `null` SHALL be treated as an empty map.
- `waitlistCouponCode`: optional waitlist coupon code.

The handler SHALL:
1. Look up the `Registration` by `registrationId` and verify it belongs to the resolved event and API-key team; return HTTP 404 if not found or the registration does not belong to this event/team scope.
2. Verify the registration `Status` is `Registered`; return HTTP 409 if `Cancelled`.
3. Load the `TicketedEvent` and verify `Status` is `Active`; reject with reason "event not active" if not.
4. Verify registration is within the registration window (`now ∈ [opensAt, closesAt)`); reject with reason "registration not open" if not.
5. Validate `firstName` and `lastName` using the same value-object rules as registration creation.
6. Validate `additionalDetails` against the event's current `AdditionalDetailSchema`; reject unknown keys and values exceeding the configured max length, accept missing keys, and preserve empty-string values.
7. If a waitlist coupon code is supplied, validate that the coupon is valid, unexpired, unredeemed, issued for the same event, issued to the registration email, and sourced from waitlist.
8. Load the `TicketCatalog` and validate the final ticket selection: no duplicates, no unknown ticket types, no cancelled ticket types, no overlapping time slots.
9. Compute capacity delta: `toRelease` = current ids minus final ids; `toClaim` = final ids minus current ids.
10. Call `catalog.Release(toRelease)`.
11. Claim added capacity: the waitlist coupon's offered ticket type is claimed with coupon bypass semantics; all other `toClaim` ticket types are claimed with `enforce: true`.
12. Mark the waitlist coupon redeemed when one was supplied.
13. Persist the registration's first name, last name, additional details, and final ticket snapshot in the same unit of work.

When the final ticket selection differs from the current ticket selection, the system SHALL raise a `TicketsChangedDomainEvent` carrying the same fields as for admin ticket changes. When only first name, last name, or additional details change, the system SHALL NOT raise a ticket-change event or send a ticket-change confirmation email.

#### Scenario: Successful self-service registration update returns 200
- **GIVEN** a registration with id "reg-abc" holding ["Early Bird"] on event "devconf-2026" with Status Active and registration open, and "Workshop" has capacity 5/20 used
- **WHEN** the attendee submits `firstName = "Alice"`, `lastName = "Anderson"`, `ticketTypeIds = [Workshop]`, and `additionalDetails = { "dietary": "vegan" }` to `/api/events/{eventSlug}/registrations/reg-abc` with a valid API key for the event's team and without an Authorization header
- **THEN** the response is HTTP 200, the registration's first name, last name, additional details, and ticket snapshot are updated atomically, "Early Bird" capacity decreases by 1, and "Workshop" capacity increases by 1

#### Scenario: Registration not found returns 404
- **WHEN** the attendee submits an update request with a registration ID that does not exist, belongs to a different event, or belongs to a different team than the API key owner
- **THEN** the response is HTTP 404 Not Found

#### Scenario: Capacity full rejects attendee update
- **GIVEN** "Workshop" has capacity 20/20 used
- **AND** the attendee holds ["Early Bird"] and requests ["Workshop"] without a waitlist coupon for Workshop
- **WHEN** the attendee submits the registration update
- **THEN** the response is HTTP 422 with reason "ticket type at capacity" and no attendee details or ticket capacity changes are persisted

#### Scenario: Registration window closed rejects update
- **GIVEN** the event's registration window has closed or has not opened
- **WHEN** the attendee submits a registration update
- **THEN** the response is HTTP 422 with reason "registration not open" and no attendee details or ticket capacity changes are persisted

#### Scenario: Cancelled registration returns 409
- **GIVEN** a registration in state `Cancelled` with id "reg-abc"
- **WHEN** the attendee submits a registration update
- **THEN** the response is HTTP 409 Conflict

#### Scenario: Unknown ticket type returns 422
- **WHEN** the attendee submits a final ticket selection containing an ID that does not exist in the catalog
- **THEN** the response is HTTP 422 with reason "unknown ticket type" and no attendee details are persisted

#### Scenario: Details-only update succeeds without ticket confirmation
- **GIVEN** a registration holding ["General Admission"] with first name "Alice", last name "Test", and no additional details
- **WHEN** the attendee submits `firstName = "Alice"`, `lastName = "Anderson"`, `ticketTypeIds = [General Admission]`, and `additionalDetails = { "dietary": "vegan" }`
- **THEN** the response is HTTP 200, the registration's last name and additional details are updated, no capacity delta occurs, and no `TicketsChangedDomainEvent` is raised

#### Scenario: Missing first name returns validation problem
- **WHEN** the attendee submits a registration update without `firstName`
- **THEN** the response is HTTP 400 with a validation error on `firstName`

#### Scenario: Missing last name returns validation problem
- **WHEN** the attendee submits a registration update without `lastName`
- **THEN** the response is HTTP 400 with a validation error on `lastName`

#### Scenario: Missing ticket selection returns validation problem
- **WHEN** the attendee submits a registration update without `ticketTypeIds`
- **THEN** the response is HTTP 400 with a validation error on `ticketTypeIds`

#### Scenario: Additional detail unknown key returns validation error
- **GIVEN** the event's additional-detail schema does not declare key `shoesize`
- **WHEN** the attendee submits `additionalDetails = { "shoesize": "44" }`
- **THEN** the response is HTTP 422 with reason "additional detail key not in schema" and no attendee details or ticket capacity changes are persisted

#### Scenario: Additional detail value too long returns validation error
- **GIVEN** the event's additional-detail schema declares key `tshirt` with max length 5
- **WHEN** the attendee submits `additionalDetails = { "tshirt": "XXXXL-extra-long" }`
- **THEN** the response is HTTP 422 with reason "additional detail value too long" and no attendee details or ticket capacity changes are persisted

#### Scenario: Omitted additional details replace with empty map
- **GIVEN** a registration currently has `additionalDetails = { "dietary": "vegan" }`
- **WHEN** the attendee submits a valid registration update with `additionalDetails` omitted or `null`
- **THEN** the registration's additional details are replaced with an empty map

#### Scenario: Missing API key is rejected
- **WHEN** the attendee submits to the registration update endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the registration update handler

#### Scenario: Waitlist coupon changes existing registration
- **GIVEN** a registration holding ["Workshop A"] and a valid waitlist coupon for overlapping "Workshop B" issued to the registration email
- **WHEN** the attendee submits a registration update with `ticketTypeIds = [Workshop B]` and the waitlist coupon code
- **THEN** the registration is changed from "Workshop A" to "Workshop B", the coupon is marked redeemed, and the offered ticket bypasses capacity and WaitlistMode checks

#### Scenario: Waitlist coupon rejected when final selection omits offered ticket
- **GIVEN** a valid waitlist coupon for "Workshop B"
- **WHEN** the attendee submits a registration update with the coupon code but a final ticket selection that does not include "Workshop B"
- **THEN** the request is rejected and the coupon remains unredeemed

#### Scenario: Waitlist coupon rejected when final selection still overlaps
- **GIVEN** a registration holding ["Workshop A"] and a valid waitlist coupon for overlapping "Workshop B"
- **WHEN** the attendee submits a registration update with `ticketTypeIds = [Workshop A, Workshop B]` and the waitlist coupon code
- **THEN** the request is rejected with reason "overlapping time slots" and the coupon remains unredeemed

### Requirement: Self-service ticket change rejects ticket types not enabled for self-service
The system SHALL reject a self-service registration update that would add a ticket type with `SelfServiceEnabled = false` to the registration. The check applies only to ticket types being newly claimed (i.e. in `toClaim`, not `toRelease`). Admin ticket changes are not subject to this check.

#### Scenario: Self-service update rejected when new ticket type is not self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "vip" has `SelfServiceEnabled = false`
- **WHEN** the attendee submits a registration update with `ticketTypeIds = [vip]`
- **THEN** the response is HTTP 422 with reason "ticket type not available for self-service" and no attendee details or ticket capacity changes are persisted

#### Scenario: Self-service update allowed when all new ticket types are self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "workshop" has `SelfServiceEnabled = true`
- **WHEN** the attendee submits a registration update with `ticketTypeIds = [workshop]`
- **THEN** the change succeeds assuming capacity, event status, registration window, attendee details, and additional details are valid
