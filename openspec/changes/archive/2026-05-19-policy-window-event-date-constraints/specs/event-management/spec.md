## MODIFIED Requirements

### Requirement: TicketedEvent owns the registration policy
The `TicketedEvent` aggregate SHALL own a `TicketedEventRegistrationPolicy`
value object storing a registration window (`OpensAt` and `ClosesAt`) and an
optional email-domain restriction (single domain pattern, e.g. "@acme.com").
The close datetime SHALL be strictly after the open datetime. The close datetime
SHALL be on or before the event's `EndsAt`. The aggregate
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

#### Scenario: Rejected — registration window closes after event ends
- **WHEN** an organizer sets a registration window whose `ClosesAt` is after the event's `EndsAt`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Cancelled
- **WHEN** event "DevConf" has status Cancelled and an organizer attempts to set the registration window
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

#### Scenario: Rejected — event is Archived
- **WHEN** event "DevConf" has status Archived and an organizer attempts to set the registration window
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

---

### Requirement: TicketedEvent owns the reconfirm policy
The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventReconfirmPolicy` value object storing:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes,
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how often the scheduler ticks to evaluate reconfirmation, and
- a `MinEmailInterval` expressed as a positive integer in hours (minimum 1) representing the minimum time that must elapse since the later of (an attendee's registration time, the last reconfirmation email sent to that attendee) before the system will send them another reconfirmation email.

The close datetime SHALL be strictly after the open datetime. The close datetime
SHALL be strictly before the event's `StartsAt`. The cadence SHALL be strictly
positive and at least 1 day. The `MinEmailInterval` SHALL be a positive integer
of at least 1 hour. The policy describes *when and how often* attendees are
asked to reconfirm; sending messages is not part of this capability. The policy
is optional; when absent the system SHALL NOT ask attendees to reconfirm. The
policy MAY be cleared. Configuring or updating the policy SHALL be rejected when
the `TicketedEvent` status is Archived.

#### Scenario: Configure a reconfirm policy
- **WHEN** an organizer sets the reconfirm window for active event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days and MinEmailInterval 24 hours
- **THEN** the `TicketedEventReconfirmPolicy` is saved with the provided window, cadence, and MinEmailInterval

#### Scenario: Update a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and MinEmailInterval 24 hours and an organizer updates cadence to 3 days and MinEmailInterval to 48 hours
- **THEN** the policy is updated to cadence 3 days and MinEmailInterval 48 hours

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

#### Scenario: Rejected — event is Archived
- **WHEN** event "DevConf" has status Archived and an organizer attempts to configure the reconfirm policy
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"
