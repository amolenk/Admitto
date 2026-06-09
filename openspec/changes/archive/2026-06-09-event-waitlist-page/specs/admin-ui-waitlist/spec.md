## MODIFIED Requirements

### Requirement: Organizer can view the waitlist for a ticket type

The admin UI SHALL provide a waitlist management page per ticket type. The page SHALL
be accessible from:
- The event's Waitlist overview page (via a row link in the summary table), and
- The ticket types list (via the hourglass button on each ticket type card).

The page SHALL show:

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
