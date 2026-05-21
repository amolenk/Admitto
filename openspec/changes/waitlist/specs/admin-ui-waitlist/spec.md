# Admin UI — Waitlist Management Specification

## Purpose

Organisers need visibility into the waitlist state for each ticket type and the
ability to manage entries (remove individuals, monitor pending notifications).

## Requirements

### Requirement: Organizer can view the waitlist for a ticket type

The admin UI SHALL provide a waitlist management page per ticket type (accessible
from the event's ticket types list). The page SHALL show:

- The ordered list of active waitlist entries: position, email (masked to first 3
  chars + domain, e.g. `ali***@example.com`), and join date.
- Any pending (notified, coupon not yet redeemed or expired) notifications: position
  (previously on waitlist), masked email, coupon expiry time, and remaining claim
  time countdown.
- Summary statistics: total active entries, total pending notifications, total
  notifications sent today.

#### Scenario: View waitlist page for a ticket type with entries
- **WHEN** an organizer opens the waitlist management page for "General Admission" on
  "DevConf" which has 3 active entries and 1 pending notification
- **THEN** all 3 active entries are shown in ranked order with masked email and join
  date; the 1 pending notification is shown with masked email and remaining claim time

#### Scenario: View waitlist page for a ticket type with no entries
- **WHEN** an organizer opens the waitlist management page for "General Admission"
  which has no entries and no pending notifications
- **THEN** the page shows an empty state message

---

### Requirement: Organizer can remove a waitlist entry

The admin UI SHALL allow an organizer to remove an individual active waitlist entry.
This immediately removes the attendee from the queue. No notification is sent.

#### Scenario: Remove an active waitlist entry
- **WHEN** an organizer removes "alice@example.com" from the waitlist for "General
  Admission" on "DevConf"
- **THEN** the entry is removed, the positions of subsequent entries shift up, and
  "alice@example.com" no longer appears in the active entries list

#### Scenario: Remove entry triggers WaitlistMode re-evaluation
- **WHEN** an organizer removes the last active waitlist entry for "General Admission"
  and there are no pending notifications and capacity is available
- **THEN** WaitlistMode is lifted for "General Admission" and self-service
  registrations are accepted again

---

### Requirement: Ticket type form includes WaitlistEnabled toggle and ClaimWindowHours input

The ticket type create and edit form in the admin UI SHALL include:

- A `waitlistEnabled` toggle (default off), visible only when the ticket type has a
  capacity set.
- A `claimWindowHours` number input (minimum 1, default 8), visible only when
  `waitlistEnabled` is on.
- A tooltip or help text explaining that quiet hours apply at the event level.

#### Scenario: WaitlistEnabled toggle appears only when capacity is configured
- **WHEN** an organizer opens the ticket type edit form for a type with no capacity
- **THEN** the `waitlistEnabled` toggle is not shown (unlimited capacity types cannot
  sell out)

---

### Requirement: Event settings form gains quiet hours pickers

The event settings (edit) form SHALL add `quietHoursStart` and `quietHoursEnd` time
pickers (defaulting to 22:00 and 08:00) alongside the existing timezone selector.
A help text SHALL explain that waitlist notifications will not count these hours toward
the claim window.

#### Scenario: Organizer sets quiet hours on event
- **WHEN** an organizer sets `quietHoursStart: "22:00"` and `quietHoursEnd: "08:00"`
  on event "DevConf" (which already has a timezone configured)
- **THEN** the event is saved with the configured quiet hours, and subsequent waitlist
  coupons for "DevConf" have their `ExpiresAt` extended to skip the quiet window
