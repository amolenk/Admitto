## Context

The event sidebar currently exposes: Dashboard, Registrations, Ticket types, Badges,
Emails, and Settings. Each individual ticket type card on the Ticket Types page has a
hourglass button that links to the per-ticket-type waitlist detail page
(`/events/[eventId]/ticket-types/[ticketTypeId]/waitlist`). This is the only access
route to waitlist data today.

Organizers managing events with multiple waitlist-enabled ticket types have no
aggregated view — they must drill into each ticket type individually. A dedicated
Waitlist sidebar entry would make the feature discoverable and provide a summary that
lets organizers quickly assess the waitlist situation across all ticket types.

The change is Admin UI only. No backend or domain changes are required; the existing
`GET /api/teams/[teamId]/events/[eventId]/ticket-types/[ticketTypeId]/waitlist`
endpoints already return the statistics needed.

## Goals / Non-Goals

**Goals:**
- Add a "Waitlist" entry to the event sidebar (between Ticket types and Badges).
- Add a new event-level overview page listing all waitlist-enabled ticket types with
  summary stats (waiting count, pending notifications count).
- Each row in the table links to the existing per-ticket-type waitlist detail page.
- Show an appropriate empty state when no ticket types have `waitlistEnabled = true`.
- The hourglass button on ticket type cards remains as a direct access shortcut.

**Non-Goals:**
- Inline management (remove entries) from the overview page — that stays on the
  per-ticket-type detail page.
- Pagination or search on the overview table (events realistically have ≤ 20 ticket
  types).
- Backend changes or new API endpoints.

## Decisions

### D1: Fetch stats by calling per-ticket-type endpoints individually

The existing API returns stats per ticket type. The new overview page will:
1. Call `GET /ticket-types` to get the list, filter to `waitlistEnabled = true`.
2. For each such ticket type, call the waitlist endpoint in parallel.

**Alternative considered: a new aggregated endpoint**
A new backend endpoint returning all waitlists for an event would be cleaner. However,
the proposal explicitly scopes this to UI only, and the number of ticket types per event
is small, making N parallel fetches acceptable. This decision can be revisited if a
backend endpoint is added later.

### D2: Page location — top-level event route, not under settings

Waitlist management is an operational activity (not configuration), so it belongs
alongside Registrations and Emails at the event top level, not under Settings.

### D3: Sidebar position — after Ticket types, before Badges

Waitlists are conceptually linked to ticket types (they are per-ticket-type), so
placing the entry immediately after Ticket types reflects this relationship.

### D4: Visibility of sidebar entry

Show the Waitlist entry in the sidebar unconditionally for all events (not only when at
least one ticket type has `waitlistEnabled`). Fetching ticket types on every sidebar
render to conditionally show/hide the entry would add latency and complexity; the empty
state on the page itself adequately handles the case where no ticket types have waitlists
enabled.

## Risks / Trade-offs

- **N+1 fetch on page load** → The overview page triggers one request per
  waitlist-enabled ticket type. Mitigation: queries run in parallel via `Promise.all`
  inside a single `useQueries`/multiple `useQuery` calls; with TanStack Query caching
  subsequent navigations are fast.
- **Data staleness** → Stats are fetched at page load; they do not auto-refresh.
  Mitigation: acceptable for an admin overview; the detail page can be refreshed
  manually.
