## 1. Sidebar Navigation

- [x] 1.1 Add a "Waitlist" entry to the `eventPages` array in `nav-event-pages.tsx`, positioned between "Ticket types" and "Badges", using the `Hourglass` icon from lucide-react and href `/waitlist`

## 2. Waitlist Overview Page

- [x] 2.1 Create the page file at `app/(dashboard)/teams/[teamId]/events/[eventId]/waitlist/page.tsx`
- [x] 2.2 Fetch all ticket types for the event and filter to those with `waitlistEnabled = true`
- [x] 2.3 For each waitlist-enabled ticket type, fetch waitlist stats in parallel using the existing proxy route
- [x] 2.4 Render a summary table with columns: Ticket type, Waiting, Pending notifications, and a link icon to the per-ticket-type detail page
- [x] 2.5 Render an empty state when no ticket types have `waitlistEnabled = true`, with a hint to enable waitlists on a ticket type
- [x] 2.6 Add loading skeleton while data is being fetched

## 3. Spec Sync

- [x] 3.1 Verify the `admin-ui-waitlist` spec delta (new access route via overview page) is accurate and complete
