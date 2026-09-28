# 05: Registration read model exposes waitlist state

**What to build:** Fetching a registration's details tells the caller which ticket types the attendee currently holds an active waitlist position for, and where in the queue they stand — not just which tickets are confirmed.

**Blocked by:** 01 (Waitlisted registration status)

**Status:** ready-for-agent

- [ ] Fetching a registration's details returns, alongside the existing confirmed ticket-type ids, the list of ticket-type ids the registration's email currently holds an active waitlist entry for.
- [ ] Each returned waitlisted ticket-type entry includes that entry's current queue position.
- [ ] The lookup is performed by matching the registration's email against active waitlist entries for the event (there is no stored link from Registration to WaitlistEntry).
- [ ] A registration with no active waitlist entries returns an empty list for this field; all other existing response fields are unchanged.
- [ ] An integration test covers a registration with a mix of confirmed and waitlisted ticket types, asserting both appear correctly with the right position.
