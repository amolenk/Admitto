# Change Attendee Tickets Specification

## Purpose

Allow authorized team members to replace the ticket-type selection on an existing registration while preserving registration history, ticket capacity accounting, and attendee email notifications.

## Requirements

### Requirement: Admin can change the ticket-type selection on an existing registration

The system SHALL provide an admin-only command and HTTP endpoint (`PUT /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/tickets`) that replaces the ticket-type selection on an existing `Registered` registration.

The handler SHALL:
1. Load the `Registration` and verify its `Status` is `Registered`; a `Cancelled` registration SHALL be rejected with reason "registration is cancelled".
2. Load the `TicketedEvent` and verify `Status` is `Active`; otherwise reject with "event not active".
3. Load the `TicketCatalog` and validate the new ticket selection using the same rules as admin-add: no duplicates, no unknown ticket types, no cancelled ticket types, no overlapping time slots.
4. Compute the capacity delta: `toRelease` = current slugs ∖ new slugs; `toClaim` = new slugs ∖ current slugs.
5. Call `catalog.Release(toRelease)` to free capacity for removed tickets.
6. Call `catalog.Claim(toClaim, enforce: false)` to record capacity for added tickets (unenforced, matching admin-add behaviour).
7. Call `registration.ChangeTickets(newTickets)` to update the snapshot and raise a `TicketsChangedDomainEvent`.

The `TicketsChangedDomainEvent` SHALL carry: `teamId`, `ticketedEventId`, `registrationId`, `recipientEmail`, `firstName`, `lastName`, `oldTickets` (list of `{slug, name}`), `newTickets` (list of `{slug, name}`), and `changedAt`.

#### Scenario: SC001 Admin successfully changes tickets on an active registration

- **GIVEN** a registration with status `Registered` holding ticket "Early Bird" on event "DevConf" (Status Active), with ticket catalog having "Early Bird" (50/100 used) and "Workshop" (10/20 used)
- **WHEN** an admin changes the tickets to ["Workshop"]
- **THEN** the registration's ticket snapshot is updated to ["Workshop"], "Early Bird" `UsedCapacity` decreases to 49, "Workshop" `UsedCapacity` increases to 11, and a `TicketsChangedDomainEvent` is raised

#### Scenario: SC002 Sold-out event does not block changing already-held tickets

- **GIVEN** a registration holding "General Admission" (UsedCapacity 100/100 — sold out) on a fully sold-out event
- **WHEN** an admin changes the tickets from ["General Admission"] to ["General Admission", "Workshop"] where "Workshop" has capacity 1/1 used
- **THEN** the change is applied; "Workshop" UsedCapacity becomes 2 (unenforced admin claim)

#### Scenario: SC003 Changing to identical set is a no-op success

- **GIVEN** a registration holding ["Early Bird"]
- **WHEN** an admin submits the same selection ["Early Bird"]
- **THEN** the registration is unchanged, no capacity delta occurs, a `TicketsChangedDomainEvent` is still raised (to allow timeline/email), and the endpoint returns 200

#### Scenario: SC004 Rejected — registration is cancelled

- **GIVEN** a registration with status `Cancelled`
- **WHEN** an admin attempts to change its tickets
- **THEN** the request is rejected with reason "registration is cancelled"

#### Scenario: SC005 Rejected — event not active

- **GIVEN** a registration whose event has `TicketedEvent.Status` Cancelled
- **WHEN** an admin attempts to change tickets
- **THEN** the request is rejected with reason "event not active"

#### Scenario: SC006 Rejected — duplicate ticket types in new selection

- **WHEN** an admin submits a new selection containing "Workshop" twice
- **THEN** the request is rejected with reason "duplicate ticket types"

#### Scenario: SC007 Rejected — unknown ticket type in new selection

- **WHEN** an admin submits a selection containing a slug that does not exist in the event's ticket catalog
- **THEN** the request is rejected with reason "unknown ticket types"

#### Scenario: SC008 Rejected — cancelled ticket type in new selection

- **WHEN** an admin submits a selection that includes a ticket type whose status is Cancelled
- **THEN** the request is rejected with reason "cancelled ticket types"

#### Scenario: SC009 Rejected — overlapping time slots in new selection

- **WHEN** an admin selects two ticket types that share an overlapping time slot
- **THEN** the request is rejected with reason "overlapping time slots"

#### Scenario: SC010 Rejected — registration not found

- **WHEN** an admin supplies a registrationId that does not exist scoped to the team and event
- **THEN** the endpoint returns 404

---

### Requirement: Ticket change triggers a confirmation email to the attendee

Upon a successful ticket change, the Email module SHALL send a confirmation email to the attendee using the `ticket` template type.

The integration event handler SHALL use idempotency key `tickets-changed:{registrationId}:{changedAt-unix-ms}` so that the email is sent exactly once per change operation.

The email SHALL be sent with the same `ticket_types` parameter used by the initial-registration email, listing the new ticket type names.

#### Scenario: SC011 Attendee receives confirmation email after ticket change

- **GIVEN** a successful ticket change for "alice@example.com" whose new selection is ["Workshop", "Dinner"]
- **WHEN** the `AttendeeTicketsChangedIntegrationEvent` is processed by the Email module
- **THEN** a confirmation email of type `ticket` is sent to "alice@example.com" listing "Workshop" and "Dinner"

#### Scenario: SC012 Email is not re-sent on duplicate event delivery

- **GIVEN** the integration event for a ticket change has already been processed
- **WHEN** the same event is delivered again
- **THEN** no duplicate email is sent (idempotency key already present in `email_log`)

---

### Requirement: Ticket change endpoint is exposed via admin HTTP

The system SHALL expose `PUT /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/tickets`, restricted to authenticated members of the team.

The request body SHALL carry: `ticketTypeSlugs` (non-empty array of strings), `version` (integer — the registration's optimistic concurrency token).

The response SHALL return 200 on success.

#### Scenario: SC013 Authenticated organizer can change tickets

- **GIVEN** a user is a member of the team with the Organizer role
- **WHEN** they call the endpoint with a valid new selection
- **THEN** the system returns 200

#### Scenario: SC014 Non-member of the team is forbidden

- **GIVEN** a user is authenticated but not a member of the team
- **WHEN** they call the endpoint
- **THEN** the system returns 403
