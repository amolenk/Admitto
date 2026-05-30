## Context

The event dashboard currently shows static snapshot metrics (total registrations, ticket type availability, check-in readiness). Organizers have no way to see how sales are trending over time. The hero card and check-in card also carry "Copy link" / "Share link" buttons that are not yet functional and create visual clutter.

All registration data flows through the existing `GET /admin/teams/{teamId}/events/{eventId}/registrations` endpoint, which returns `RegistrationListItemDto` objects including a `createdAt` timestamp. No new backend work is required.

Recharts (`^2.15.3`) is already installed in the Admin UI.

## Goals / Non-Goals

**Goals:**
- Remove the two non-functional copy/share shortcut buttons from the dashboard.
- Add a `SalesTrendCard` component that computes a daily registration count from `createdAt` timestamps and renders it as an area sparkline.

**Non-Goals:**
- Real-time push updates (polling every page load is sufficient for now).
- Filtering by ticket type within the trend card.
- A new backend analytics endpoint — client-side aggregation of existing data is enough.

## Decisions

### Reuse the existing registrations endpoint instead of a new analytics endpoint
Fetching all registrations and aggregating client-side keeps backend scope to zero. For events up to ~10 000 registrations this is fast enough. A dedicated aggregation endpoint could be added later if needed.

*Alternative considered*: A new `/admin/.../registration-stats` endpoint returning pre-aggregated time-series data. Rejected because it adds backend work with no user-facing benefit at this scale.

### Recharts `AreaChart` for the sparkline
Recharts is already present and used elsewhere in the codebase (e.g. check-in stats). Using it avoids a new dependency.

*Alternative considered*: A hand-rolled SVG sparkline (as seen in `design/dashboard.jsx`). Rejected because Recharts provides responsive sizing, tooltip, and accessibility out of the box.

### Show last 14 days of data (or full range if < 14 days since opening)
14 days gives enough history to see a trend without requiring scrolling or pagination. The x-axis always ends at today and starts 13 days prior (or at the registration-opens date if more recent).

*Alternative considered*: A tab switcher (24h / 14d / 90d) as sketched in `design/dashboard.jsx`. Deferred to keep scope small; can be added as a follow-up.

## Risks / Trade-offs

- [Large event] For events with tens of thousands of registrations, fetching all records to compute a sparkline is wasteful. → Mitigation: The registrations query is already called by the page for the registrations tab; it is cached by React Query. The trend card shares that cache key, so no extra network requests are made.
- [Empty state] Events with zero registrations will show an empty chart area. → Mitigation: Render a zero-line or a friendly "No registrations yet" placeholder.
