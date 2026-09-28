# 06: Admin UI: Waitlisted status display

**What to build:** An event organizer looking at registration lists and exports can immediately tell a purely-waitlisted attendee apart from a confirmed one.

**Blocked by:** 01 (Waitlisted registration status)

**Status:** ready-for-agent

- [ ] The registration list view renders a distinct status badge for `Waitlisted`, alongside the existing `Registered`/`Cancelled` badges.
- [ ] The registration list's status filter includes `Waitlisted` as a selectable option.
- [ ] The registration export includes `Waitlisted` as a possible value in its status column.
- [ ] The Admin UI SDK is regenerated to include the new status value before this UI code is written.
