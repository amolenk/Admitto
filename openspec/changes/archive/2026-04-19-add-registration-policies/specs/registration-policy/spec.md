## MODIFIED Requirements

### Requirement: Organizer can configure a registration window
The system SHALL allow organizers (Owner or Organizer role) to configure a
registration window (open and close datetimes) for an event. This configuration
is stored in the Registrations module as part of the event's registration policy.
The close datetime SHALL be strictly after the open datetime. Self-service registrations
outside the window SHALL be rejected; coupon-based registrations are unaffected
unless the coupon has `bypassRegistrationWindow` disabled.

Configuring or updating the registration window SHALL go through the lifecycle guard
(see event-lifecycle-guard) and SHALL therefore only succeed when the event's
lifecycle status is Active.

There is no separate "registration status" toggle: registration is accepted when
`now ∈ [opensAt, closesAt)` and the lifecycle guard is Active.

#### Scenario: Configure registration window
- **WHEN** an organizer sets the registration window for event "DevConf" from "2025-01-01T00:00Z" to "2025-06-01T00:00Z"
- **THEN** the registration window is saved for "DevConf"

#### Scenario: Update existing registration window
- **WHEN** an organizer updates the registration window for event "DevConf" from "2025-01-01T00:00Z" / "2025-06-01T00:00Z" to "2025-02-01T00:00Z" / "2025-07-01T00:00Z"
- **THEN** the registration window is updated

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a registration window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — configuring on a cancelled event
- **WHEN** event "DevConf" has a lifecycle guard with status Cancelled and an organizer attempts to set the registration window
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Rejected — configuring on an archived event
- **WHEN** event "DevConf" has a lifecycle guard with status Archived and an organizer attempts to set the registration window
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can configure an email domain restriction
The system SHALL allow organizers (Owner or Organizer role) to configure an
optional email domain restriction (single domain pattern, e.g. "@acme.com") for
an event. Self-service registrations from non-matching domains SHALL be rejected.
Coupon-based registrations SHALL bypass domain restrictions. The restriction MAY
be removed, after which any email domain is accepted for self-service registration.

Configuring or updating the email-domain restriction SHALL go through the lifecycle
guard and SHALL therefore only succeed when the event's lifecycle status is Active.

#### Scenario: Configure email domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com"
- **THEN** self-service registrations for "CorpConf" are restricted to "@acme.com" emails

#### Scenario: Remove email domain restriction
- **WHEN** an organizer removes the email domain restriction from event "CorpConf" which was restricted to "@acme.com"
- **THEN** self-service registrations for "CorpConf" accept any email domain

#### Scenario: Rejected — configuring on a cancelled event
- **WHEN** event "CorpConf" has a lifecycle guard with status Cancelled and an organizer attempts to set the email-domain restriction
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Registration openness is derived from window and lifecycle
The system SHALL derive whether registration is open for an event from two sources
only: the registration window (`now ∈ [opensAt, closesAt)`) and the event's
lifecycle status from the lifecycle guard. The system SHALL NOT store a separate
registration-status value.

Registration is "open" when **all** of the following hold:
- a `RegistrationPolicy` exists for the event and has a registration window configured, and
- `opensAt ≤ now < closesAt`, and
- the `TicketedEventLifecycleGuard` for the event has `LifecycleStatus = Active`.

Otherwise registration is "closed".

#### Scenario: Registration open within window and Active lifecycle
- **WHEN** event "DevConf" has window "2025-01-01T00:00Z" / "2025-06-01T00:00Z", current time is "2025-03-15T12:00Z", and guard status is Active
- **THEN** registration for "DevConf" is reported as open

#### Scenario: Registration closed before window opens
- **WHEN** current time is "2024-12-31T23:59Z" and the window opens "2025-01-01T00:00Z"
- **THEN** registration is reported as closed

#### Scenario: Registration closed after window closes
- **WHEN** current time is "2025-06-01T00:01Z" and the window closes "2025-06-01T00:00Z"
- **THEN** registration is reported as closed

#### Scenario: Registration closed with no window configured
- **WHEN** event "DevConf" has no registration window configured
- **THEN** registration is reported as closed

#### Scenario: Registration closed when lifecycle is Cancelled
- **WHEN** event "OldConf" has an open window and guard status Cancelled
- **THEN** registration is reported as closed

#### Scenario: Registration closed when lifecycle is Archived
- **WHEN** event "OldConf" has an open window and guard status Archived
- **THEN** registration is reported as closed

## REMOVED Requirements

### Requirement: Registration policy tracks event lifecycle status
**Reason**: Lifecycle status has moved out of `RegistrationPolicy` and into the dedicated `TicketedEventLifecycleGuard` aggregate (see event-lifecycle-guard capability). Centralising lifecycle in one place lets every policy type — registration, cancellation, reconfirm, ticket types — share one consistent active/cancelled/archived check and one concurrency anchor.
**Migration**:
- The `EventLifecycleStatus` column on the `EventRegistrationPolicies` table is dropped.
- An `EventLifecycleGuards` table is added and backfilled from existing `EventRegistrationPolicy.EventLifecycleStatus` values.
- All code that previously read `RegistrationPolicy.EventLifecycleStatus` SHALL be updated to read from the guard.
- The behavioural requirement itself is preserved by the combination of the new event-lifecycle-guard capability and the `attendee-registration` spec, which together reject registration and ticket-type mutations on Cancelled/Archived events.
