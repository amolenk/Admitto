# Registration Policy Specification

## Purpose

The Registrations module owns each event's `EventRegistrationPolicy`, including an explicit `RegistrationStatus` (`Draft`, `Open`, `Closed`) that organizers transition via admin endpoints. Opening registration is gated by the event's lifecycle status, not by organizer-managed email settings. Policies are created exclusively in response to a `TicketedEventCreatedModuleEvent` published by the Organization module — handlers never create policies on demand and surface NotFound errors when a policy is missing.

## Requirements

### Requirement: Registration policy tracks an explicit registration status
The system SHALL track an explicit `RegistrationStatus` on each `EventRegistrationPolicy` with values `Draft`, `Open`, and `Closed`. Newly created policies for events created through the admin UI SHALL default to `Draft`. Policies migrated from pre-existing data SHALL default to `Open` to preserve current behavior. Self-service registration and coupon-based registration SHALL be rejected unless the status is `Open` (in addition to existing window and lifecycle checks).

#### Scenario: New event defaults to Draft
- **WHEN** a team owner creates a new ticketed event "devconf-2026" via the admin UI
- **THEN** its `EventRegistrationPolicy.RegistrationStatus` is `Draft`

#### Scenario: Self-service registration blocked when status is Draft
- **WHEN** the registration status for "devconf-2026" is `Draft` and an attendee attempts self-service registration
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Self-service registration blocked when status is Closed
- **WHEN** the registration status for "devconf-2026" is `Closed` and an attendee attempts self-service registration
- **THEN** the registration is rejected with reason "registration not open"

#### Scenario: Coupon registration blocked when status is Draft
- **WHEN** the registration status for "devconf-2026" is `Draft` and an attendee attempts to register with a coupon
- **THEN** the registration is rejected with reason "registration not open"

---

### Requirement: Organizer can open an event for registration
The system SHALL allow organizers (Owner or Organizer role) to transition an event's registration status from `Draft` or `Closed` to `Open` via an admin endpoint. Registration policy configuration and registration-open status SHALL NOT depend on team-scoped or event-scoped organizer email settings. Application email is platform configured through the Admitto system sender, so missing organizer SMTP settings SHALL NOT block opening or configuring registration. Lifecycle status (Cancelled or Archived) SHALL block the transition.

#### Scenario: Open event when lifecycle allows it
- **WHEN** an organizer opens registration for active event "devconf-2026"
- **THEN** the registration status becomes `Open`

#### Scenario: Configure registration policy without team email settings
- **WHEN** an organizer configures a registration policy for an active event and no team email settings row exists
- **THEN** the policy configuration is accepted when all registration-policy rules pass

#### Scenario: Open rejected when event lifecycle is Cancelled
- **WHEN** an organizer attempts to open registration for an event whose lifecycle status is `Cancelled`
- **THEN** the request is rejected with a validation error

#### Scenario: Re-open a previously closed event
- **WHEN** an organizer opens registration for an active event whose status is `Closed`
- **THEN** the registration status becomes `Open`

---

### Requirement: Organizer can close an event for registration
The system SHALL allow organizers (Owner or Organizer role) to transition an event's registration status from `Open` to `Closed` via an admin endpoint. Closing SHALL be permitted regardless of email configuration. After closing, self-service and coupon registrations SHALL be rejected.

#### Scenario: Close an open event
- **WHEN** an organizer closes registration for "devconf-2026" whose status is `Open`
- **THEN** the registration status becomes `Closed`

#### Scenario: Close is idempotent
- **WHEN** an organizer closes registration for an event whose status is already `Closed`
- **THEN** the request succeeds and the status remains `Closed`

---

### Requirement: Registrations module exposes can-open status for the admin UI
The Registrations module SHALL expose an admin query endpoint that reports whether the "Open for registration" action is currently allowed for an event. The response SHALL include the current `RegistrationStatus` and a boolean indicating whether opening is permitted. The implementation SHALL use registration policy and event lifecycle state, not email-settings existence. The endpoint exists so the Admin UI can reflect backend gating without bypassing module boundaries.

#### Scenario: Status reports can-open=true when conditions are met
- **WHEN** the admin UI queries can-open status for an event with `Draft` status and lifecycle `Active`
- **THEN** the response is `{ status: "Draft", canOpen: true }`

#### Scenario: Open-status response does not require email settings
- **WHEN** the admin UI queries registration-open status for an active event with a valid registration window and no organizer SMTP settings
- **THEN** the response is based on the registration policy and event lifecycle, not on email-settings existence

---

### Requirement: Event creation in Organization synchronously creates a registration policy in Registrations
The Organization module SHALL publish a `TicketedEventCreatedModuleEvent` (containing `TeamId` and `TicketedEventId`) whenever a new ticketed event is created. The Registrations module SHALL consume this module event and create an `EventRegistrationPolicy` for the same `TicketedEventId` with `EventLifecycleStatus = Active` and `RegistrationStatus = Draft`. The handler SHALL be idempotent: re-delivery of the same event SHALL NOT create a duplicate policy and SHALL NOT raise an error.

#### Scenario: New event creates a Draft + Active policy
- **WHEN** the Organization module publishes a `TicketedEventCreatedModuleEvent` for event "devconf-2026"
- **THEN** the Registrations module persists an `EventRegistrationPolicy` with the matching id, `EventLifecycleStatus = Active`, and `RegistrationStatus = Draft`

#### Scenario: Re-delivery is a no-op
- **WHEN** the same `TicketedEventCreatedModuleEvent` is delivered twice
- **THEN** exactly one `EventRegistrationPolicy` exists for the event and its lifecycle status is unchanged

---

### Requirement: Registrations handlers surface NotFound when the policy is missing
Every Registrations command and query handler that operates on an `EventRegistrationPolicy` (set window, set allowed email domain, open/close registration, add/update/cancel ticket types, create coupon, self-register attendee, register with coupon, get registration open status) SHALL look up the policy by `TicketedEventId` and SHALL throw a `BusinessRuleViolationException` carrying `EventRegistrationPolicy.Errors.EventNotFound` (`ErrorType.NotFound`) when the policy does not exist. Handlers SHALL NOT create the policy on demand; the policy is created exclusively by the Org→Registrations event-creation sync.

#### Scenario: Adding a ticket type for an unknown event is rejected with NotFound
- **WHEN** an organizer attempts to add a ticket type to an event id that has no `EventRegistrationPolicy` row
- **THEN** the handler throws `BusinessRuleViolationException` with the `EventNotFound` error
- **AND** the API surfaces an HTTP 404 ProblemDetails response

#### Scenario: Setting the registration window for an unknown event is rejected with NotFound
- **WHEN** an organizer attempts to set the registration window for an event id that has no `EventRegistrationPolicy` row
- **THEN** the handler throws `BusinessRuleViolationException` with the `EventNotFound` error
