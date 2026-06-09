## Why

Organizers currently have to navigate into the Ticket Types page and locate a specific
ticket type to access its waitlist — there is no quick way to see all waitlists for an
event at a glance. Adding a dedicated Waitlist page to the event sidebar gives immediate
visibility into all active waitlists and serves as a convenient hub to jump into the
per-ticket-type detail view.

## What Changes

- A new **Waitlist** entry is added to the event sidebar navigation (between
  Ticket types and Badges).
- A new event-level Waitlist page is added at `/teams/[teamId]/events/[eventId]/waitlist`
  showing a summary table: one row per ticket type that has `waitlistEnabled = true`,
  displaying the ticket type name, number waiting, number of pending notifications, and
  a link to the existing per-ticket-type detail page.
- Ticket types without `waitlistEnabled` are excluded from the table.
- If no ticket type has waitlists enabled, an appropriate empty state is shown.

## Capabilities

### New Capabilities

- `event-waitlist-overview`: A top-level event page in the sidebar that lists all
  waitlist-enabled ticket types with their summary statistics and links to the detailed
  per-ticket-type waitlist view.

### Modified Capabilities

- `admin-ui-waitlist`: The existing spec says the waitlist is accessible from the ticket
  types list only. The new overview page becomes an additional access route; the hourglass
  button on individual ticket type cards remains. The spec should be updated to reflect
  the new sidebar entry point.

## Impact

- **Admin UI only** — no backend changes required; the new page calls the existing
  per-ticket-type waitlist API for each ticket type to gather statistics.
- Files affected: `nav-event-pages.tsx` (sidebar), new page at
  `app/(dashboard)/teams/[teamId]/events/[eventId]/waitlist/page.tsx`.
- No API contract changes; no new proxy routes needed beyond the existing
  `/api/teams/[teamId]/events/[eventId]/ticket-types/[ticketTypeId]/waitlist` endpoints.
