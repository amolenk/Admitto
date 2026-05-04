## ADDED Requirements

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
