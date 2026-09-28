# 12: Admin UI: "Promote to VIP" action

**What to build:** From the existing per-ticket-type waitlist-detail view, an organizer can pick a specific entry and give it VIP priority with one click, without leaving the page or looking up the person's email separately.

**Blocked by:** 11 (Manual VIP waitlist promotion, backend)

**Status:** ready-for-agent

- [ ] The existing per-ticket-type waitlist-detail view gains an action, available per listed entry, to issue that entry a VIP coupon.
- [ ] Triggering the action calls the new backend endpoint and, on success, removes that entry from the displayed list (reflecting its immediate queue removal).
- [ ] The action is only available for entries that are still active.
- [ ] The Admin UI SDK is regenerated to include the new endpoint before this UI code is written.
