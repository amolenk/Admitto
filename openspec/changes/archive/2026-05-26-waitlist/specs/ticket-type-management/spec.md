# Ticket Type Management — Delta Spec (Waitlist Change)

## Status: MODIFIED

Changes to the `ticket-type-management` capability introduced by the waitlist change.

---

### MODIFIED Requirement: Organizer can add a ticket type — gains WaitlistEnabled flag

The ticket type create request SHALL accept an optional `waitlistEnabled` boolean
(default `false`). When `waitlistEnabled = true` and the ticket type reaches capacity,
the system will automatically activate WaitlistOnly mode for that type.

#### Scenario: Add a ticket type with waitlist enabled
- **WHEN** an organizer adds ticket type "General Admission" with `waitlistEnabled: true`
  and capacity 200
- **THEN** the ticket type is created with `WaitlistEnabled = true` and WaitlistOnly
  mode is initially `false` (not yet at capacity)

#### Scenario: Add a ticket type with waitlist disabled (default)
- **WHEN** an organizer adds ticket type "VIP Pass" without specifying `waitlistEnabled`
- **THEN** the ticket type is created with `WaitlistEnabled = false`

---

### MODIFIED Requirement: Organizer can update a ticket type — gains WaitlistEnabled flag

The ticket type update request SHALL accept an optional `waitlistEnabled` boolean
and optional `claimWindowHours` integer.

When `waitlistEnabled` is toggled from `false` to `true` and the ticket type is
already at capacity, the system SHALL immediately activate WaitlistMode and create
the Waitlist aggregate, as if the last slot had just been claimed.

When `waitlistEnabled` is set to `false` on a type that currently has WaitlistMode
active, the system SHALL perform a force-disable cleanup: revoke all pending waitlist
coupons (notifying their holders), remove all active waitlist entries, and clear
WaitlistMode — all in the same transaction. This is a deliberate destructive action
that must be confirmed in the Admin UI.

When the capacity limit is removed entirely (set to unlimited) on a ticket type with
`WaitlistEnabled = true`, the system SHALL automatically force `WaitlistEnabled` to
`false` with the same cleanup if WaitlistMode was active.

#### Scenario: Enable waitlist on an existing ticket type with capacity remaining
- **WHEN** an organizer updates ticket type "General Admission" setting `waitlistEnabled: true`
  while `UsedCapacity < MaxCapacity`
- **THEN** `WaitlistEnabled` is set to `true` and `WaitlistMode` remains `false`

#### Scenario: Enable waitlist on a sold-out ticket type activates WaitlistMode immediately
- **WHEN** an organizer updates ticket type "General Admission" setting `waitlistEnabled: true`
  while `UsedCapacity >= MaxCapacity`
- **THEN** `WaitlistEnabled` is set to `true`, `WaitlistMode` is set to `true`, and a
  Waitlist aggregate is created for that ticket type in the same transaction

#### Scenario: Disable waitlist while WaitlistMode is active performs cleanup
- **WHEN** ticket type "General Admission" has `WaitlistMode = true` with 3 active
  entries and 1 pending coupon, and an organizer updates it setting `waitlistEnabled: false`
- **THEN** the 1 pending coupon is revoked (and its holder receives a cancellation email),
  the 3 active waitlist entries are removed, `WaitlistMode` is cleared, and
  `WaitlistEnabled` is set to `false` — all in the same transaction

#### Scenario: Disable waitlist when WaitlistMode is not active
- **WHEN** ticket type "General Admission" has `WaitlistEnabled = true` and
  `WaitlistMode = false`, and an organizer sets `waitlistEnabled: false`
- **THEN** `WaitlistEnabled` is set to `false` with no side effects

#### Scenario: Increase capacity while WaitlistMode is active triggers notification
- **WHEN** ticket type "General Admission" has `WaitlistMode = true` with 2 active entries
  and the organizer increases `MaxCapacity` by 1 (creating 1 free slot)
- **THEN** `ProcessWaitlistNotifications` is triggered for 1 freed slot, the first
  waiting attendee is notified, and WaitlistMode conditions are re-evaluated

#### Scenario: Update ClaimWindowHours on a ticket type
- **WHEN** an organizer updates ticket type "General Admission" setting `claimWindowHours: 12`
- **THEN** `ClaimWindowHours` is updated to 12 and subsequent claim offers will expire
  12 hours after notification time

---

### MODIFIED Requirement: Ticket type listings include WaitlistMode status

The ticket type listing response SHALL include a derived `waitlistMode` boolean for
each ticket type. This lets the public event website know when to display the "join
waitlist" button for sold-out ticket types.

#### Scenario: List ticket types includes WaitlistMode
- **WHEN** event "DevConf" has ticket type "General Admission" with `WaitlistMode = true`
  and "VIP Pass" with `WaitlistMode = false`
- **THEN** the listing returns `"waitlistMode": true` for "General Admission" and
  `"waitlistMode": false` for "VIP Pass"
