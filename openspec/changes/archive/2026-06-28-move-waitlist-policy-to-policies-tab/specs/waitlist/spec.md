## MODIFIED Requirements

### Requirement: Waitlist claim window and quiet hours are configurable per ticket type

Organisers SHALL be able to configure a `ClaimWindowHours` (integer, minimum 1,
default 8) per ticket type. The event-level `WaitlistPolicy` SHALL contain
`QuietHoursStart` and `QuietHoursEnd` time-of-day values, defaulting to `22:00`
and `08:00`, and SHALL apply to all ticket type waitlists for that event.
Quiet-hours evaluation uses the event's `TimeZoneId` (IANA identifier); if not
set, UTC is assumed.

#### Scenario: Custom claim window is respected
- **WHEN** "General Admission" has `ClaimWindowHours = 12` and a coupon is issued at
  10:00
- **THEN** the coupon's `ExpiresAt` is 22:00 the same day (10:00 + 12 hours)

#### Scenario: Quiet hours shift the claim window end, not the send time
- **WHEN** a coupon would be issued at 23:00 with `ClaimWindowHours = 8` and
  waitlist policy quiet hours are 22:00–08:00
- **THEN** the coupon IS issued at 23:00 and `ExpiresAt` is 16:00 the next day
  (max(23:00, 08:00) + 8h = 08:00 + 8h = 16:00); the email states the deadline as 16:00

#### Scenario: Waitlist policy defaults apply to new events
- **WHEN** a new ticketed event is created
- **THEN** its waitlist policy uses `QuietHoursStart = 22:00` and `QuietHoursEnd = 08:00`
