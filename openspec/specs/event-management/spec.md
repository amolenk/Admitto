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
   to create the `TicketedEvent` aggregate. The system assigns an `EventId` (UUID)
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
for their team. The list is served by the Registrations module. Archived events
SHALL be excluded from listings. Events in the `Pending` creation
state (not yet materialised in Registrations) SHALL NOT appear in this list;
they are discoverable through the creation-status endpoint instead.

This requirement applies equally to the admin API endpoint
(`GET /admin/teams/{teamId}/events`); both admin and non-admin callers receive
only non-archived events.

#### Scenario: List active events excludes archived
- **WHEN** a Crew member of team "Acme Events" lists events and "Acme Conf 2026" (active), "Q1 Meetup" (cancelled), and "Acme Conf 2025" (archived) exist
- **THEN** "Acme Conf 2026" and "Q1 Meetup" are returned and "Acme Conf 2025" is not included

#### Scenario: Pending creations are not listed
- **WHEN** a team has a pending creation request and a materialised active event "Acme Conf 2026"
- **THEN** only "Acme Conf 2026" is returned by the events list

#### Scenario: Admin listing also excludes archived events
- **WHEN** an admin calls `GET /admin/teams/{teamId}/events` and events with active, cancelled, and archived status exist
- **THEN** only active and cancelled events are returned

---

### Requirement: Organizer can update event details
The system SHALL allow organizers to update a `TicketedEvent`'s name, website
URL, base URL, and start/end dates. Updates are handled by the Registrations
module. The system SHALL use optimistic concurrency (expected version) to
prevent lost updates. The `TicketedEvent` aggregate SHALL reject modifications
to itself when its own status is Cancelled or Archived. When start/end dates
change, the aggregate SHALL re-validate any already-configured policies against
the new dates: the registration window's `ClosesAt` SHALL remain on or before
the new `StartsAt`, and the reconfirm window's `ClosesAt` SHALL remain strictly
before the new `StartsAt`. Updates that would violate these constraints SHALL be
rejected with a validation error.

#### Scenario: Update event details
- **WHEN** an organizer of team "acme" updates event "conf-2026" name to "Acme Conference 2026" with expected version 1 and the current version is 1
- **THEN** the event name is changed and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an organizer updates event "conf-2026" with expected version 1 but the current version is 2
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Reject update of cancelled event
- **WHEN** an organizer attempts to update the name of a cancelled event
- **THEN** the `TicketedEvent` rejects the update with reason "event not active"

#### Scenario: Rejected — moving event dates into a configured registration window
- **WHEN** an event has a registration policy and an organizer changes the event's `StartsAt` to be before the policy's `ClosesAt`
- **THEN** the request is rejected with a validation error

---

### Requirement: Organizer can archive an event
The system SHALL allow organizers to archive an **active** `TicketedEvent`. The command is handled by the Registrations module. In the same unit of work the `TicketedEvent` aggregate SHALL transition its status to Archived, publish an in-module domain event that projects `EventStatus = Archived` onto the event's `TicketCatalog`, and outbox a `TicketedEventArchived` integration event to Organization.

Archiving from the `Cancelled` status is no longer supported because the `Cancelled` status has been removed.

#### Scenario: Archive an active event
- **WHEN** an organizer archives event "conf-2025" which is active
- **THEN** the `TicketedEvent` status is changed to Archived, the `TicketCatalog.EventStatus` is set to Archived, and a `TicketedEventArchived` integration event is outboxed

#### Scenario: Reject archiving an already archived event
- **WHEN** an organizer attempts to archive event "conf-2024" which is already archived
- **THEN** the request is rejected because the event is already archived

---

### Requirement: TicketedEvent lifecycle is Active and Archived only
The `TicketedEvent` aggregate SHALL support exactly two lifecycle statuses: `Active` and `Archived`. The `Cancelled` status is removed. Transition `Active → Archived` is the only permitted lifecycle mutation after creation. The `TicketedEvent` aggregate SHALL reject modifications to itself when its own status is Archived (replacing the prior Cancelled-or-Archived guard).

#### Scenario: Reject update of archived event
- **WHEN** an organizer attempts to update the name of an archived event
- **THEN** the `TicketedEvent` rejects the update with reason "event not active"

#### Scenario: List events returns only active events
- **WHEN** an admin calls `GET /admin/teams/{teamId}/events` and events with active and archived status exist
- **THEN** only active events are returned (archived events are excluded)

---

### Requirement: Creation-status endpoint surfaces async creation outcome
The Organization module SHALL expose
`GET /admin/teams/{teamId}/event-creations/{creationRequestId}` that
returns the current state of a `TeamEventCreationRequest`: `Pending`,
`Created` (with the created event's UUID), `Rejected` (with a structured
reason), or `Expired`. Responses SHALL include cache
headers appropriate for short-interval polling.

#### Scenario: Pending creation request
- **WHEN** a creation request has been accepted but no response event has been processed yet
- **THEN** the endpoint returns status `Pending`

#### Scenario: Successful creation
- **WHEN** Organization has processed a `TicketedEventCreated` for the request
- **THEN** the endpoint returns status `Created` with the event's UUID

#### Scenario: Rejected creation
- **WHEN** Organization has processed a `TicketedEventCreationRejected`
- **THEN** the endpoint returns status `Rejected` with the rejection reason

#### Scenario: Unknown request id
- **WHEN** the `creationRequestId` does not exist for the team
- **THEN** the endpoint returns `404`

---

### Requirement: TicketedEvent owns the registration policy
The `TicketedEvent` aggregate SHALL own a `TicketedEventRegistrationPolicy`
value object storing a registration window (`OpensAt` and `ClosesAt`) and an
optional email-domain restriction (single domain pattern, e.g. "@acme.com").
The close datetime SHALL be strictly after the open datetime. The close datetime
SHALL be on or before the event's `StartsAt`. The aggregate
SHALL allow organizers (Owner or Organizer role) to configure and update the
policy. The policy is optional and MAY be cleared; when absent, self-service
registration is closed. Configuring the policy SHALL require both `OpensAt` and
`ClosesAt` (with an optional email-domain restriction); supplying an email-domain
restriction or only one window bound without a complete window SHALL be rejected
as an incomplete policy. Policy mutations SHALL be rejected when the
`TicketedEvent`'s own status is Cancelled or Archived.

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

#### Scenario: Clear the registration policy
- **WHEN** event "DevConf" has a registration policy and an organizer clears it by sending no policy fields
- **THEN** the `TicketedEventRegistrationPolicy` is removed and self-service registration is closed

#### Scenario: Rejected — incomplete registration policy
- **WHEN** an organizer submits a registration policy with only one of `OpensAt`/`ClosesAt`, or with only an email-domain restriction
- **THEN** the request is rejected with an incomplete-policy validation error

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a registration window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — registration window closes after event starts
- **WHEN** an organizer sets a registration window whose `ClosesAt` is after the event's `StartsAt`
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

### Requirement: TicketedEvent owns the reconfirm policy
The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventReconfirmPolicy` value object storing:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes,
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how often the scheduler ticks to evaluate reconfirmation,
- a `MinEmailInterval` expressed as a positive integer in hours (minimum 1) representing the minimum time that must elapse since the later of (an attendee's registration time, the last reconfirmation email sent to that attendee) before the system will send them another reconfirmation email,
- an `AutoCancelEnabled` boolean (default `false`) that controls whether registrations are automatically cancelled after exceeding `MaxReconfirmAttempts` unanswered reconfirm emails, and
- a `MaxReconfirmAttempts` positive integer (minimum 1, required when `AutoCancelEnabled=true`, null/absent when `AutoCancelEnabled=false`) representing the number of reconfirm emails sent without reconfirmation before the system auto-cancels the registration.

The close datetime SHALL be strictly after the open datetime. The close datetime
SHALL be strictly before the event's `StartsAt`. The cadence SHALL be strictly positive and at least 1 day. The `MinEmailInterval` SHALL be a positive integer of at least 1 hour. When `AutoCancelEnabled=true`, `MaxReconfirmAttempts` SHALL be a positive integer of at least 1. The policy describes *when and how often* attendees are asked to reconfirm; sending messages and auto-cancellation decisions are not part of this capability. The policy is optional; when absent the system SHALL NOT ask attendees to reconfirm. The policy MAY be cleared. Configuring or updating the policy SHALL be rejected when the `TicketedEvent` status is Archived.

#### Scenario: Configure a reconfirm policy
- **WHEN** an organizer sets the reconfirm window for active event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days and MinEmailInterval 24 hours
- **THEN** the `TicketedEventReconfirmPolicy` is saved with the provided window, cadence, and MinEmailInterval, and `AutoCancelEnabled=false`

#### Scenario: Configure a reconfirm policy with auto-cancel enabled

- **WHEN** an organizer sets the reconfirm policy for active event "DevConf" with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`
- **THEN** the policy is saved with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`

#### Scenario: Update a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and MinEmailInterval 24 hours and an organizer updates cadence to 3 days and MinEmailInterval to 48 hours
- **THEN** the policy is updated to cadence 3 days and MinEmailInterval 48 hours

#### Scenario: Enable auto-cancel on an existing policy

- **WHEN** event "DevConf" has an existing reconfirm policy with `AutoCancelEnabled=false` and an organizer enables auto-cancel with `MaxReconfirmAttempts=2`
- **THEN** the policy is updated with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=2`

#### Scenario: Remove a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy and an organizer removes it
- **THEN** the policy no longer exists for "DevConf"

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a reconfirm window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — reconfirm window closes after event starts
- **WHEN** an organizer sets a reconfirm window whose `ClosesAt` is on or after the event's `StartsAt`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — cadence below minimum
- **WHEN** an organizer sets a reconfirm cadence below 1 day
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — MinEmailInterval below minimum
- **WHEN** an organizer sets a MinEmailInterval below 1 hour
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — MaxReconfirmAttempts required when auto-cancel is enabled

- **WHEN** an organizer enables `AutoCancelEnabled=true` but provides no `MaxReconfirmAttempts` (or sets it to null)
- **THEN** the request is rejected with a validation error indicating MaxReconfirmAttempts is required

#### Scenario: Rejected — MaxReconfirmAttempts below minimum

- **WHEN** an organizer sets `AutoCancelEnabled=true` and `MaxReconfirmAttempts=0`
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
`TeamId`, the `TicketedEventId` (when one exists), and — for the
creation-response events — the originating `CreationRequestId` so Organization
can correlate with its `TeamEventCreationRequest` record.

Event consumers on the Organization side SHALL be idempotent against redelivery.

#### Scenario: Cancellation emits an integration event
- **WHEN** a `TicketedEvent` transitions to Cancelled
- **THEN** a `TicketedEventCancelled` integration event is outboxed carrying the `TeamId` and `TicketedEventId`

#### Scenario: Archival emits an integration event
- **WHEN** a `TicketedEvent` transitions to Archived
- **THEN** a `TicketedEventArchived` integration event is outboxed

#### Scenario: Creation success emits an integration event
- **WHEN** Registrations successfully creates a `TicketedEvent` from a `TicketedEventCreationRequested`
- **THEN** a `TicketedEventCreated` integration event is outboxed carrying the `CreationRequestId` and the new event's identity

#### Scenario: Redelivery of a lifecycle event is idempotent
- **WHEN** the same `TicketedEventCancelled` integration event is delivered twice to Organization
- **THEN** the team counters are updated at most once

---

### Requirement: TicketedEvent carries an IANA time zone

The `TicketedEvent` aggregate SHALL carry a required `TimeZone` field (IANA zone id, e.g. `Europe/Amsterdam`, `America/Los_Angeles`). The value SHALL be validated against the IANA TZ database at write time. Once persisted, the field MAY be updated by an admin command but the new value SHALL still validate against the IANA database.

The time zone determines the local-clock interpretation of any wall-clock-relative scheduling derived from the event — most notably the cron schedule used to drive reconfirm sending (see `reconfirm-sending`). All other event datetimes (`StartsAt`, `EndsAt`, reconfirm `Window.OpensAt`/`ClosesAt`) continue to be persisted as UTC `DateTimeOffset` values; the `TimeZone` field is the authoritative *display and scheduling* zone for the event, not a reinterpretation of stored instants.

#### Scenario: Create event with time zone
- **WHEN** an organizer posts a creation request including `timeZone: "Europe/Amsterdam"`
- **THEN** the materialised `TicketedEvent` carries `TimeZone="Europe/Amsterdam"`

#### Scenario: Reject creation with unknown time zone
- **WHEN** the creation request carries `timeZone: "Mars/Olympus_Mons"`
- **THEN** Organization (sync) rejects the request with a `400` validation error and `PendingEventCount` is not incremented

#### Scenario: Update event time zone
- **WHEN** an organizer updates the event time zone from `Europe/Amsterdam` to `Europe/London`
- **THEN** the `TicketedEvent.TimeZone` is updated, a `TicketedEventTimeZoneChanged` integration event is outboxed, and any time-zone-dependent scheduling (e.g. the per-event reconfirm cron trigger) is rebuilt against the new zone

#### Scenario: Time zone is required
- **WHEN** a creation request omits `timeZone`
- **THEN** Organization rejects the request with a `400` validation error

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

---

### Requirement: TicketedEvent owns a per-event signing key generated at creation

The `TicketedEvent` aggregate SHALL carry a `SigningKey` value generated at creation time using a cryptographically-secure random source with at least 32 bytes (256 bits) of entropy. The key SHALL be unique per event and SHALL be assigned automatically by the aggregate's factory method — callers SHALL NOT supply the key.

The `SigningKey` SHALL NOT be exposed via:
- public read DTOs of any endpoint, admin or otherwise;
- integration events published from the Registrations module;
- the Organization module's view of ticketed events (slug/id resolution only);
- any structured log entry written by application code.

The `SigningKey` SHALL be persisted in the Registrations schema as a `NOT NULL` column on the `ticketed_events` table.

Existing `TicketedEvent` rows that predate this requirement SHALL be migrated by populating each row with a freshly-generated key in the same schema migration that introduces the column. The migration SHALL transition the column to `NOT NULL` only after every existing row has a value.

#### Scenario: Newly-created event has a signing key
- **WHEN** the Registrations module materialises a new `TicketedEvent` in response to a `TicketedEventCreationRequested` integration event
- **THEN** the resulting aggregate has a non-empty `SigningKey` whose decoded byte length is at least 32

#### Scenario: Each event gets its own key
- **WHEN** two `TicketedEvent`s are created in rapid succession on the same team
- **THEN** their `SigningKey` values differ

#### Scenario: Migration backfills existing events
- **WHEN** the schema migration that introduces `signing_key` runs against a database with pre-existing `ticketed_events` rows
- **THEN** every existing row receives a freshly-generated key before the column is altered to `NOT NULL`, and the migration is safe to retry

#### Scenario: Signing key is not exposed in event details
- **WHEN** an admin retrieves event details via the admin API
- **THEN** the response contains no field carrying the signing key or any value derived from it
