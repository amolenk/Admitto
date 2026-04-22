## Purpose

Organizers create and manage ticketed events within their teams. The `TicketedEvent`
aggregate lives in the Registrations module and owns the event's lifecycle status
along with its registration, cancellation, and reconfirm policies. Event creation is
asynchronous: the Organization module accepts the request and tracks pending creations
on the `Team` aggregate; the Registrations module materialises the event and publishes
lifecycle integration events back to Organization.

## Requirements

### Requirement: Organizer can create a ticketed event
The system SHALL allow organizers to request creation of a ticketed event with a
slug, name, website URL, base URL, and start/end dates. Event creation is a
two-phase asynchronous flow:

1. The **Organization** module receives the request at
   `POST /admin/teams/{teamSlug}/events`, validates team-level preconditions
   (team not archived, request payload well-formed, end date on or after start
   date), increments the team's `PendingEventCount`, records a
   `TeamEventCreationRequest` with a new `CreationRequestId`, outboxes a
   `TicketedEventCreationRequested` integration event, and returns
   `202 Accepted` with a `Location` header pointing to the creation-status
   endpoint.
2. The **Registrations** module consumes the integration event and attempts
   to create the `TicketedEvent` aggregate. Event slugs SHALL be unique within a
   team, enforced by a unique index on `(TeamId, Slug)` in the Registrations
   schema. On success it outboxes `TicketedEventCreated`; on slug conflict or
   any other validation failure it outboxes `TicketedEventCreationRejected`
   carrying the `CreationRequestId` and a reason.

Organization handles both response events to advance team counters and to mark
the `TeamEventCreationRequest` terminal (see team-management).

#### Scenario: Successfully accept a creation request
- **WHEN** an organizer of team "acme" posts a creation request for an event with slug "conf-2026", name "Acme Conf 2026", website "https://conf.acme.org", base URL "https://tickets.acme.org", starting 2026-06-01 and ending 2026-06-03
- **THEN** the response is `202 Accepted`, the `Location` header points to the creation-status endpoint, the team's `PendingEventCount` is incremented, and a `TicketedEventCreationRequested` event is outboxed

#### Scenario: Registrations materialises the event
- **WHEN** Registrations processes a `TicketedEventCreationRequested` for slug "conf-2026" that does not conflict
- **THEN** a `TicketedEvent` aggregate is created with the provided details, its status is Active, and a `TicketedEventCreated` integration event is outboxed

#### Scenario: Reject duplicate event slug within a team (asynchronous)
- **WHEN** team "acme" already has a `TicketedEvent` with slug "conf-2026" and Registrations processes a `TicketedEventCreationRequested` for slug "conf-2026"
- **THEN** no new `TicketedEvent` is created and a `TicketedEventCreationRejected` event is outboxed with reason "duplicate slug"

#### Scenario: Reject end date before start date (synchronous)
- **WHEN** an organizer posts a creation request with start 2026-06-03 and end 2026-06-01
- **THEN** Organization rejects the request with a `400` validation error and does not increment `PendingEventCount`

#### Scenario: Reject creating an event for an archived team (synchronous)
- **WHEN** a team is archived and an organizer posts a creation request for it
- **THEN** Organization rejects the request with a `409` error because the team is archived and does not increment `PendingEventCount`

#### Scenario: Crew member cannot create events
- **WHEN** a Crew member of team "acme" posts a creation request
- **THEN** Organization rejects the request as unauthorized

---

### Requirement: Team member can view event details
The system SHALL allow team members with Crew role or above to view a ticketed
event's details by event slug. The `TicketedEvent` aggregate lives in the
Registrations module and the read is served from there. Ticket types continue
to be served separately by the ticket-catalog read paths.

#### Scenario: View event details
- **WHEN** a Crew member of team "acme" views event "conf-2026"
- **THEN** the event's name, dates, URLs, and status are returned

#### Scenario: Non-member cannot view events
- **WHEN** a user who is not a member of team "acme" attempts to view an event
- **THEN** the request is rejected as unauthorized

---

### Requirement: Team member can list team events
The system SHALL allow team members with Crew role or above to list all events
for their team. The list is served by the Registrations module. Archived events
SHALL be excluded from listings by default. Events in the `Pending` creation
state (not yet materialised in Registrations) SHALL NOT appear in this list;
they are discoverable through the creation-status endpoint instead.

#### Scenario: List active events excludes archived
- **WHEN** a Crew member of team "acme" lists events and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are returned and "conf-2025" is not included

#### Scenario: Pending creations are not listed
- **WHEN** team "acme" has a pending creation request for slug "future-conf" and a materialised active event "conf-2026"
- **THEN** only "conf-2026" is returned by the events list

---

### Requirement: Organizer can update event details
The system SHALL allow organizers to update a `TicketedEvent`'s name, website
URL, base URL, and start/end dates. Updates are handled by the Registrations
module. The system SHALL use optimistic concurrency (expected version) to
prevent lost updates. The `TicketedEvent` aggregate SHALL reject modifications
to itself when its own status is Cancelled or Archived.

#### Scenario: Update event details
- **WHEN** an organizer of team "acme" updates event "conf-2026" name to "Acme Conference 2026" with expected version 1 and the current version is 1
- **THEN** the event name is changed and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an organizer updates event "conf-2026" with expected version 1 but the current version is 2
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Reject update of cancelled event
- **WHEN** an organizer attempts to update the name of a cancelled event
- **THEN** the `TicketedEvent` rejects the update with reason "event not active"

---

### Requirement: Organizer can cancel an event
The system SHALL allow organizers to cancel an active `TicketedEvent`. The
command is handled by the Registrations module. In the same unit of work the
`TicketedEvent` aggregate SHALL transition its status to Cancelled, publish an
in-module domain event that projects `EventStatus = Cancelled` onto the
event's `TicketCatalog`, and outbox a `TicketedEventCancelled` integration
event to Organization so the team's counters can be advanced (see
team-management).

#### Scenario: Cancel an active event
- **WHEN** an organizer cancels event "conf-2026" which is active
- **THEN** the `TicketedEvent` status is changed to Cancelled, the event's `TicketCatalog.EventStatus` is set to Cancelled in the same unit of work, and a `TicketedEventCancelled` integration event is outboxed

#### Scenario: Reject cancelling an already cancelled event
- **WHEN** an organizer attempts to cancel event "meetup-q1" which is already cancelled
- **THEN** the request is rejected because the event is already cancelled

---

### Requirement: Organizer can archive an event
The system SHALL allow organizers to archive an active or cancelled
`TicketedEvent`. The command is handled by the Registrations module. In the
same unit of work the `TicketedEvent` aggregate SHALL transition its status
to Archived, publish an in-module domain event that projects `EventStatus = Archived`
onto the event's `TicketCatalog`, and outbox a `TicketedEventArchived`
integration event to Organization.

#### Scenario: Archive an active event
- **WHEN** an organizer archives event "conf-2025" which is active
- **THEN** the `TicketedEvent` status is changed to Archived, the `TicketCatalog.EventStatus` is set to Archived, and a `TicketedEventArchived` integration event is outboxed

#### Scenario: Archive a cancelled event
- **WHEN** an organizer archives event "meetup-q1" which is cancelled
- **THEN** the `TicketedEvent` status is changed to Archived, the `TicketCatalog.EventStatus` is set to Archived, and a `TicketedEventArchived` integration event is outboxed

#### Scenario: Reject archiving an already archived event
- **WHEN** an organizer attempts to archive event "conf-2024" which is already archived
- **THEN** the request is rejected because the event is already archived

---

### Requirement: Creation-status endpoint surfaces async creation outcome
The Organization module SHALL expose
`GET /admin/teams/{teamSlug}/events/creation-requests/{creationRequestId}` that
returns the current state of a `TeamEventCreationRequest`: `Pending`,
`Created` (with the created event's slug), `Rejected` (with a structured
reason such as `duplicate_slug`), or `Expired`. Responses SHALL include cache
headers appropriate for short-interval polling.

#### Scenario: Pending creation request
- **WHEN** a creation request has been accepted but no response event has been processed yet
- **THEN** the endpoint returns status `Pending`

#### Scenario: Successful creation
- **WHEN** Organization has processed a `TicketedEventCreated` for the request
- **THEN** the endpoint returns status `Created` with the event slug

#### Scenario: Rejected creation
- **WHEN** Organization has processed a `TicketedEventCreationRejected` with reason "duplicate slug"
- **THEN** the endpoint returns status `Rejected` with reason `duplicate_slug`

#### Scenario: Unknown request id
- **WHEN** the `creationRequestId` does not exist for the team
- **THEN** the endpoint returns `404`

---

### Requirement: TicketedEvent owns the registration policy
The `TicketedEvent` aggregate SHALL own a `TicketedEventRegistrationPolicy`
value object storing a registration window (`OpensAt` and `ClosesAt`) and an
optional email-domain restriction (single domain pattern, e.g. "@acme.com").
The close datetime SHALL be strictly after the open datetime. The aggregate
SHALL allow organizers (Owner or Organizer role) to configure and update the
policy. Policy mutations SHALL be rejected when the `TicketedEvent`'s own
status is Cancelled or Archived.

Self-service registrations outside the window or from a non-matching email
domain SHALL be rejected by the attendee-registration capability.
Coupon-based registrations SHALL bypass the domain restriction and, when the
coupon has `bypassRegistrationWindow` enabled, also the window. There is no
separate stored "registration status".

#### Scenario: Configure the registration window
- **WHEN** an organizer sets the registration window for active event "DevConf" to "2025-01-01T00:00Z" / "2025-06-01T00:00Z"
- **THEN** the `TicketedEventRegistrationPolicy` is saved with the provided window

#### Scenario: Update the registration window
- **WHEN** an organizer updates the registration window for event "DevConf" to "2025-02-01T00:00Z" / "2025-07-01T00:00Z"
- **THEN** the policy is updated

#### Scenario: Configure an email-domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com"
- **THEN** the policy is saved with the restriction

#### Scenario: Remove an email-domain restriction
- **WHEN** an organizer removes the email-domain restriction from event "CorpConf"
- **THEN** the policy is saved with no domain restriction

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a registration window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Cancelled
- **WHEN** event "DevConf" has status Cancelled and an organizer attempts to set the registration window
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

#### Scenario: Rejected — event is Archived
- **WHEN** event "DevConf" has status Archived and an organizer attempts to set the registration window
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

---

### Requirement: Registration openness is derived from window and event status
The system SHALL derive whether registration is open for an event from two
sources only: the `TicketedEventRegistrationPolicy` window
(`now ∈ [opensAt, closesAt)`) and the `TicketedEvent`'s own status. The system
SHALL NOT store a separate registration-status value.

Registration is "open" when all of the following hold:

- the `TicketedEvent` has a `TicketedEventRegistrationPolicy` with a window configured, and
- `opensAt ≤ now < closesAt`, and
- the `TicketedEvent.Status` is Active.

Otherwise registration is "closed".

#### Scenario: Registration open within window and Active status
- **WHEN** event "DevConf" has window "2025-01-01T00:00Z" / "2025-06-01T00:00Z", current time is "2025-03-15T12:00Z", and status is Active
- **THEN** registration for "DevConf" is reported as open

#### Scenario: Registration closed before window opens
- **WHEN** current time is "2024-12-31T23:59Z" and the window opens "2025-01-01T00:00Z"
- **THEN** registration is reported as closed

#### Scenario: Registration closed after window closes
- **WHEN** current time is "2025-06-01T00:01Z" and the window closes "2025-06-01T00:00Z"
- **THEN** registration is reported as closed

#### Scenario: Registration closed with no policy configured
- **WHEN** event "DevConf" has no `TicketedEventRegistrationPolicy`
- **THEN** registration is reported as closed

#### Scenario: Registration closed when event is Cancelled
- **WHEN** event "OldConf" has an open window and status Cancelled
- **THEN** registration is reported as closed

#### Scenario: Registration closed when event is Archived
- **WHEN** event "OldConf" has an open window and status Archived
- **THEN** registration is reported as closed

---

### Requirement: TicketedEvent owns the cancellation policy
The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventCancellationPolicy` value object storing a single
`LateCancellationCutoff` datetime. Attendee-initiated cancellations submitted
on or after that moment SHALL be classified as "late"; cancellations submitted
before it SHALL be classified as "on time". The policy itself does not reject
cancellations or impose fees — it is pure classification data.

The policy is optional. When no `TicketedEventCancellationPolicy` is configured,
no cancellation is ever classified as late. The policy MAY be cleared.
Configuring or updating the policy SHALL be rejected when the `TicketedEvent`
status is Cancelled or Archived.

#### Scenario: Configure a late-cancellation cutoff
- **WHEN** an organizer sets the late-cancellation cutoff for active event "DevConf" to "2025-05-25T00:00Z"
- **THEN** the `TicketedEventCancellationPolicy` is saved with `LateCancellationCutoff = 2025-05-25T00:00Z`

#### Scenario: Update a late-cancellation cutoff
- **WHEN** event "DevConf" has a policy with cutoff "2025-05-25T00:00Z" and an organizer updates it to "2025-05-20T00:00Z"
- **THEN** the policy is updated to "2025-05-20T00:00Z"

#### Scenario: Remove the cancellation policy
- **WHEN** event "DevConf" has a cancellation policy and an organizer removes it
- **THEN** the policy no longer exists for "DevConf"

#### Scenario: Cancellation at cutoff is late
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at exactly "2025-05-25T00:00Z"
- **THEN** the cancellation is classified as late

#### Scenario: Cancellation after cutoff is late
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at "2025-05-28T00:00Z"
- **THEN** the cancellation is classified as late

#### Scenario: Cancellation before cutoff is on time
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at "2025-05-20T12:00Z"
- **THEN** the cancellation is classified as on time

#### Scenario: No policy means never late
- **WHEN** event "DevConf" has no cancellation policy and an attendee cancels at "2025-07-01T00:00Z"
- **THEN** the cancellation is classified as on time

#### Scenario: Rejected — event is Cancelled
- **WHEN** event "DevConf" has status Cancelled and an organizer attempts to set the late-cancellation cutoff
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

---

### Requirement: TicketedEvent owns the reconfirm policy
The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventReconfirmPolicy` value object storing:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes, and
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how
  often attendees are asked to reconfirm.

The close datetime SHALL be strictly after the open datetime. The cadence
SHALL be strictly positive and at least 1 day. The policy describes *when
and how often* attendees should be asked to reconfirm; sending messages is
not part of this capability. The policy is optional; when absent the system
SHALL NOT ask attendees to reconfirm. The policy MAY be cleared. Configuring
or updating the policy SHALL be rejected when the `TicketedEvent` status is
Cancelled or Archived.

#### Scenario: Configure a reconfirm policy
- **WHEN** an organizer sets the reconfirm window for active event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days
- **THEN** the `TicketedEventReconfirmPolicy` is saved with the provided window and cadence

#### Scenario: Update a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and an organizer updates it to cadence 3 days
- **THEN** the policy cadence is updated to 3 days

#### Scenario: Remove a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy and an organizer removes it
- **THEN** the policy no longer exists for "DevConf"

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a reconfirm window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — cadence below minimum
- **WHEN** an organizer sets a reconfirm cadence below 1 day
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Archived
- **WHEN** event "DevConf" has status Archived and an organizer attempts to configure the reconfirm policy
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

---

### Requirement: Lifecycle transitions publish integration events to Organization
The Registrations module SHALL outbox a corresponding integration event for Organization whenever a `TicketedEvent` transitions lifecycle state:
`TicketedEventCreated` on creation, `TicketedEventCreationRejected` on a
failed creation attempt, `TicketedEventCancelled` on cancel, and
`TicketedEventArchived` on archive. Each event SHALL carry at minimum the
`TeamId`, the `TicketedEventId` (when one exists), the event slug (when one
exists), and — for the creation-response events — the originating
`CreationRequestId` so Organization can correlate with its
`TeamEventCreationRequest` record.

Event consumers on the Organization side SHALL be idempotent against redelivery.

#### Scenario: Cancellation emits an integration event
- **WHEN** a `TicketedEvent` transitions to Cancelled
- **THEN** a `TicketedEventCancelled` integration event is outboxed carrying the `TeamId`, `TicketedEventId`, and event slug

#### Scenario: Archival emits an integration event
- **WHEN** a `TicketedEvent` transitions to Archived
- **THEN** a `TicketedEventArchived` integration event is outboxed

#### Scenario: Creation success emits an integration event
- **WHEN** Registrations successfully creates a `TicketedEvent` from a `TicketedEventCreationRequested`
- **THEN** a `TicketedEventCreated` integration event is outboxed carrying the `CreationRequestId` and the new event's identity

#### Scenario: Creation rejection emits an integration event
- **WHEN** Registrations rejects a `TicketedEventCreationRequested` because of a duplicate slug
- **THEN** a `TicketedEventCreationRejected` integration event is outboxed carrying the `CreationRequestId` and reason `duplicate_slug`

#### Scenario: Redelivery of a lifecycle event is idempotent
- **WHEN** the same `TicketedEventCancelled` integration event is delivered twice to Organization
- **THEN** the team counters are updated at most once

---

### Requirement: TicketedEvent owns the additional-detail schema
The `TicketedEvent` aggregate SHALL own an ordered `AdditionalDetailSchema` listing the additional information fields collected from attendees during registration. Each `AdditionalDetailField` SHALL carry a stable `Key` (kebab-case, immutable once persisted), a human-readable `Name` (editable), and a `MaxLength` integer in `[1, 4000]`. The schema SHALL allow at most 25 fields per event. Field `Key` and field `Name` SHALL each be unique within the schema (case-insensitive for `Name`).

The aggregate SHALL allow organizers (Owner or Organizer role) to atomically replace the entire schema via a single `UpdateAdditionalDetailSchema` admin command. Schema mutations SHALL be rejected when the event's status is Cancelled or Archived. Schema updates SHALL participate in optimistic concurrency via `TicketedEvent.Version`.

Removing a field from the schema SHALL leave any existing values for that field untouched on already-persisted registrations (see registration-additional-details for storage and presentation behaviour).

The detailed validation rules for additional detail values at registration time, the storage shape on registrations, and the public/admin surfaces are defined in the `registration-additional-details` capability.

#### Scenario: Configure an initial additional-detail schema
- **WHEN** an organizer of active event "DevConf" updates the schema to `[{ key: "dietary", name: "Dietary requirements", maxLength: 200 }, { key: "tshirt", name: "T-shirt size", maxLength: 5 }]`
- **THEN** `TicketedEvent` persists the schema in the supplied order and `Version` is incremented

#### Scenario: Reorder fields
- **WHEN** an organizer submits the same fields in reversed order
- **THEN** the schema is persisted in the new order

#### Scenario: Rename a field while keeping its key
- **WHEN** an organizer changes `name` of the field with `key: "dietary"` to "Dietary needs"
- **THEN** the field's `Key` remains "dietary" and existing registration values for that key remain accessible

#### Scenario: Rejected — duplicate key
- **WHEN** an organizer submits a schema containing two fields with `key: "dietary"`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — duplicate name (case-insensitive)
- **WHEN** an organizer submits a schema containing fields named "Dietary" and "dietary"
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — invalid key format
- **WHEN** an organizer submits a field with `key: "Dietary Needs"`
- **THEN** the request is rejected with a validation error indicating the key must match `^[a-z0-9][a-z0-9-]{0,49}$`

#### Scenario: Rejected — too many fields
- **WHEN** an organizer submits a schema with 26 fields
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Cancelled
- **WHEN** event "DevConf" has status Cancelled and an organizer attempts to update the additional-detail schema
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

#### Scenario: Rejected — concurrent update conflict
- **WHEN** an organizer submits a schema update with a stale `Version`
- **THEN** the request is rejected with a concurrency conflict
