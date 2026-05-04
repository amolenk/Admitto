# Attendee Registration Specification

## Purpose

Attendees register themselves for a ticketed event either via a public self-service endpoint (subject to capacity, registration window, and email-domain rules) or via a single-use coupon code (which can bypass select policies). Registration is gated by the event's lifecycle status on `TicketedEvent`, with `TicketCatalog.EventStatus` providing the atomic claim-time safety net.
## Requirements
### Requirement: Attendee can self-register
The system SHALL allow attendees to register themselves via a public endpoint by
providing their email, attendee info, selected ticket types, **and a valid
email-verification token proving ownership of the supplied email address**.
Self-service registrations SHALL enforce per-ticket-type capacity (ticket types
without an explicit capacity set SHALL be rejected as not available), the
registration window, and optional email domain restrictions.

The system SHALL reject self-service requests that omit the verification token
with reason "email verification required". The system SHALL reject self-service
requests whose token fails signature verification, has expired, or whose embedded
email does not match the supplied registration email, with reason "email
verification invalid". The verification check SHALL run before any event,
catalog, coupon, or ticket-type lookups so that token-related failures do not
leak information about other resources.

Whether registration is open SHALL be derived from the registration window
(`now ∈ [opensAt, closesAt)`) combined with the event's lifecycle status read from
the `TicketedEvent` aggregate (see event-management). There is no separate stored
registration-status. Application handlers SHALL load the `TicketedEvent` to validate
window, domain, and active-status invariants, then atomically claim ticket capacity
on the `TicketCatalog`. The atomic claim SHALL also be guarded by
`TicketCatalog.EventStatus` so that a concurrent cancel/archive cannot leak
through after the policy check; an `EventStatus` of Cancelled or Archived at
claim time SHALL fail the registration with reason "event not active" (the EF
optimistic concurrency token on `TicketCatalog` is the safety net).

#### Scenario: Successful self-service registration
- **WHEN** an attendee self-registers as "dave@example.com" for "General Admission" on event "DevConf" with capacity 100 (50 used), `TicketedEvent.Status` Active, `TicketCatalog.EventStatus` Active, window "2025-01-01T00:00Z" / "2025-06-01T00:00Z" at current time "2025-03-15T12:00Z", no domain restriction, and a valid verification token bound to "dave@example.com"
- **THEN** a registration is created for "dave@example.com" with ticket "General Admission" and capacity used increases to 51

#### Scenario: Self-service rejected — verification token missing
- **WHEN** an attendee self-registers without supplying a verification token
- **THEN** the registration is rejected with reason "email verification required" and no event, catalog, or capacity lookup is performed

#### Scenario: Self-service rejected — verification token invalid
- **WHEN** an attendee self-registers with a token that fails signature verification, has expired, or is bound to a different email than the registration email
- **THEN** the registration is rejected with reason "email verification invalid"

#### Scenario: Self-service rejected — capacity full
- **WHEN** an attendee self-registers for "Workshop" where capacity is 20/20 used and the window is open
- **THEN** the registration is rejected with reason "ticket type at capacity"

#### Scenario: Self-service rejected — ticket type has no capacity set
- **WHEN** an attendee self-registers for "Speaker Pass" which has no capacity configured
- **THEN** the registration is rejected with reason "ticket type not available"

#### Scenario: Self-service rejected — before registration window opens
- **WHEN** an attendee self-registers for an event whose registration window opens tomorrow
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — after registration window closes
- **WHEN** an attendee self-registers for an event whose registration window closed yesterday
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Self-service rejected — no registration window configured
- **WHEN** an attendee self-registers for an event with no registration window configured
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service rejected — email domain mismatch
- **WHEN** an attendee self-registers as "outsider@gmail.com" for event "CorpConf" which is restricted to "@acme.com" and the window is open
- **THEN** the registration is rejected with reason "email domain not allowed"

#### Scenario: Self-service allowed — email domain matches
- **WHEN** an attendee self-registers as "employee@acme.com" for event "CorpConf" which is restricted to "@acme.com", the window is open, and a valid verification token bound to "employee@acme.com"
- **THEN** a registration is created for "employee@acme.com"

#### Scenario: Concurrent cancel detected at claim time
- **WHEN** an attendee self-registers and `TicketedEvent.Status` is Active at policy-check time but `TicketCatalog.EventStatus` has been transitioned to Cancelled by an in-flight cancel before the claim commits
- **THEN** the registration fails with reason "event not active" and no capacity is consumed

---

### Requirement: Attendee can register using a coupon code
The system SHALL allow attendees to register using a valid, unexpired, single-use
coupon code via a dedicated public endpoint. Coupon-based registrations SHALL
restrict ticket type selection to the coupon's allowlisted types.
**Coupon-based registrations SHALL reject any request whose supplied email does
not match `coupon.TargetEmail`, with reason "coupon email mismatch".** This
binds the bearer credential (the coupon code, delivered to the target email) to
the address it was issued for, without requiring a separate verification token.

Coupon-based registrations SHALL bypass capacity enforcement, email domain
restrictions, and the requirement for a capacity to be set. If the coupon has
`bypassRegistrationWindow` set, the registration SHALL also bypass the
registration window. Upon successful registration, the system SHALL mark the
coupon as redeemed. The used capacity counter SHALL be incremented for each
ticket type regardless of bypass. Coupon registrations SHALL NOT bypass the
active-status gate: registrations are rejected when `TicketedEvent.Status` is
Cancelled or Archived (with the `TicketCatalog.EventStatus` claim-time check as
the safety net for concurrent transitions). Coupon registrations SHALL NOT
require an email-verification token.

#### Scenario: Successful coupon registration
- **WHEN** an attendee registers as "speaker@gmail.com" using valid coupon "INVITE-001" issued to "speaker@gmail.com" for "Speaker Pass" on event "DevConf" where capacity is 5/5 used and the window is open
- **THEN** a registration is created for "speaker@gmail.com", coupon "INVITE-001" is marked as redeemed, and capacity used increases to 6

#### Scenario: Coupon rejected — email does not match coupon target
- **WHEN** an attendee registers as "attacker@gmail.com" using valid coupon "INVITE-001" that was issued to "speaker@gmail.com"
- **THEN** the registration is rejected with reason "coupon email mismatch" and the coupon remains unredeemed

#### Scenario: Coupon rejected — expired
- **WHEN** an attendee registers using coupon "INVITE-002" that expired yesterday
- **THEN** the registration is rejected with reason "coupon expired"

#### Scenario: Coupon rejected — already redeemed
- **WHEN** an attendee registers using coupon "INVITE-003" that has already been redeemed
- **THEN** the registration is rejected with reason "coupon already used"

#### Scenario: Coupon rejected — revoked
- **WHEN** an attendee registers using coupon "INVITE-004" that has been revoked
- **THEN** the registration is rejected with reason "coupon revoked"

#### Scenario: Coupon rejected — ticket type not allowlisted
- **WHEN** an attendee registers using coupon "INVITE-005" (allowlisting only "Speaker Pass") for "General Admission"
- **THEN** the registration is rejected with reason "ticket type not allowed for this coupon"

#### Scenario: Coupon bypasses registration window when flag set
- **WHEN** an attendee registers using valid coupon "INVITE-006" with bypassRegistrationWindow enabled for an event whose window has closed, and the supplied email matches the coupon target
- **THEN** a registration is created

#### Scenario: Coupon respects registration window when flag not set
- **WHEN** an attendee registers using valid coupon "INVITE-007" with bypassRegistrationWindow disabled for an event whose window has closed
- **THEN** the registration is rejected with reason "registration closed"

#### Scenario: Coupon bypasses domain restriction
- **WHEN** an attendee registers as "external@gmail.com" using valid coupon "INVITE-008" issued to "external@gmail.com" for event "CorpConf" which is restricted to "@acme.com"
- **THEN** a registration is created for "external@gmail.com"

#### Scenario: Coupon bypasses capacity requirement
- **WHEN** an attendee registers using valid coupon "INVITE-009" for "Speaker Pass" which has no capacity configured, with email matching the coupon target
- **THEN** a registration is created

#### Scenario: Coupon does not bypass cancelled/archived event
- **WHEN** an attendee registers using valid coupon "INVITE-010" for an event with `TicketedEvent.Status` Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Coupon does not require an email-verification token
- **WHEN** an attendee registers using valid coupon "INVITE-011" with email matching the coupon target and no verification token supplied
- **THEN** a registration is created

### Requirement: Ticket selection validation applies to all registration paths
The system SHALL allow selecting multiple ticket types in a single registration.
The system SHALL reject registrations with duplicate ticket types in the selection.
The system SHALL reject registrations referencing non-existent or cancelled ticket
types. The system SHALL reject registrations where selected ticket types have
overlapping time slots. The system SHALL reject all registrations when the
`TicketedEvent.Status` for the event is Cancelled or Archived (and as a
consistency safety net, the atomic claim against `TicketCatalog` rejects when
`TicketCatalog.EventStatus` is Cancelled or Archived). The system SHALL reject
registrations if the email address is already registered for the same event.

#### Scenario: Successful registration with multiple ticket types
- **WHEN** an attendee self-registers selecting both "General Admission" (capacity 100, 50 used) and "Workshop A" (capacity 20, 10 used) on event "DevConf" with an open window and `TicketedEvent.Status` Active
- **THEN** a registration is created with both ticket types, "General Admission" capacity used increases to 51, and "Workshop A" capacity used increases to 11

#### Scenario: Rejected — duplicate ticket types in selection
- **WHEN** an attendee registers selecting "General Admission" twice
- **THEN** the registration is rejected with reason "duplicate ticket types"

#### Scenario: Rejected — non-existent ticket type
- **WHEN** an attendee registers selecting ticket type "Premium VIP" which does not exist on the event
- **THEN** the registration is rejected with reason "unknown ticket type"

#### Scenario: Rejected — cancelled ticket type
- **WHEN** an attendee registers selecting "Workshop A" which has been cancelled
- **THEN** the registration is rejected with reason "ticket type cancelled"

#### Scenario: Rejected — overlapping time slots
- **WHEN** an attendee registers selecting both "Workshop A" (slot "morning") and "Workshop B" (slot "morning") which share a time slot
- **THEN** the registration is rejected with reason "overlapping time slots"

#### Scenario: Rejected — TicketedEvent status is Cancelled
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — TicketedEvent status is Archived
- **WHEN** an attendee attempts to register for event "OldConf" whose `TicketedEvent.Status` is Archived
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Rejected — TicketCatalog.EventStatus catches concurrent transition
- **WHEN** policy checks pass against `TicketedEvent` (Active) but the event is cancelled before the claim commits, so `TicketCatalog.EventStatus` is Cancelled at claim time
- **THEN** the registration is rejected with reason "event not active" and no capacity is consumed

#### Scenario: Rejected — duplicate email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and attempts to register again
- **THEN** the registration is rejected with reason "already registered"

---

### Requirement: Self-registration accepts and validates additional detail values
The self-registration command and public endpoint SHALL accept an optional `additionalDetails` map of `string` keys to `string` values. The handler SHALL validate the map against the event's current `AdditionalDetailSchema` (see event-management). All additional detail values SHALL be optional at the platform layer; missing keys SHALL be treated as not provided and SHALL NOT cause a rejection.

The handler SHALL reject the registration when the map contains a key that is not present in the current schema (`AdditionalDetailKeyNotInSchema`), or when any value's length exceeds the field's `MaxLength` (`AdditionalDetailValueTooLong`). Empty-string values SHALL be accepted and stored verbatim.

Accepted values SHALL be stored on the resulting `Registration` aggregate (see registration-additional-details for the storage model).

#### Scenario: Self-service accepts additional details matching the schema
- **WHEN** an attendee self-registers for event "DevConf" whose schema declares `dietary` (maxLength 200) and `tshirt` (maxLength 5), submitting `{ "dietary": "vegan", "tshirt": "M" }`
- **THEN** the registration is created and the values are stored

#### Scenario: Self-service accepts when additional details are omitted
- **WHEN** an attendee self-registers for "DevConf" without sending any `additionalDetails`
- **THEN** the registration is created with no additional detail values

#### Scenario: Self-service accepts a partial set of declared keys
- **WHEN** an attendee self-registers for "DevConf" with only `{ "dietary": "vegan" }`
- **THEN** the registration is created and `tshirt` is recorded as not provided

#### Scenario: Self-service accepts empty-string values
- **WHEN** an attendee self-registers for "DevConf" with `{ "dietary": "" }`
- **THEN** the registration is created and `dietary` is stored as the empty string

#### Scenario: Self-service rejected — unknown key
- **WHEN** an attendee self-registers for "DevConf" with `{ "shoesize": "44" }` and the schema has no `shoesize` field
- **THEN** the registration is rejected with reason "additional detail key not in schema"

#### Scenario: Self-service rejected — value too long
- **WHEN** an attendee self-registers for "DevConf" with `{ "tshirt": "XXXXL-extra-long" }` and the `tshirt` field has `maxLength: 5`
- **THEN** the registration is rejected with reason "additional detail value too long"

---

### Requirement: Coupon registration accepts and validates additional detail values
The coupon registration command and public endpoint SHALL accept the same optional `additionalDetails` map and apply the same validation rules described in "Self-registration accepts and validates additional detail values". Coupon-based registrations SHALL NOT bypass additional-detail validation; the schema applies regardless of coupon usage.

#### Scenario: Coupon registration accepts additional details
- **WHEN** an attendee redeems coupon "EARLYBIRD" for event "DevConf" and submits `{ "dietary": "vegan" }`
- **THEN** the registration is created with the values stored

#### Scenario: Coupon registration rejected — unknown key
- **WHEN** an attendee redeems a coupon for "DevConf" with `{ "shoesize": "44" }` and the schema has no `shoesize` field
- **THEN** the registration is rejected with reason "additional detail key not in schema"

### Requirement: Registration carries attendee identity, lifecycle status, and reconfirm state

The `Registration` aggregate (Registrations module) SHALL carry:

- Required `FirstName` and `LastName` value objects (string, 1–100 chars, trimmed). They are part of the aggregate's identity-facing data, not derived from the email.
- A `Status` enum with values `Registered` (initial state) and `Cancelled`.
- A `HasReconfirmed` boolean (default `false`) and a `ReconfirmedAt?` timestamp (set when reconfirm succeeds).

The aggregate SHALL expose two domain operations:

- `Cancel(reason)` — transitions `Status` from `Registered` to `Cancelled`, captures the supplied reason on the aggregate, and raises `RegistrationCancelledDomainEvent` (mapped via the module's message policy to `RegistrationCancelledIntegrationEvent` in `Admitto.Module.Registrations.Contracts`). Calling `Cancel` on a registration that is already `Cancelled` SHALL fail with a business-rule violation.
- `Reconfirm()` — sets `HasReconfirmed=true` and `ReconfirmedAt` to the current instant, and raises `RegistrationReconfirmedDomainEvent` (mapped to `RegistrationReconfirmedIntegrationEvent`). Calling `Reconfirm` on a `Cancelled` registration SHALL fail with a business-rule violation.

#### Scenario: New registration starts Registered, not reconfirmed
- **WHEN** a registration is created via any channel
- **THEN** `Status=Registered`, `HasReconfirmed=false`, `ReconfirmedAt=null`, and `FirstName`/`LastName` carry the supplied values

#### Scenario: Cancel transitions to Cancelled and emits an integration event
- **WHEN** an admin cancels a `Registered` registration with reason "duplicate signup"
- **THEN** `Status=Cancelled`, the reason is captured, and `RegistrationCancelledIntegrationEvent` is enqueued in the outbox

#### Scenario: Cancel rejected on already-cancelled registration
- **WHEN** Cancel is invoked on a registration whose `Status` is already `Cancelled`
- **THEN** the operation fails with a business-rule violation and no event is raised

#### Scenario: Reconfirm sets the flag and emits an integration event
- **WHEN** an attendee reconfirms an active registration
- **THEN** `HasReconfirmed=true`, `ReconfirmedAt` is the current instant, and `RegistrationReconfirmedIntegrationEvent` is enqueued

#### Scenario: Reconfirm rejected on cancelled registration
- **WHEN** Reconfirm is invoked on a registration whose `Status` is `Cancelled`
- **THEN** the operation fails with a business-rule violation

---

### Requirement: Self-registration and admin-add registration require first and last name

Every registration-creation channel — public self-service, coupon redemption, and admin-add — SHALL require non-empty `firstName` and `lastName` in the request DTO. FluentValidation SHALL reject requests that omit either field with a `400` response. The values SHALL be parsed via the `FirstName`/`LastName` value object factories so trim/length rules apply at the boundary.

#### Scenario: Self-service registration requires names
- **WHEN** an attendee posts a self-service registration without `firstName`
- **THEN** the API returns `400` with a validation error on `firstName`

#### Scenario: Admin-add registration requires names
- **WHEN** an admin posts an admin-add registration without `lastName`
- **THEN** the API returns `400` with a validation error on `lastName`

#### Scenario: Coupon-based registration requires names
- **WHEN** an attendee redeems a coupon without supplying `firstName` and `lastName`
- **THEN** the API returns `400` with validation errors on the missing fields

---

### Requirement: AttendeeRegisteredIntegrationEvent carries first and last name

The `AttendeeRegisteredIntegrationEvent` published from the Registrations module SHALL carry the registrant's `FirstName` and `LastName` (in addition to the existing `TeamId`, `TicketedEventId`, `RegistrationId`, and email). Downstream consumers (notably the Email module's single-send template renderer) SHALL use these fields rather than deriving a display name from the email.

#### Scenario: Names propagate to the integration event
- **WHEN** "Alice Anderson" registers as `alice@example.com`
- **THEN** the enqueued `AttendeeRegisteredIntegrationEvent` carries `FirstName="Alice"`, `LastName="Anderson"`, `Email="alice@example.com"`

---

### Requirement: Successful attendee registration publishes an integration event
The Registrations module SHALL publish an `AttendeeRegisteredIntegrationEvent` whenever an attendee registration is successfully persisted, regardless of whether the registration originated from self-service or an admin flow. The integration event SHALL be derived from the existing `AttendeeRegisteredDomainEvent` via the module's `MessagePolicy` and SHALL be enqueued through the existing outbox so that delivery is at-least-once and durable.

The integration event SHALL carry at minimum: `TeamId`, `TicketedEventId`, `RegistrationId`, the recipient email address, and the recipient's display name. It SHALL be defined in `Admitto.Module.Registrations.Contracts.IntegrationEvents`.

#### Scenario: Self-service registration publishes the event
- **WHEN** an attendee successfully self-registers for event "DevConf" as "alice@example.com"
- **THEN** an `AttendeeRegisteredIntegrationEvent` is enqueued in the Registrations module's outbox containing `TicketedEventId`, the new `RegistrationId`, recipient="alice@example.com", and the team id

#### Scenario: Failed registration does not publish the event
- **WHEN** a registration attempt is rejected (capacity full, window closed, domain mismatch, etc.)
- **THEN** no `AttendeeRegisteredIntegrationEvent` is enqueued

#### Scenario: Event delivery is at-least-once via the existing outbox + queue
- **WHEN** a registration succeeds and the event is enqueued
- **THEN** the event is delivered through the same outbox + queue infrastructure as other Registrations integration events, with the same retry semantics

---

### Requirement: Self-service registration rejects ticket types not enabled for self-service
The system SHALL reject a self-service registration that includes a ticket type
with `SelfServiceEnabled = false`. This check is performed during `catalog.Claim`
with `enforce: true`. Coupon-based and admin registrations are not subject to
this check.

#### Scenario: Self-service rejected — ticket type not self-service enabled
- **GIVEN** ticket type "vip" on event "conf-2026" has `SelfServiceEnabled = false`
- **WHEN** an attendee self-registers selecting "vip"
- **THEN** the registration is rejected with HTTP 422 and reason "ticket type not available for self-service"

#### Scenario: Coupon registration succeeds for admin-only ticket type
- **GIVEN** ticket type "vip" on event "conf-2026" has `SelfServiceEnabled = false`
- **WHEN** an attendee registers using a valid coupon that includes "vip"
- **THEN** the registration succeeds (coupons bypass the self-service flag)

