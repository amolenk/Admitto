# Attendee Registration — Delta Spec (Waitlist Change)

## Status: MODIFIED

Changes to the `attendee-registration` capability introduced by the waitlist change.

---

### MODIFIED Requirement: Self-service registration is rejected for ticket types in WaitlistOnly mode

The existing requirement "Self-service registration is rejected when ticket type is
at capacity" SHALL be extended with an additional rejection reason. When the ticket
type has `WaitlistMode = true` (i.e., `WaitlistEnabled` is on and the type is at
capacity with an active or pending waitlist), the rejection reason SHALL be
`"ticket type in waitlist mode"` rather than the generic capacity reason.

This distinct reason allows the external event website to detect WaitlistOnly mode
and redirect the attendee to the waitlist join flow instead of showing a generic
sold-out message.

#### Scenario: Self-service registration rejected in WaitlistOnly mode
- **WHEN** an attendee submits a self-service registration for ticket type "General
  Admission" on event "DevConf" and "General Admission" has `WaitlistMode = true`
- **THEN** the registration is rejected with reason `"ticket type in waitlist mode"`
  and no registration record is created

#### Scenario: Coupon-based registration bypasses WaitlistOnly mode check
- **WHEN** an attendee submits a registration with a valid waitlist coupon for ticket
  type "General Admission" on event "DevConf" and "General Admission" has
  `WaitlistMode = true`
- **THEN** the registration proceeds normally (coupon bypass is unchanged)
