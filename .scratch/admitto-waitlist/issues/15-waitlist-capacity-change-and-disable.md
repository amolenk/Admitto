# 15: Waitlist behaviour on capacity changes and explicit disable

**What to build:** Changing a ticket type's capacity or disabling its waitlist does what the organizer expects. Raising the capacity gives the people waiting coupons for the new slots. Removing the capacity limit gives everyone waiting a coupon. Explicitly disabling the waitlist removes everyone still waiting, and the organizer informs them.

**Blocked by:** none

**Status:** ready-for-agent

Today `TicketCatalog.UpdateTicketType` raises `WaitlistCapacityFreedDomainEvent` when public capacity increases, and `WaitlistForcedDisabledDomainEvent` when the waitlist is disabled or the capacity limit removed — but neither event has a handler, so nothing happens. Raising capacity issues no coupons (contrary to §8.14), and the Admin UI's "Disable waitlist?" dialog promises to "revoke all pending claim coupons and remove all waiting entries", which is not implemented. Removing `MaxCapacity` currently goes through the same force-disable branch as an explicit disable.

**Raising capacity**
- [ ] A handler for `WaitlistCapacityFreedDomainEvent` dispatches `ProcessWaitlistNotificationsCommand` with the freed slots (capped at the active entry count, as today). This covers raising `MaxCapacity` and lowering `ReservedCapacity`.

**Removing the capacity limit (`MaxCapacity` → null)**
- [ ] Every active entry receives a coupon (and the waitlist-offer email), after which the waitlist is disabled and `WaitlistMode` lifted (a waitlist requires bounded capacity). Issued coupons stay valid until they expire.
- [ ] Because `ProcessWaitlistNotifications` exits early unless the ticket type is `WaitlistEnabled && WaitlistMode`, and the aggregate switches both off synchronously, issuance must not depend on that guard — the mechanism (e.g. a "promote everyone" event, or a dedicated command) is the implementer's choice. The invariant: every entry that was active received an offer.
- [ ] This path no longer removes anyone from the waitlist.

**Explicitly disabling the waitlist**
- [ ] A handler for the explicit-disable case removes all active entries on that ticket type's `Waitlist`, in the same unit of work. Disabling always removes all entries (a waitlist only has people waiting while the ticket type is in `WaitlistMode`; any slot that looks free at that moment is already promised to an outstanding coupon).
- [ ] Outstanding coupons stay valid and lapse as usual; when they lapse the expired-offer email is still sent (the cascade is a no-op since the waitlist is disabled).
- [ ] No emails are sent to removed entries — the organizer informs them manually.
- [ ] A registration left with no confirmed tickets and no active entries stays `Waitlisted` (same as today after a lapsed offer).
- [ ] Afterwards, the registration's read model and any later confirmation / ticket-changed email no longer show the removed waitlisted ticket type.

**Capacity increase and explicit disable in the same update**
- [ ] Freed slots are handled first (coupons issued to the front of the queue), then the remaining entries are removed.

**Admin UI**
- [ ] The "Disable waitlist?" confirmation dialog is shown only for an explicit disable, with wording along the lines of: "Disabling the waitlist removes everyone who is still waiting. You'll need to inform them yourself. Offers already sent stay valid until they expire."
- [ ] Removing the capacity limit on a ticket type with people waiting shows an informational note (not a warning) that everyone waiting will receive an offer.

**Tests & docs**
- [ ] Domain tests for `TicketCatalog.UpdateTicketType` cover the distinct events for explicit disable vs capacity-limit removal, and the ordering when capacity increase and disable are combined.
- [ ] Integration tests cover: raising capacity issues N coupons; removing the limit issues a coupon to every active entry and disables the waitlist; explicit disable removes all entries and keeps outstanding coupons redeemable; a registration's details no longer show the removed waitlisted ticket type.
- [ ] Admin UI tests cover the dialog wording and the informational note.
- [ ] arc42 §8.14 and §6 describe the capacity-freed and disable flows as implemented.
